using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading;
using Microsoft.Extensions.Logging;

namespace Uml.Robotics.Ros
{
    public class PollSignal : IDisposable
    {
        private ILogger Logger { get; } = ApplicationLogging.CreateLogger<PollSignal>();
        public MethodInfo Method;
        public object Target;
        public delegate void PollSignalFunc();
        private Thread thread;
        private Action _op;
        private AutoResetEvent _go = new AutoResetEvent(false);
        private bool disposed = false;

        /// <summary>
        /// Sets this Poll_Signal's periodic operation, AND makes it be auto-polled by PollManager.
        /// </summary>
        public Action Op
        {
            get
            {
                return _op;
            }

            set
            {
                ManualOp = value;
                if (value != null)
                {
                    SignalEvent += signal;
                }
                else
                {
                    SignalEvent -= signal;
                }
            }
        }

        /// <summary>
        /// Sets this Poll_Signal's operation, without making it be auto-polled by PollManager
        /// </summary>
        public Action ManualOp
        {
            get
            {
                return _op;
            }
            set
            {
                try
                {
                    SignalEvent -= signal;
                }
                catch { }

               Method = value.GetMethodInfo();
               Target = value.Target;
                _op = value;
            }
        }

        internal static event PollSignalFunc SignalEvent;

        public PollSignal(Action psf)
        {
            if (psf != null)
            {
                Op = psf;
            }
            thread = new Thread(threadFunc) { IsBackground = true };
            thread.Start();
        }

        internal void signal()
        {
            _go.Set();
        }

        private void threadFunc()
        {
            while (ROS.ok && !disposed)
            {
                _go.WaitOne();
                if (ROS.ok && !disposed)
                    Op();
            }
            thread = null;
        }

        internal static void Signal()
        {
            if (SignalEvent != null) SignalEvent.Invoke();
        }

        public void Dispose()
        {
            SignalEvent -= signal;
            disposed = true;
            do
            {
                signal();
            } while (thread != null && !thread.Join(1));
        }
    }

    public class PollManager
    {
        private ILogger Logger { get; } = ApplicationLogging.CreateLogger<PollManager>();

        public static PollManager Instance
        {
            get { return _instance.Value; }
        }

        public PollSet poll_set;
        public bool shutting_down;
        public object signal_mutex = new object();
        private static Lazy<PollManager> _instance = new Lazy<PollManager>(LazyThreadSafetyMode.ExecutionAndPublication);
        private List<PollSignal> signals = new List<PollSignal>();
        public TcpTransport tcpserver_transport;
        private Thread thread;


        public static void Terminate()
        {
            Instance.Shutdown();
            _instance = new Lazy<PollManager>(LazyThreadSafetyMode.ExecutionAndPublication);
        }


        public PollManager()
        {
            poll_set = new PollSet();
        }

        public void addPollThreadListener(Action poll)
        {
            Logger.LogDebug("Adding pollthreadlistener " + poll.Target + ":" + poll.GetMethodInfo().Name);
            lock (signal_mutex)
            {
                signals.Add(new PollSignal(poll));
            }
            signal();
        }

        private void signal()
        {
            PollSignal.Signal();
        }

        public void removePollThreadListener(Action poll)
        {
            lock (signal_mutex)
            {
                signals.RemoveAll((s) => s.Op == poll);
            }
            signal();
        }

        private void threadFunc()
        {
            while (!shutting_down)
            {
                signal();
                Thread.Sleep(ROS.WallDuration);
                if (shutting_down)
                    return;
            }
            Logger.LogDebug("PollManager thread finished");
        }


        public void Start()
        {
            if (thread == null)
            {
                shutting_down = false;
                thread = new Thread(threadFunc);
                thread.Start();
            }
        }


        public void Shutdown()
        {
            if (thread != null && !shutting_down)
            {
                shutting_down = true;
                poll_set.Dispose();
                poll_set = null;
                signals.Clear();
                if (!thread.Join(2000))
                {
                    Logger.LogWarning("thread.Join timed out.");
                    // AKo: ## fixme .NET Core has no Abort() on Thread obj
                    //try
                    //{
                    //    thread.Abort();
                    //}
                    //catch (Exception ex)
                    //{
                    //}
                }
                thread = null;
            }
        }
    }
}
