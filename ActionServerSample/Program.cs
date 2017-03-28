using System;
using System.Threading;

using Uml.Robotics.Ros;
using Uml.Robotics.Ros.ActionLib;
using Messages.control_msgs;

namespace ActionServerSample
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("Start ROS");
            ROS.Init(new string[0], "ActionServer");
            NodeHandle serverNodeHandle = new NodeHandle();
            Console.WriteLine("Create server");
            var actionServer = new ActionServer<SingleJointPositionGoal, SingleJointPositionResult,
                SingleJointPositionFeedback>(serverNodeHandle, "ActionTest");
            Console.WriteLine("Start Server");
            actionServer.Start();

            bool goalRegistered = false;
            actionServer.RegisterGoalCallback((goalHandle) =>
            {
                Console.WriteLine($"Goal registered callback. Max velo: {goalHandle.Goal.max_velocity}");
                var fb = new SingleJointPositionFeedback();
                fb.velocity = 10.0;
                goalHandle.PublishFeedback(fb);
                Thread.Sleep(100);
                goalHandle.SetGoalStatus(Messages.actionlib_msgs.GoalStatus.SUCCEEDED, "done");
                goalRegistered = true;
            });

            Console.WriteLine("Wait for action server receiving the goal");
            while (!goalRegistered)
            {
                Thread.Sleep(1);
            }

            serverNodeHandle.shutdown();
            ROS.shutdown();
        }
    }
}