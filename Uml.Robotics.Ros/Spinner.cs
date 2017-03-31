using Microsoft.Extensions.Logging;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Uml.Robotics.Ros
{
    public class SingleThreadSpinner
    {
        ICallbackQueue callbackQueue;
        private ILogger Logger { get; } = ApplicationLogging.CreateLogger<SingleThreadSpinner>();

        public SingleThreadSpinner(ICallbackQueue callbackQueue)
        {
            this.callbackQueue = callbackQueue;
        }


        public void Spin()
        {
            SpinCancelable(null);
            Logger.LogCritical("CallbackQueue thread broke out! This only can happen if ROS.ok is false.");
        }


        public void SpinCancelable(CancellationToken? token)
        {
            TimeSpan wallDuration = new TimeSpan(0, 0, 0, 0, ROS.WallDuration);
            Logger.LogInformation("Start spinning");
            while (ROS.ok)
            {
                DateTime begin = DateTime.Now;
                if (!callbackQueue.CallAvailable(ROS.WallDuration) || (token?.IsCancellationRequested ?? false))
                    break;
                DateTime end = DateTime.Now;
                if (wallDuration.Subtract(end.Subtract(begin)).Ticks > 0)
                    Thread.Sleep(wallDuration.Subtract(end.Subtract(begin)));
            }
        }


        public void SpinOnce()
        {
            callbackQueue.CallAvailable(ROS.WallDuration);
        }
    }


    /*public class MultiThreadSpinner
    {
        public void Dispose()
        {
            throw new NotImplementedException();
        }

        public void Spin()
        {
            throw new NotImplementedException();
        }

        public void SpinOnce()
        {
            throw new NotImplementedException();
        }
    }*/


    public class AsyncSpinner : IDisposable
    {
        private ICallbackQueue callbackQueue;
        private Task spinTask;
        private CancellationTokenSource tokenSource = new CancellationTokenSource();
        private CancellationToken token;

        public AsyncSpinner(ICallbackQueue callbackQueue)
        {
            this.callbackQueue = callbackQueue;
        }

        public void Dispose()
        {
            tokenSource.Dispose();
        }

        public void Start()
        {
            spinTask = Task.Factory.StartNew(() =>
            {
                token = tokenSource.Token;
                var spinner = new SingleThreadSpinner(callbackQueue);
                spinner.SpinCancelable(token);
            });
        }

        public void Stop()
        {
            if (spinTask != null)
            {
                tokenSource.Cancel();
                spinTask = null;
            }
        }
    }
}