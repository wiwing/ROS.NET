using System;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Text;

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

namespace ServiceClientSample2
{
    class Program
    {
        static void Main(string[] args)
        {
            NodeHandle nodeHandle;
            string NODE_NAME = "ServiceClientTest";
            
            ROS.Init(new string[0], NODE_NAME+DateTime.Now.Ticks);

            nodeHandle = new NodeHandle();
            ServiceClient<TwoInts.Request, TwoInts.Response> client = nodeHandle.serviceClient<TwoInts.Request, TwoInts.Response>("/add_two_ints");
            while (ROS.ok)
            {
                
                Random r = new Random();
                TwoInts.Request req = new TwoInts.Request() { a = r.Next(100), b = r.Next(100) };
                TwoInts.Response resp = new TwoInts.Response();
                DateTime before = DateTime.Now;
                Console.WriteLine("Before client call");
                bool res = client.call(req, ref resp);
                Console.WriteLine("After client call");
                TimeSpan dif = DateTime.Now.Subtract(before);

                string str = "";
                if (res)
                    str = "" + req.a + " + " + req.b + " = " + resp.sum + "\n";
                else
                    str = "call failed after\n";
                
                str += Math.Round(dif.TotalMilliseconds,2) + " ms";
                Console.WriteLine(str);
                Thread.Sleep(1000);
            }
            
            ROS.shutdown();
            ROS.waitForShutdown();
            
        }
    }
}
