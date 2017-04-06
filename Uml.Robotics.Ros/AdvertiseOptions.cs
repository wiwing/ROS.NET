using System;
using System.Collections.Generic;
using Messages;

namespace Uml.Robotics.Ros
{
    public class AdvertiseOptions<T> where T : RosMessage, new()
    {
        public ICallbackQueue callbackQueue;
        public SubscriberStatusCallback connectCB;
        public SubscriberStatusCallback disconnectCB;
        public string dataType = "";
        public bool hasHeader;
        public bool latch;
        public string md5Sum = "";
        public string messageDefinition = "";
        public int queueSize;
        public string topic = "";

        public AdvertiseOptions()
        {
        }

        public AdvertiseOptions(string topic, int queueSize, string md5, string dt, string messageDefinition,
            SubscriberStatusCallback connectCallback)
            : this(topic, queueSize, md5, dt, messageDefinition, connectCallback, null)
        {
        }


        public AdvertiseOptions(string topic, int queueSize, string md5, string dt, string messageDefinition)
            : this(topic, queueSize, md5, dt, messageDefinition, null, null)
        {
        }

        public AdvertiseOptions(
            string topic,
            int queueSize,
            string md5,
            string dt,
            string messageDefinition,
            SubscriberStatusCallback connectcallback,
            SubscriberStatusCallback disconnectcallback)
        {
            this.topic = topic;
            this.queueSize = queueSize;
            md5Sum = md5;
            T tt = new T();
            dataType = dt.Length > 0 ? dt : tt.MessageType;

            if (string.IsNullOrEmpty(messageDefinition))
                this.messageDefinition = tt.MessageDefinition();
            else
                this.messageDefinition = messageDefinition;
            hasHeader = tt.HasHeader();
            connectCB = connectcallback;
            disconnectCB = disconnectcallback;
        }

        public AdvertiseOptions(string t, int q_size)
            : this(t, q_size, null, null)
        {
        }

        public AdvertiseOptions(
            string t,
            int queueSize,
            SubscriberStatusCallback connectCallback,
            SubscriberStatusCallback disconnectCallback
        )
        : this(
            t,
            queueSize,
            new T().MD5Sum(),
            new T().MessageType,
            new T().MessageDefinition(),
            connectCallback,
            disconnectCallback
        )
        {
        }

        public static AdvertiseOptions<M> Create<M>(
            string topic,
            int queueSize,
            SubscriberStatusCallback connectcallback,
            SubscriberStatusCallback disconnectcallback,
            ICallbackQueue queue
        )
            where M : RosMessage, new()
        {
            return new AdvertiseOptions<M>(topic, queueSize, connectcallback, disconnectcallback) { callbackQueue = queue };
        }
    }
}