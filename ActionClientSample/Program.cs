using System;
using System.Threading;

using Uml.Robotics.Ros;
using Uml.Robotics.Ros.ActionLib;
using Messages.xamlamoveit;
using Messages.control_msgs;

namespace ActionClientSample
{
    class Program
    {
        /*static void Main(string[] args)
        {
            Console.WriteLine("Start ROS");
            ROS.Init(new string[0], "ActionClient");
            NodeHandle clientNodeHandle = new NodeHandle();

            Console.WriteLine("Create client");
            var actionClient = new ActionClient<SingleJointPositionGoal, SingleJointPositionResult,
                SingleJointPositionFeedback>("ActionTest", clientNodeHandle);

            Console.WriteLine("Wait for client and server to negotiate connection");
            bool started = actionClient.WaitForActionServerToStart(new TimeSpan(0, 0, 10));


            if (started || true)
            {
                var goal = new SingleJointPositionGoal();
                goal.position = 12.0;
                goal.max_velocity = 42.0;

                Console.WriteLine("Send goal from client");
                var transition = false;
                actionClient.SendGoal(goal,
                    (goalHandle) => { Console.WriteLine($"Transition: {goalHandle}"); transition = true; },
                    (goalHandle, feedback) => { Console.WriteLine($"Feedback: {feedback}"); });


                Console.WriteLine("Wait for action client to receive transition");
                while (!transition)
                {
                    Thread.Sleep(1);
                }
            } else
            {
                Console.WriteLine("Negotiation with server failed!");
            }
        }*/


        static void Main(string[] args)
        {
            Console.WriteLine("Start ROS");
            ROS.Init(new string[0], "ActionClient");

            NodeHandle clientNodeHandle = new NodeHandle();

            Console.WriteLine("Create client");
            var actionClient = new ActionClient<moveJGoal, moveJResult,
                moveJFeedback>("moveJ_action", clientNodeHandle);

            Console.WriteLine("Wait for client and server to negotiate connection");
            bool started = actionClient.WaitForActionServerToStart(new TimeSpan(0, 0, 10));


            if (started || true)
            {
                var goal = new moveJGoal();
                goal.group_name.data = "manipulator";
                goal.goal.positions = new double[] {1.2658,
                                        -1.9922,
                                        2.3327,
                                        -3.0303,
                                        -1.6643,
                                        2.3804
                };

                Console.WriteLine("Send goal from client");
                var transition = false;
                actionClient.SendGoal(goal,
                    (goalHandle) => { Console.WriteLine($"Transition: {goalHandle.State.ToString()}"); transition = true; },
                    (goalHandle, feedback) => { Console.WriteLine($"Feedback: {feedback}"); });


                Console.WriteLine("Wait for action client to receive transition");
                while (!transition)
                {
                    Thread.Sleep(1);
                }
            }
            else
            {
                Console.WriteLine("Negotiation with server failed!");
            }
        }
    }
}