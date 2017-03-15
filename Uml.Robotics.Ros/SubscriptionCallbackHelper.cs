using System;
using System.Diagnostics;
using Messages;
using m = Messages.std_msgs;
using gm = Messages.geometry_msgs;
using nm = Messages.nav_msgs;

namespace Uml.Robotics.Ros
{
    public class SubscriptionCallbackHelper<M> : ISubscriptionCallbackHelper where M : IRosMessage, new()
    {
        public SubscriptionCallbackHelper(MsgTypes t, CallbackDelegate<M> cb) : this(new Callback<M>(cb))
        {
            type = t;
        }

        public SubscriptionCallbackHelper(MsgTypes t)
        {
            type = t;
        }

        public SubscriptionCallbackHelper(CallbackInterface q)
            : base(q)
        {
        }

        public override void call(IRosMessage msg)
        {
            Callback.func(msg);
        }
    }

    public class ISubscriptionCallbackHelper
    {
        public CallbackInterface Callback { protected set; get; }

        public MsgTypes type;

        protected ISubscriptionCallbackHelper()
        {
            // EDB.WriteLine("ISubscriptionCallbackHelper: 0 arg constructor");
        }

        protected ISubscriptionCallbackHelper(CallbackInterface Callback)
        {
            //EDB.WriteLine("ISubscriptionCallbackHelper: 1 arg constructor");
            //throw new NotImplementedException();
            this.Callback = Callback;
        }

        public virtual void call(IRosMessage parms)
        {
            // EDB.WriteLine("ISubscriptionCallbackHelper: call");
            throw new NotImplementedException();
        }
    }
}