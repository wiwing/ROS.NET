namespace Uml.Robotics.Ros
{
    public class SubscribeOptions<T> where T : RosMessage, new()
    {
        public bool allow_concurrent_callbacks = true;
        public ICallbackQueue callback_queue;
        public string datatype = "";
        public bool has_header;
        public SubscriptionCallbackHelper<T> helper;
        public bool latch;
        public string md5sum = "";
        public string message_definition = "";
        public uint queue_size;
        public string topic = "";

        public SubscribeOptions()
            : this("", 1)
        {
        }

        public SubscribeOptions(string topic, uint queue_size, CallbackDelegate<T> CALL = null)
        {
            this.topic = topic;
            this.queue_size = queue_size;
            var generic = new T();
            if (CALL != null)
                helper = new SubscriptionCallbackHelper<T>(generic.MessageType, CALL);
            else
                helper = new SubscriptionCallbackHelper<T>(generic.MessageType);


            datatype = generic.MessageType;
            md5sum = generic.MD5Sum();
        }
    }

    public delegate void CallbackDelegate<in T>(T argument) where T : RosMessage, new();
}
