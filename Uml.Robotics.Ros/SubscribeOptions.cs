using System;
using System.Diagnostics;
using Messages;
using m = Messages.std_msgs;
using gm = Messages.geometry_msgs;
using nm = Messages.nav_msgs;

namespace Uml.Robotics.Ros
{
    public class SubscribeOptions<T> where T : RosMessage, new()
    {
        public bool allow_concurrent_callbacks = true;
        public CallbackQueueInterface callback_queue;
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
            //allow_concurrent_callbacks = false;
            //allow_concurrent_callbacks = true;
        }

        public SubscribeOptions(string topic, uint queue_size, CallbackDelegate<T> CALL = null)
        {
            // TODO: Complete member initialization
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
