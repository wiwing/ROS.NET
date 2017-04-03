namespace Uml.Robotics.Ros
{
    public class SubscriberCallbacks
    {
        public ICallbackQueue Callback;
        public SubscriberStatusCallback connect, disconnect;

        public SubscriberCallbacks() : this(null, null, null)
        {
        }

        public SubscriberCallbacks(SubscriberStatusCallback connectCB, SubscriberStatusCallback disconnectCB,
            ICallbackQueue Callback)
        {
            connect = connectCB;
            disconnect = disconnectCB;
            this.Callback = Callback;
        }

        internal ulong Get()
        {
            return ROS.getPID();
        }
    }
}
