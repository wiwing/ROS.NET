using System;
using System.Collections.Generic;
using Messages;
using m = Messages.std_msgs;
using gm = Messages.geometry_msgs;
using nm = Messages.nav_msgs;

namespace Uml.Robotics.Ros
{
    public class LocalPublisherLink : PublisherLink, IDisposable
    {
        private object drop_mutex = new object();
        private bool dropped;
        private LocalSubscriberLink publisher = null;

        public LocalPublisherLink(Subscription parent, string xmlrpc_uri) : base(parent, xmlrpc_uri)
        {
        }

        public new string TransportType
        {
            get { return "INTRAPROCESS"; /*lol... pwned*/ }
        }

        public void setPublisher(LocalSubscriberLink pub_link)
        {
            lock (parent)
            {
                IDictionary<string, string> header = new Dictionary<string, string>();
                header["topic"] = parent.name;
                header["md5sum"] = parent.md5sum;
                header["callerid"] = this_node.Name;
                header["type"] = parent.datatype;
                header["tcp_nodelay"] = "1";
                setHeader(new Header {Values = header});
            }
        }

        public override void drop()
        {
            lock (drop_mutex)
            {
                if (dropped) return;
                dropped = true;
            }


            if (publisher != null)
            {
                publisher.drop();
            }

            lock (parent)
                parent.removePublisherLink(this);
        }

        public void handleMessage<T>(T m, bool ser, bool nocopy) where T : RosMessage, new()
        {
            stats.messages_received++;
            if (m.Serialized == null)
            {
                //ignore stats to avoid an unnecessary allocation
            }
            else
            {
                stats.bytes_received += (ulong) m.Serialized.Length;
            }
            if (parent != null)
                lock (parent)
                    stats.drops += parent.handleMessage(m, ser, nocopy, m.connection_header, this);
        }

        public void getPublishTypes(ref bool ser, ref bool nocopy, MsgTypes mt)
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
            if (parent != null)
                lock (parent)
                {
                    parent.getPublishTypes(ref ser, ref nocopy, mt);
                }
            else
            {
                ser = true;
                nocopy = false;
            }
        }

        #region IDisposable Members

        public void Dispose()
        {
        }

        #endregion
    }
}