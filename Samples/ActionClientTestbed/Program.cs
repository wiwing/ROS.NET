using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Threading;
using Uml.Robotics.Ros;
using Uml.Robotics.Ros.ActionLib;
using Messages.actionlib_msgs;


namespace ActionClientTestbed
{
    class Program
    {
        private ILogger Logger { get; } = ApplicationLogging.CreateLogger("ActionClientTestbed");
        private TestState testState;
        private int receivedFeedback = 0;
        private ActionClient<Messages.actionlib.TestGoal, Messages.actionlib.TestResult, Messages.actionlib.TestFeedback> actionClient;
        private List<TestParameters> TestParams = new List<TestParameters>();

        static void Main(string[] args)
        {
            Console.WriteLine("Start roscore and ActionServerTesbed.lua and press any key");
            while (!Console.KeyAvailable)
            {
                Thread.Sleep(1);
            }

            (new Program()).Start();
        }


        private void Start()
        {
            Console.WriteLine("Start ROS");
            ROS.Init(new string[0], "ActionClient");

            NodeHandle clientNodeHandle = ROS.GlobalNodeHandle;

            Console.WriteLine("Create client");
            actionClient = new ActionClient<Messages.actionlib.TestGoal, Messages.actionlib.TestResult,
                Messages.actionlib.TestFeedback>("test_action", clientNodeHandle);

            Console.WriteLine("Wait for client and server to negotiate connection");
            bool started = actionClient.WaitForActionServerToStart(new TimeSpan(0, 0, 3));

            TestParams.Add(new TestParameters("Reject Goal", GoalStatus.REJECTED, 0, false, null));
            TestParams.Add(new TestParameters("Cancel not yet accepted goal", GoalStatus.RECALLED, 0, true, null));
            TestParams.Add(new TestParameters("Cancel accepted goal", GoalStatus.PREEMPTED, 0, true, null));
            TestParams.Add(new TestParameters("Abort Goal", GoalStatus.ABORTED, 20, false, null));
            TestParams.Add(new TestParameters("Get Result 123", GoalStatus.SUCCEEDED, 20, false, 123));


            bool testError = false;
            if (started)
            {
                Console.WriteLine("Server connected, start tests");
                foreach(var parameter in TestParams)
                {
                    if (!TestCase(parameter.Name, parameter.ExpectedState, parameter.ExpectedFeedback,
                        parameter.CancelGoal, parameter.ExpectedGoal))
                    {
                        testError = true;
                        break;
                    }
                    Thread.Sleep(1000);
                }
            }
            else
            {
                Logger.LogError("Could not connect to server");
            }

            if (testError)
            {
                Logger.LogError("Errors ocured during testing!");
            } else
            {
                Logger.LogInformation("Testbed completed successfully");
            }

            Console.WriteLine("All done, press any key to exit");
            while (!Console.KeyAvailable)
            {
                Thread.Sleep(1);
            }
            actionClient.Shutdown();
            clientNodeHandle.shutdown();
            ROS.shutdown();
        }


        private bool TestCase(string testName, byte successState, int expectedNumberOfFeedback, bool cancelGoal, int? expectedGoal)
        {
            testState = TestState.Unknown;
            receivedFeedback = 0;
            Console.WriteLine("");
            Console.WriteLine(testName);

            var goal = new Messages.actionlib.TestGoal();
            goal.goal = 42;

            var clientHandle = actionClient.SendGoal(goal,
                (goalHandle) => {
                    Console.WriteLine($"Transition: {goalHandle.State} {goalHandle.LatestGoalStatus.status} {goalHandle.Result?.result}");

                    if (goalHandle.State == CommunicationState.DONE)
                    {
                        if (goalHandle.LatestGoalStatus.status == successState)
                        {
                            if (expectedGoal == null)
                            {
                                testState = TestState.Succeeded;
                            }
                            else
                            {
                                if (goalHandle.Result?.result == expectedGoal)
                                {
                                    Console.WriteLine($"Received expected Goal Result: {goalHandle.Result?.result}");
                                    testState = TestState.Succeeded;
                                }
                                else
                                {
                                    Console.WriteLine($"Received unexpected Goal Result: {goalHandle.Result?.result}");
                                    testState = TestState.Failed;
                                }
                            }
                        }
                        else
                        {
                            testState = TestState.Failed;
                        }
                    }
                },
                (goalHandle, feedback) => {
                    Console.WriteLine($"Feedback {feedback.Feedback.feedback} {feedback.GoalStatus.status}");
                    receivedFeedback += 1;
                }
            );
            Console.WriteLine($"Sent goal {clientHandle.Id}");

            if (cancelGoal)
            {
                Thread.Sleep(500);
                Console.WriteLine("Canceling goal");
                actionClient.CancelPublisher.publish(clientHandle.Goal.GoalId);
            }

            WaitForSuccessWithTimeOut(5, expectedNumberOfFeedback, expectedGoal);
            var testResult = (testState == TestState.Succeeded);
            var feedbackResult = (receivedFeedback == expectedNumberOfFeedback);
            var result = testResult && feedbackResult;
            Console.WriteLine(result ? "SUCCESS" : $"FAIL transistion: {testResult} feedback: {feedbackResult}: {receivedFeedback}/{expectedNumberOfFeedback}");
            Console.WriteLine("");
            return result;
        }


        private void WaitForSuccessWithTimeOut(int timeOutInSeconds, int expectedFeedback, int? expectedGoal)
        {
            var timeSpan = new TimeSpan(0, 0, timeOutInSeconds);
            var start = DateTime.Now;
            while (DateTime.Now - start < timeSpan)
            {
                if ((testState == TestState.Succeeded) && (receivedFeedback == expectedFeedback))
                {
                    break;
                }
                Thread.Sleep(1);
            }
        }


        private enum TestState
        {
            Unknown,
            Succeeded,
            Failed
        }

    }

    public class TestParameters
    {
        public String Name { get; set; }
        public int ExpectedFeedback { get; set; }
        public byte ExpectedState { get; set; }
        public bool CancelGoal { get; set; }
        public int? ExpectedGoal { get; set; }

        public TestParameters(string name, byte state, int feedback, bool cancel, int? goal)
        {
            Name = name;
            ExpectedState = state;
            ExpectedFeedback = feedback;
            CancelGoal = cancel;
            ExpectedGoal = goal;
        }
    }
}