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
        private static void getIK( NodeHandle node, gm.PoseStamped poseStamped, string group, double ikTimeout=1.0, int ikAttempts=0, bool avoidCollisions = false)
        {
            Console.WriteLine("getIK");
            moveItMsgs.GetPositionIK.Request req = new moveItMsgs.GetPositionIK.Request();
            
            req.ik_request = new moveItMsgs.PositionIKRequest();
            Console.WriteLine(req.ik_request);
            req.ik_request.group_name = group;
            req.ik_request.pose_stamped = poseStamped;
            req.ik_request.timeout.data = new TimeData(0,10000);
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
        private static gm.PoseStamped setPose (double x, double y, double z, double roll, double pitch, double yaw, string frame_id)
        {
            gm.PoseStamped ps = new gm.PoseStamped();
            //ps.pose = new gm.Pose();
            //ps.pose.position = new gm.Point();
            //ps.header = new ms.Header();
            Console.WriteLine(ps.pose);
            ps.pose.position.x = x;
            ps.pose.position.y = y;
            ps.pose.position.z = z;
            ps.pose.orientation.x = 0.0;
            ps.pose.orientation.y = 1.0;
            ps.pose.orientation.z = 0.0;
            ps.pose.orientation.w = 0;
            ps.header.frame_id = frame_id;
            return ps;
        }
        static void Main(string[] args)
        {
            ROS.Init(args, "MoveitTest");
            NodeHandle node = new NodeHandle();
            gm.PoseStamped result = setPose(0.1,0.2,0.3,0.0,0.0,0.0,"base_link");
            Console.WriteLine("result");
            Console.WriteLine(result);
            getIK(node, result, "endeffector");
            ROS.waitForShutdown();
        }
    }
}
