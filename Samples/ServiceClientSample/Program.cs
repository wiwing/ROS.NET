using System;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Text;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Console;

using Messages;
using Messages.custom_msgs;
using Messages.roscpp_tutorials;
using Uml.Robotics.Ros;
using Int32 = Messages.std_msgs.Int32;
using String = Messages.std_msgs.String;
using m = Messages.std_msgs;
using gm = Messages.geometry_msgs;
using nm = Messages.nav_msgs;
using sm = Messages.sensor_msgs;
using cm = Messages.custom_msgs;
using tf = Messages.tf;

namespace ServiceClientSample
{
    class Program
    {
        private static ILogger Logger {get; set;}
        static void Main(string[] args)
        {
            string NODE_NAME = "ServiceClientTest";

            var loggerFactory = new LoggerFactory();
            loggerFactory.AddProvider(
                new ConsoleLoggerProvider(
                    (string text, LogLevel logLevel) => { return logLevel >= LogLevel.Debug;}, true)
            );
            Logger = ApplicationLogging.CreateLogger(nameof(Main));
            ROS.SetLoggerFactory(loggerFactory);


            try
            {
                ROS.Init(new string[0], NODE_NAME+DateTime.Now.Ticks);
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
                var nodeHandle = new NodeHandle();
                while (ROS.ok)
                {

                    Random r = new Random();
                    TwoInts.Request req = new TwoInts.Request() { a = r.Next(100), b = r.Next(100) };
                    TwoInts.Response resp = new TwoInts.Response();
                    DateTime before = DateTime.Now;
                    bool res = nodeHandle.serviceClient<TwoInts.Request, TwoInts.Response>("/add_two_ints").call(req, ref resp);
                    TimeSpan dif = DateTime.Now.Subtract(before);

                    string str = "";
                    if (res)
                        str = "" + req.a + " + " + req.b + " = " + resp.sum + "\n";
                    else
                        str = "call failed after ";

                    str += Math.Round(dif.TotalMilliseconds,2) + " ms";
                    Logger.LogInformation(str);
                    Thread.Sleep(1000);
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
