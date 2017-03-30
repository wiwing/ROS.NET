using System;

namespace Uml.Robotics.Ros
{
    public class LocalSubscriberLink : SubscriberLink, IDisposable
    {
        private object drop_mutex = new object();
        private bool dropped;
        private LocalPublisherLink subscriber;

        public LocalSubscriberLink(Publication pub)
        {
            parent = pub;
            topic = parent.Name;
        }

        public string TransportType
        {
            get { return "INTRAPROCESS"; /*lol... pwned*/ }
        }

        #region IDisposable Members

        public void Dispose()
        {
        }

        #endregion

        public void setSubscriber(LocalPublisherLink pub_link)
        {
            subscriber = pub_link;
            connection_id = ConnectionManager.Instance.GetNewConnectionID();
            destination_caller_id = this_node.Name;
        }

        internal override void enqueueMessage(MessageAndSerializerFunc holder)
        {
            lock (drop_mutex)
            {
                if (dropped) return;
            }

            if (subscriber != null)
                subscriber.handleMessage(holder.msg, holder.serialize, holder.nocopy);
        }

        public override void drop()
        {
            lock (drop_mutex)
            {
                if (dropped) return;
                dropped = true;
            }
            if (subscriber != null)
            {
                subscriber.drop();
            }

            lock (parent)
                parent.removeSubscriberLink(this);
        }

        public override void getPublishTypes(ref bool ser, ref bool nocopy, string messageType)
        {
            lock (drop_mutex)
            {
                if (dropped)
                {
                    ser = false;
                    nocopy = false;
                    return;
                }
            }
            subscriber.getPublishTypes(ref ser, ref nocopy, messageType);
        }
    }
}
