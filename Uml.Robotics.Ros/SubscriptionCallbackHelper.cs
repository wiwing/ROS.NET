using System;
using Microsoft.Extensions.Logging;

namespace Uml.Robotics.Ros
{
    public class SubscriptionCallbackHelper<M> : ISubscriptionCallbackHelper where M : RosMessage, new()
    {
        public SubscriptionCallbackHelper(string t, CallbackDelegate<M> cb) : this(new Callback<M>(cb))
        {
            type = t;
        }

        public SubscriptionCallbackHelper(string t)
        {
            type = t;
        }

        public SubscriptionCallbackHelper(CallbackInterface q)
            : base(q)
        {
        }

        public override void call(RosMessage msg)
        {
            Callback.SendEvent(msg);
        }
    }

    public class ISubscriptionCallbackHelper
    {
        private ILogger Logger { get; } = ApplicationLogging.CreateLogger<ISubscriptionCallbackHelper>();
        public CallbackInterface Callback { protected set; get; }

        public string type;

        protected ISubscriptionCallbackHelper()
        {
            // Logger.LogDebug("ISubscriptionCallbackHelper: 0 arg constructor");
        }

        protected ISubscriptionCallbackHelper(CallbackInterface Callback)
        {
            //Logger.LogDebug("ISubscriptionCallbackHelper: 1 arg constructor");
            //throw new NotImplementedException();
            this.Callback = Callback;
        }

        public virtual void call(RosMessage parms)
        {
            // Logger.LogDebug("ISubscriptionCallbackHelper: call");
            throw new NotImplementedException();
        }
    }
}