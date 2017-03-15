using System;
using System.Diagnostics;
using System.IO;
using System.Threading;
using Messages;
using Messages.custom_msgs;
using Uml.Robotics.Ros;
using moveItMsgs = Messages.moveit_msgs;
using gm = Messages.geometry_msgs;
using ms = Messages.std_msgs;
using System.Text;

namespace MoveitMsgTests
{
    public class Program
    {
        private static void GetIK( NodeHandle node, gm.PoseStamped poseStamped, string group, double ikTimeout=1.0, int ikAttempts=0, bool avoidCollisions = false)
        {
            Console.WriteLine("GetIK");
            moveItMsgs.GetPositionIK.Request req = new moveItMsgs.GetPositionIK.Request();
            
            req.ik_request = new moveItMsgs.PositionIKRequest();
            Console.WriteLine(req.ik_request);
            req.ik_request.group_name = group;
            req.ik_request.pose_stamped = poseStamped;
            req.ik_request.timeout.data = new TimeData(1,0); // one second
            req.ik_request.attempts = ikAttempts;
            req.ik_request.avoid_collisions = avoidCollisions;
            
            moveItMsgs.GetPositionIK.Response resp = new moveItMsgs.GetPositionIK.Response();
            DateTime before = DateTime.Now;
            Console.WriteLine("node.serviceClient");
            bool res = node.serviceClient<moveItMsgs.GetPositionIK.Request, moveItMsgs.GetPositionIK.Response>("/compute_ik").call(req, ref resp);
            if (res)
            {
                Console.WriteLine("got result");
                Console.WriteLine(resp.error_code.val);
            }else
            {
                Console.WriteLine("FAILED to receive respond from service");
            }
        }


        private static double Deg2Rad(double angle)
        {
            return Math.PI * angle / 180.0;
        }


        public static gm.Quaternion SetQuaternionFromRPY(double roll, double pitch,double yaw)
        {
            double halfRoll = roll / 2;
            double halfPitch = pitch / 2;
            double halfYaw = yaw / 2;

            double sinRoll2 = Math.Sin(halfRoll);
            double sinPitch2 = Math.Sin(halfPitch);
            double sinYaw2 = Math.Sin(halfYaw);

            double cosRoll2 = Math.Cos(halfRoll);
            double cosPitch2 = Math.Cos(halfPitch);
            double cosYaw2 = Math.Cos(halfYaw);
            gm.Quaternion q = new gm.Quaternion();

            q.x = cosPitch2*sinRoll2*cosYaw2 - sinPitch2*cosRoll2*sinYaw2;
            q.y = cosPitch2*sinRoll2*sinYaw2 + sinPitch2*cosRoll2*cosYaw2;
            q.z = cosPitch2*cosRoll2*sinYaw2 - sinPitch2*sinRoll2*cosYaw2;
            q.w = cosPitch2*cosRoll2*cosYaw2 + sinPitch2*sinRoll2*sinYaw2;

            return q;
        }


        private static gm.PoseStamped SetPose (double x, double y, double z, double roll, double pitch, double yaw, string frameID)
        {
            gm.PoseStamped ps = new gm.PoseStamped();
            Console.WriteLine(ps.pose);
            ps.pose.position.x = x;
            ps.pose.position.y = y;
            ps.pose.position.z = z;
            ps.pose.orientation = SetQuaternionFromRPY(Deg2Rad(roll), Deg2Rad(pitch) ,Deg2Rad(yaw));
            ps.header.frame_id = frameID;
            return ps;
        }
        static void Main(string[] args)
        {
            ROS.Init(args, "MoveitTest");
            NodeHandle node = new NodeHandle();
            gm.PoseStamped result = SetPose(-0.1,0.1,0.2,0.0,180.0,0.0,"");
            Console.WriteLine("result");
            Console.WriteLine(result);
            GetIK(node, result, "endeffector");
            ROS.waitForShutdown();
        }
    }
}
