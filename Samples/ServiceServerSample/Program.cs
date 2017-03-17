using System;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Text;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Console;


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
        private static ILogger Logger {get; set;}

        private static bool addition(TwoInts.Request req, ref TwoInts.Response resp)
        {
            Logger.LogInformation("[ServiceServerSample] addition callback");
            resp.sum = req.a + req.b;
            Logger.LogInformation(req.ToString());
            Logger.LogInformation(resp.sum.ToString());
            return true;
        }
        static void Main(string[] args)
        {
            // Setup the logging system
            var loggerFactory = new LoggerFactory();
            loggerFactory.AddProvider(
                new ConsoleLoggerProvider(
                    (string text, LogLevel logLevel) => { return logLevel >= LogLevel.Information;}, true)
            );
            Logger = ApplicationLogging.CreateLogger(nameof(Main));
            ROS.SetLoggerFactory(loggerFactory);

            NodeHandle nodeHandle;
            string NODE_NAME = "ServiceServerTest";
            ServiceServer server;

            try
            {
                ROS.Init(new string[0], NODE_NAME);
            }
            catch (RosException e)
            {
                Logger.LogCritical("ROS.Init failed, shutting down: {0}", e.Message);
                ROS.shutdown();
                ROS.waitForShutdown();
                return;
            }

            try
            {
                nodeHandle = new NodeHandle();
                server = nodeHandle.advertiseService<TwoInts.Request, TwoInts.Response>("/add_two_ints", addition);
                while (ROS.ok && server.IsValid)
                {
                    Thread.Sleep(10);
                }
            }
            catch (RosException e)
            {
                Logger.LogCritical("Shutting down: {0}", e.Message);
            }


            ROS.shutdown();
            ROS.waitForShutdown();
        }
    }
}
