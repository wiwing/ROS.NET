using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using Uml.Robotics.Ros;
using System.Collections.ObjectModel;
using System;

namespace Uml.Robotics.Samples
{
    public class Program
    {
        static void Main(string[] args)
        {
            string ignoredStrings = null;
            if (args.Length > 0)
            {
                ignoredStrings = args[0];
            }

            var rosoutDebug = new RosoutDebug();

            if (ignoredStrings != null)
            {
                Console.WriteLine("Using ignoredStrings: " + ignoredStrings);
                rosoutDebug.IgnoredStrings = ignoredStrings;
            }
        }
    }


    public class RosoutDebug
    {
        ObservableCollection<RosoutString> rosoutdata = new ObservableCollection<RosoutString>();
        Subscriber<Messages.rosgraph_msgs.Log> sub;
        private NodeHandle nh;
        private Thread waitforinit;

        // Right now, these are split from a semicolon-delimited string, and matching is REALLY DUMB...
        // Just a containment check.
        private List<string> ignoredStrings = new List<string>();


        public RosoutDebug()
        {
            ROS.Init(new string[0], "RosoutDebug");
            ROS.WaitForMaster();
            nh = new NodeHandle();
            Init();
        }


        public RosoutDebug(NodeHandle n, string IgnoredStrings)
            : this()
        {
            nh = n;
            this.IgnoredStrings = IgnoredStrings;
        }



        /// <summary>
        /// A semicolon-delimited list of substrings that, when found in a concatenation of any rosout msgs fields,
        /// will not display that message
        /// </summary>
        public string IgnoredStrings
        {
            get { return ignoredStrings.ToString(); }
            set
            {
                if (Process.GetCurrentProcess().ProcessName == "devenv")
                    return;
                ignoredStrings.Clear();
                ignoredStrings.AddRange(value.Split(';'));
                Init();
            }
        }


        public void Shutdown()
        {
            if (sub != null)
            {
                sub.shutdown();
                sub = null;
            }
            if (nh != null)
            {
                nh.shutdown();
                nh = null;
            }
        }


        private void Init()
        {
            lock (this)
            {
                if (!ROS.isStarted())
                {
                    if (waitforinit == null)
                    {
                        string workaround = IgnoredStrings;
                        waitforinit = new Thread(() => Waitfunc(workaround));
                    }
                    if (!waitforinit.IsAlive)
                    {
                        waitforinit.Start();
                    }
                }
                else
                    SetupIgnore(IgnoredStrings);
            }
        }


        private void Waitfunc(string ignored)
        {
            while (!ROS.isStarted())
            {
                Thread.Sleep(100);
            }
            SetupIgnore(ignored);
        }


        private void SetupIgnore(string ignored)
        {
            if (Process.GetCurrentProcess().ProcessName == "devenv")
                return;
            if (nh == null)
                nh = new NodeHandle();
            if (sub == null)
                sub = nh.subscribe<Messages.rosgraph_msgs.Log>("/rosout_agg", 100, Callback);
        }


        private void Callback(Messages.rosgraph_msgs.Log msg)
        {
            string teststring = string.Format("{0}\n{1}\n{2}\n{3}\n{4}\n{5}", msg.level, msg.msg, msg.name, msg.file,
                msg.function, msg.line
            );
            if (ignoredStrings.Count > 0 && ignoredStrings.Any(teststring.Contains))
            {
                return;
            }

            RosoutString rss = new RosoutString((1.0 * msg.header.stamp.data.sec +
                (1.0 * msg.header.stamp.data.nsec) / 1000000000.0),
                msg.level,
                msg.msg,
                msg.name,
                msg.file,
                msg.function,
                "" + msg.line
            );

            rosoutdata.Add(rss);

            if (rosoutdata.Count > 1000)
            {
                rosoutdata.RemoveAt(0);
            }
        }
    }


    public class RosoutString
    {
        //converts the int warning value from msg to a meaningful string
        private string ConvertVerbosityLevel(int level)
        {
            switch (level)
            {
                case 1:
                    return "DEBUG";
                case 2:
                    return "INFO";
                case 4:
                    return "WARN";
                case 8:
                    return "ERROR";
                case 16:
                    return "FATAL";
                default:
                    return "" + level;
            }
        }

        public string Timestamp { get; set; }
        public string Level { get; set; }
        public string Msgdata { get; set; }
        public string Msgname { get; set; }
        public string Filename { get; set; }
        public string Functionname { get; set; }
        public string Lineno { get; set; }
        public double Stamp  { get; set; }
        public int LevelNr = 0;


        public RosoutString(double stamp, int level, string data, string name, string filename, string function,
            string lineno)
        {
            this.Stamp = stamp;
            this.Timestamp = "" + stamp;
            this.LevelNr = level;
            this.Level = ConvertVerbosityLevel(level);
            this.Msgdata = data;
            this.Msgname = name;
            this.Filename = filename;
            this.Functionname = function;
            this.Lineno = lineno;
        }
    }
}