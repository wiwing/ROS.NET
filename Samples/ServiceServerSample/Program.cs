using System;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Text;

using Messages;
using Messages.custom_msgs;
using Uml.Robotics.Ros;
using Int32 = Messages.std_msgs.Int32;
using String = Messages.std_msgs.String;
using m = Messages.std_msgs;
using gm = Messages.geometry_msgs;
using nm = Messages.nav_msgs;
using sm = Messages.sensor_msgs;
//using d = System.Drawing;
using cm = Messages.custom_msgs;
using tf = Messages.tf;
using Messages.roscpp_tutorials;

namespace ServiceServerSample
{

    class Program
    {
        private ILogger Logger { get; } = ApplicationLogging.CreateLogger<Program>();
        private static bool addition(TwoInts.Request req, ref TwoInts.Response resp)
        {
            Logger.LogInformation("[ServiceServerSample] addition callback");
            resp.sum = req.a + req.b;
            Logger.LogInformation(req);
            Logger.LogInformation(resp.sum);
            return true;
        }
        static void Main(string[] args)
        {
            NodeHandle nodeHandle;
            string NODE_NAME = "ServiceServerTest";
            ServiceServer server;
            ROS.Init(new string[0], NODE_NAME);

            nodeHandle = new NodeHandle();

            server = nodeHandle.advertiseService<TwoInts.Request, TwoInts.Response>("/add_two_ints", addition);
            while (ROS.ok && server.IsValid)
            {
                Thread.Sleep(10);
            }
            
            ROS.shutdown();
            ROS.waitForShutdown();
        }
    }
}
