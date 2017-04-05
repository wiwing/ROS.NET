using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Threading;

namespace Uml.Robotics.Ros
{
    public class CallbackQueue : ICallbackQueue
    {
        private ILogger Logger { get; } = ApplicationLogging.CreateLogger<CallbackQueue>();
        private int count;
        private int calling;
        private bool enabled;
        private Dictionary<UInt64, IDInfo> idInfo = new Dictionary<UInt64, IDInfo>();
        private object idInfoMutex = new object();
        private AutoResetEvent sem = new AutoResetEvent(false);
        private object mutex = new object();
        private List<CallbackInfo> callbacks = new List<CallbackInfo>();
        private TLS tls;


        public CallbackQueue()
        {
            enabled = true;
        }

        public bool IsEmpty
        {
            get { return count == 0; }
        }

        public bool IsEnabled
        {
            get { return enabled; }
        }

        public void AddCallback(CallbackInterface callback)
        {
            AddCallback(callback, callback.Uid);
        }

        public void AddCallback(CallbackInterface cb, UInt64 owner_id)
        {
            CallbackInfo info = new CallbackInfo { Callback = cb, RemovalId = owner_id };
            //Logger.LogDebug($"CallbackQueue@{cbthread.ManagedThreadId}: Add callback owner: {owner_id} {cb.ToString()}");

            lock (mutex)
            {
                if (!enabled)
                    return;
                callbacks.Add(info);
                //Logger.LogDebug($"CallbackQueue@{cbthread.ManagedThreadId}: Added");
                count++;
            }
            lock (idInfoMutex)
            {
                if (!idInfo.ContainsKey(owner_id))
                {
                    idInfo.Add(owner_id, new IDInfo { calling_rw_mutex = new object(), id = owner_id });
                }
            }
            NotifyOne();
        }

        public bool CallAvailable()
        {
            return CallAvailable(ROS.WallDuration);
        }

        public bool CallAvailable(int timeout)
        {
            SetupTls();
            int called = 0;
            lock (mutex)
            {
                if (!enabled) return false;
            }
            if (count == 0 && timeout != 0)
            {
                if (!sem.WaitOne(timeout))
                    return true;
            }
            //Logger.LogDebug($"CallbackQueue@{cbthread.ManagedThreadId}: Enqueue TLS");
            lock (mutex)
            {
                if (count == 0)
                    return true;
                if (!enabled)
                    return false;
                callbacks.ForEach(cbi => tls.Enqueue(cbi));
                callbacks.Clear();
                count = 0;
                calling += tls.Count;
            }
            //Logger.LogDebug($"CallbackQueue@{cbthread.ManagedThreadId}: TLS count {tls.Count}");
            while (tls.Count > 0 && ROS.ok)
            {
                //Logger.LogDebug($"CallbackQueue@{cbthread.ManagedThreadId}: call {tls.head.Callback.ToString()}");
                if (CallOne(tls) != CallOneResult.Empty)
                    ++called;
            }
            lock (mutex)
            {
                calling -= called;
            }
            sem.Set();
            return true;
        }

        public void Clear()
        {
            lock (mutex)
            {
                callbacks.Clear();
                count = 0;
            }
        }

        public void Disable()
        {
            lock (mutex)
            {
                enabled = false;
            }
            NotifyAll();
        }

        public void Dispose()
        {
            lock (mutex)
            {
                Disable();
            }
        }

        public void Enable()
        {
            lock (mutex)
            {
                enabled = true;
            }
            NotifyAll();
        }

        public void RemoveById(UInt64 owner_id)
        {
            SetupTls();
            IDInfo idinfo;
            lock (idInfoMutex)
            {
                if (!idInfo.ContainsKey(owner_id)) return;
                idinfo = idInfo[owner_id];
            }
            if (idinfo.id == tls.calling_in_this_thread)
                RemoveAll(owner_id);
            else
            {
                Logger.LogDebug("removeByID w/ WRONG THREAD ID");
                RemoveAll(owner_id);
            }
        }

        private CallOneResult CallOne(TLS tls)
        {
            CallbackInfo info = tls.Head;
            if (info == null)
                return CallOneResult.Empty;
            IDInfo idinfo = null;
            idinfo = GetIdInfo(info.RemovalId);
            if (idinfo != null)
            {
                CallbackInterface cb = info.Callback;
                lock (idinfo.calling_rw_mutex)
                {
                    CallbackInterface.CallResult result = CallbackInterface.CallResult.Invalid;
                    tls.SpliceOut(info);
                    if (!info.MarkedForRemoval)
                    {
                        result = cb.Call();
                    }
                    if (result == CallbackInterface.CallResult.TryAgain && !info.MarkedForRemoval)
                    {
                        lock (mutex)
                        {
                            callbacks.Add(info);
                            count++;
                        }
                        return CallOneResult.TryAgain;
                    }
                }
                return CallOneResult.Called;
            }
            CallbackInfo cbi = tls.SpliceOut(info);
            if (cbi != null)
                cbi.Callback.Call();
            return CallOneResult.Called;
        }

        private void RemoveAll(ulong owner_id)
        {
            lock (mutex)
            {
                callbacks.RemoveAll(ici => ici.RemovalId == owner_id);
                count = callbacks.Count;
            }
        }

        private void SetupTls()
        {
            if (tls == null)
            {
                tls = new TLS
                {
                    calling_in_this_thread = ROS.getPID()
                };
            }
        }

        private void NotifyAll()
        {
            sem.Set();
        }

        private void NotifyOne()
        {
            sem.Set();
        }

        private IDInfo GetIdInfo(UInt64 id)
        {
            lock (idInfoMutex)
            {
                if (idInfo.TryGetValue(id, out IDInfo value))
                    return value;
            }
            return null;
        }
    }
}
