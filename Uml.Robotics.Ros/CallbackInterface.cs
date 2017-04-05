using System;
using System.Collections.Generic;
using Microsoft.Extensions.Logging;

namespace Uml.Robotics.Ros
{
    internal class Callback<T>
        : CallbackInterface where T : RosMessage, new()
    {
        private ILogger Logger { get; } = ApplicationLogging.CreateLogger<Callback<T>>();
        private volatile bool callback_state;

        private readonly bool allow_concurrent_callbacks;
        private readonly Queue<Item> queue = new Queue<Item>();
        private uint size;

        public Callback(CallbackDelegate<T> f, string topic, uint queue_size, bool allow_concurrent_callbacks)
            : this(f)
        {
            this.allow_concurrent_callbacks = allow_concurrent_callbacks;
            size = queue_size;
        }

        public Callback(CallbackDelegate<T> f)
        {
            base.Event += b =>
                {
                    T t = b as T;
                    b = null;
                    f.DynamicInvoke(t);
                };
        }

        public override void AddToCallbackQueue(ISubscriptionCallbackHelper helper, RosMessage message, bool nonconst_need_copy, ref bool was_full, TimeData receipt_time)
        {
            if (was_full)
                was_full = false;

            var i = new Item
            {
                helper = helper,
                message = message,
                nonconst_need_copy = nonconst_need_copy,
                receipt_time = receipt_time
            };

            lock (queue)
            {
                if (fullNoLock())
                {
                    queue.Dequeue();
                    was_full = true;
                }
                queue.Enqueue(i);
            }
        }

        public override void Clear()
        {
            queue.Clear();
        }

        public virtual bool ready()
        {
            return true;
        }

        private bool fullNoLock()
        {
            return size > 0 && queue.Count >= size;
        }

        public bool full()
        {
            lock (queue)
            {
                return fullNoLock();
            }
        }

        public class Item
        {
            public ISubscriptionCallbackHelper helper;
            public RosMessage message;
            public bool nonconst_need_copy;
            public TimeData receipt_time;
        }

        internal override CallResult Call()
        {
            if (!allow_concurrent_callbacks)
            {
                if (callback_state)
                    return CallResult.TryAgain;
                callback_state = true;
            }
            Item i = null;
            lock (queue)
            {
                if (queue.Count == 0)
                    return CallResult.Invalid;
                i = queue.Dequeue();
            }
            i.helper.call(i.message);
            callback_state = false;
            return CallResult.Success;
        }
    }


    public abstract class CallbackInterface
    {
        public UInt64 Uid { get; }
        public delegate void CallbackDelegate(RosMessage msg);
        public event CallbackDelegate Event;

        private ILogger Logger { get; } = ApplicationLogging.CreateLogger<CallbackInterface>();
        private static object uidlock = new object();
        private static UInt64 nextuid;

        public CallbackInterface()
        {
            lock (uidlock)
            {
                Uid = nextuid;
                nextuid++;
            }
        }

        public CallbackInterface(CallbackDelegate f) 
            : this()
        {
            Event += f;
        }

        public enum CallResult
        {
            Success,
            TryAgain,
            Invalid
        }

        public void SendEvent<T>(T msg) where T : RosMessage, new()
        {
            if (Event != null)
            {
                Event(msg);
            }
            else
            {
                Logger.LogError($"{nameof(Event)} is null");
            }
        }

        public abstract void AddToCallbackQueue(ISubscriptionCallbackHelper helper, RosMessage msg, bool nonconst_need_copy, ref bool was_full, TimeData receipt_time);
        public abstract void Clear();
        internal abstract CallResult Call();
    }
}
