using System;
using System.Threading;
using Microsoft.Extensions.Logging;

using Messages.rosgraph_msgs;

namespace Uml.Robotics.Ros
{
    public class SimTime
    {
        private static ILogger Logger { get; } = ApplicationLogging.CreateLogger<SimTime>();
        public delegate void SimTimeDelegate(TimeSpan ts);

        private static Lazy<SimTime> _instance = new Lazy<SimTime>(LazyThreadSafetyMode.ExecutionAndPublication);

        public static SimTime instance
        {
            get { return _instance.Value; }
        }

        private bool checkedSimTime;
        private NodeHandle nh;
        private bool simTime;
        private Subscriber<Clock> simTimeSubscriber;

        public SimTime()
        {
            new Thread(() =>
            {
                try
                {
                    while (!ROS.isStarted() && !ROS.shutting_down)
                    {
                        Thread.Sleep(100);
                    }
                    if (!ROS.shutting_down)
                    {
                        nh = new NodeHandle();
                        simTimeSubscriber = nh.subscribe<Clock>("/clock", 1, SimTimeCallback);
                    }
                }
                catch(Exception e)
                {
                    Logger.LogError("Caught exception: " + e.Message);
                }
            }).Start();
        }

        public bool IsTimeSimulated
        {
            get { return simTime; }
        }

        public event SimTimeDelegate SimTimeEvent;

        private void SimTimeCallback(Clock time)
        {
            if (!checkedSimTime)
            {
                if (Param.get("/use_sim_time", ref simTime))
                {
                    checkedSimTime = true;
                }
            }
            if (simTime && SimTimeEvent != null)
                SimTimeEvent.Invoke(TimeSpan.FromMilliseconds(time.clock.data.sec*1000.0 + (time.clock.data.nsec/100000000.0)));
        }
    }
}
