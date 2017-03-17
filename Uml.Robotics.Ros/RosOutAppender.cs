using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using Messages;
using Messages.rosgraph_msgs;
using m = Messages.std_msgs;
using gm = Messages.geometry_msgs;
using nm = Messages.nav_msgs;

namespace Uml.Robotics.Ros
{
    internal class CallerInfo
    {
        public string MemberName { get; set; }
        public string FilePath { get; set; }
        public int LineNumber { get; set; }
    }

    public class RosOutAppender
    {
        private static Lazy<RosOutAppender> _instance = new Lazy<RosOutAppender>(LazyThreadSafetyMode.ExecutionAndPublication);

        public static RosOutAppender Instance
        {
            get { return _instance.Value; }
        }

        internal enum ROSOUT_LEVEL
        {
            DEBUG = 1,
            INFO = 2,
            WARN = 4,
            ERROR = 8,
            FATAL = 16
        }

        private Queue<Log> log_queue = new Queue<Log>();
        private Thread publish_thread;
        private bool shutting_down;
        private Publisher<Log> publisher;

        public RosOutAppender()
        {
            publish_thread = new Thread(logThread) { IsBackground = true };
        }

        public bool started
        {
            get { return publish_thread != null && (publish_thread.ThreadState == System.Threading.ThreadState.Running || publish_thread.ThreadState == System.Threading.ThreadState.Background); }
        }

        public void start()
        {
            if (!shutting_down && !started)
            {
                if (publisher == null)
                    publisher = ROS.GlobalNodeHandle.advertise<Log>("/rosout", 0);
                publish_thread.Start();
            }
        }

        public void shutdown()
        {
            shutting_down = true;
            if(started)
            {
                publish_thread.Join();
            }
            if (publisher != null)
            {
                publisher.shutdown();
                publisher = null;
            }
        }

        internal void Append(string message, ROSOUT_LEVEL level, CallerInfo callerInfo)
        {
            Log logMessage = new Log
            {
                msg = message,
                name = this_node.Name,
                file = callerInfo.FilePath,
                function = callerInfo.MemberName,
                line = (uint)callerInfo.LineNumber,
                level = ((byte) ((int)level)),
                header = new m.Header() { stamp = ROS.GetTime() }
            };
            TopicManager.Instance.getAdvertisedTopics(out logMessage.topics);
            lock (log_queue)
                log_queue.Enqueue(logMessage);
        }

        private void logThread()
        {
            Queue<Log> localqueue;
            while (!shutting_down)
            {
                lock (log_queue)
                {
                    localqueue = new Queue<Log>(log_queue);
                    log_queue.Clear();
                }
                while (!shutting_down && localqueue.Count > 0)
                {
                    publisher.publish(localqueue.Dequeue());
                }
                if (shutting_down) return;
                Thread.Sleep(100);
            }
        }
    }
}
