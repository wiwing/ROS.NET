using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Messages;
using Uml.Robotics.XmlRpc;
using m = Messages.std_msgs;
using gm = Messages.geometry_msgs;
using nm = Messages.nav_msgs;
using Microsoft.Extensions.Logging;

namespace Uml.Robotics.Ros
{
    public class Subscription
    {
        private ILogger Logger { get; } = ApplicationLogging.CreateLogger<Subscription>();

        private bool _dropped;


        private List<ICallbackInfo> callbacks = new List<ICallbackInfo>();
        public object callbacks_mutex = new object();
        public string datatype = "";
        public Dictionary<PublisherLink, LatchInfo> latched_messages = new Dictionary<PublisherLink, LatchInfo>();
        public string md5sum = "";
        public object md5sum_mutex = new object();
        public string msgtype;
        public string name = "";
        public int nonconst_callbacks;
        public List<PendingConnection> pending_connections = new List<PendingConnection>();
        public object pending_connections_mutex = new object();
        public List<PublisherLink> publisher_links = new List<PublisherLink>();
        public object publisher_links_mutex = new object(), shutdown_mutex = new object();
        private bool shutting_down;

        public Subscription(string n, string md5s, string dt)
        {
            name = n;
            md5sum = md5s;
            datatype = dt;
            msgtype = dt;
        }

        public bool IsDropped
        {
            get { return _dropped; }
        }

        public int NumPublishers
        {
            get { lock (publisher_links_mutex) return publisher_links.Count; }
        }

        public int NumCallbacks
        {
            get { lock (callbacks_mutex) return callbacks.Count; }
        }

        public void shutdown()
        {
            lock (shutdown_mutex)
            {
                shutting_down = true;
            }
            drop();
        }

        public XmlRpcValue getStats()
        {
            XmlRpcValue stats = new XmlRpcValue();
            stats.Set(0, name);
            XmlRpcValue conn_data = new XmlRpcValue();
            conn_data.SetArray(0);
            lock (publisher_links_mutex)
            {
                int cidx = 0;
                foreach (PublisherLink link in publisher_links)
                {
                    XmlRpcValue v = new XmlRpcValue();
                    PublisherLink.Stats s = link.stats;
                    v.Set(0, link.ConnectionID);
                    v.Set(1, s.bytes_received);
                    v.Set(2, s.messages_received);
                    v.Set(3, s.drops);
                    v.Set(4, 0);
                    conn_data.Set(cidx++, v);
                }
            }
            stats.Set(1, conn_data);
            return stats;
        }

        public void getInfo(XmlRpcValue info)
        {
            lock (publisher_links_mutex)
            {
                //Logger.LogDebug("SUB: getInfo with " + publisher_links.Count + " publinks in list");
                foreach (PublisherLink c in publisher_links)
                {
                    //Logger.LogDebug("PUB: adding a curr_info to info!");
                    XmlRpcValue curr_info = new XmlRpcValue();
                    curr_info.Set(0, (int) c.ConnectionID);
                    curr_info.Set(1, c.XmlRpc_Uri);
                    curr_info.Set(2, "i");
                    curr_info.Set(3, c.TransportType);
                    curr_info.Set(4, name);
                    //Logger.LogDebug("PUB curr_info DUMP:\n\t");
                    //curr_info.Dump();
                    info.Set(info.Size, curr_info);
                }
                //Logger.LogDebug("SUB: outgoing info is of type: " + info.Type + " and has size: " + info.Size);
            }
        }

        public void drop()
        {
            if (!_dropped)
            {
                _dropped = true;
                dropAllConnections();
            }
        }

        public void dropAllConnections()
        {
            List<PublisherLink> localsubscribers = null;
            lock (publisher_links_mutex)
            {
                localsubscribers = new List<PublisherLink>(publisher_links);
                publisher_links.Clear();
            }
            foreach (PublisherLink it in localsubscribers)
            {
                //hot it's like
                it.drop();
                //drop it like it's hot, backwards.
            }
        }

        public bool urisEqual(string uri1, string uri2)
        {
            if (uri1 == null)
            {
                throw new ArgumentNullException(nameof(uri1));
            }
            if (uri2 == null)
            {
                throw new ArgumentNullException(nameof(uri2));
            }
            string n1;
            string h1 = n1 = "";
            int p2;
            int p1 = p2 = 0;
            network.splitURI(uri1, ref h1, ref p1);
            network.splitURI(uri2, ref n1, ref p2);
            return h1 == n1 && p1 == p2;
        }

        public void removePublisherLink(PublisherLink pub)
        {
            lock (publisher_links_mutex)
            {
                if (publisher_links.Contains(pub))
                {
                    publisher_links.Remove(pub);
                }
                if (pub.Latched)
                    latched_messages.Remove(pub);
            }
        }

        public void addPublisherLink(PublisherLink pub)
        {
            publisher_links.Add(pub);
        }

        public bool pubUpdate(IEnumerable<string> pubs)
        {
            using (Logger.BeginScope ($"{ nameof(pubUpdate) }"))
            {
                lock (shutdown_mutex)
                {
                    if (shutting_down || _dropped)
                        return false;
                }
                bool retval = true;

                Logger.LogDebug("Publisher update for [" + name + "]");

                List<string> additions = new List<string>();
                List<PublisherLink> subtractions = new List<PublisherLink>();
                lock (publisher_links_mutex)
                {
                    subtractions.AddRange(from spc in publisher_links let found = pubs.Any(up_i => urisEqual(spc.XmlRpc_Uri, up_i)) where !found select spc);
                    foreach (string up_i in pubs)
                    {
                        bool found = publisher_links.Any(spc => urisEqual(up_i, spc.XmlRpc_Uri));
                        if (found) continue;
                        lock (pending_connections_mutex)
                        {
                            if (pending_connections.Any(pc => urisEqual(up_i, pc.RemoteUri)))
                            {
                                found = true;
                            }
                            if (!found) additions.Add(up_i);
                        }
                    }
                }
                foreach (PublisherLink link in subtractions)
                {
                    if (link.XmlRpc_Uri != XmlRpcManager.Instance.uri)
                    {
                        Logger.LogDebug("Disconnecting from publisher [" + link.CallerID + "] of topic [" + name +
                                    "] at [" + link.XmlRpc_Uri + "]");
                        link.drop();
                    }
                    else
                    {
                        Logger.LogWarning("Cannot disconnect from self for topic: " + name);
                    }
                }

                foreach (string i in additions)
                {
                    if (XmlRpcManager.Instance.uri != i)
                    {
                        retval &= NegotiateConnection(i);
                        //Logger.LogDebug("NEGOTIATINGING");
                    }
                    else
                        Logger.LogInformation("Skipping myself (" + name + ", " + XmlRpcManager.Instance.uri + ")");
                }
                return retval;
            }
        }

        public bool NegotiateConnection(string xmlrpc_uri)
        {
            int protos = 0;
            XmlRpcValue tcpros_array = new XmlRpcValue(), protos_array = new XmlRpcValue(), Params = new XmlRpcValue();
            tcpros_array.Set(0, "TCPROS");
            protos_array.Set(protos++, tcpros_array);
            Params.Set(0, this_node.Name);
            Params.Set(1, name);
            Params.Set(2, protos_array);
            string peer_host = "";
            int peer_port = 0;
            if (!network.splitURI(xmlrpc_uri, ref peer_host, ref peer_port))
            {
                Logger.LogError("Bad xml-rpc URI: [" + xmlrpc_uri + "]");
                return false;
            }
            XmlRpcClient c = new XmlRpcClient(peer_host, peer_port);
            if (!c.IsConnected || !c.ExecuteNonBlock("requestTopic", Params))
            {
                Logger.LogError("Failed to contact publisher [" + peer_host + ":" + peer_port + "] for topic [" + name +
                              "]");
                c.Dispose();
                return false;
            }
            Logger.LogDebug("Began asynchronous xmlrpc connection to http://" + peer_host + ":" + peer_port +
                            "/ for topic [" + name + "]");
            PendingConnection conn = new PendingConnection(c, this, xmlrpc_uri, Params);
            lock (pending_connections_mutex)
            {
                pending_connections.Add(conn);
            }
            XmlRpcManager.Instance.addAsyncConnection(conn);
            return true;
        }

        public void pendingConnectionDone(PendingConnection conn, XmlRpcValue result)
        {
            using (Logger.BeginScope ($"{ nameof(pendingConnectionDone) }"))
            {
                //XmlRpcValue result = XmlRpcValue.LookUp(res);
                lock (shutdown_mutex)
                {
                    if (shutting_down || _dropped)
                        return;
                }
                XmlRpcValue proto = new XmlRpcValue();
                if (!XmlRpcManager.Instance.validateXmlrpcResponse("requestTopic", result, proto))
                {
                    conn.failures++;
                    Logger.LogWarning("Negotiating for " + conn.parent.name + " has failed " + conn.failures + " times");
                    return;
                }
                lock (pending_connections_mutex)
                {
                    pending_connections.Remove(conn);
                }
                string peer_host = conn.client.Host;
                int peer_port = conn.client.Port;
                string xmlrpc_uri = "http://" + peer_host + ":" + peer_port + "/";
                if (proto.Size == 0)
                {
                    Logger.LogDebug("Couldn't agree on any common protocols with [" + xmlrpc_uri + "] for topic [" + name +
                                "]");
                    return;
                }
                if (proto.Type != XmlRpcValue.ValueType.TypeArray)
                {
                    Logger.LogWarning("Available protocol info returned from " + xmlrpc_uri + " is not a list.");
                    return;
                }
                string proto_name = proto[0].Get<string>();
                if (proto_name == "UDPROS")
                {
                    Logger.LogError("Udp is currently not supported. Use TcpRos instead.");
                }
                else if (proto_name == "TCPROS")
                {
                    if (proto.Size != 3 || proto[1].Type != XmlRpcValue.ValueType.TypeString || proto[2].Type != XmlRpcValue.ValueType.TypeInt)
                    {
                        Logger.LogWarning("TcpRos Publisher should implement string, int as parameter");
                        return;
                    }
                    string pub_host = proto[1].Get<string>();
                    int pub_port = proto[2].Get<int>();
                    Logger.LogDebug("Connecting via tcpros to topic [" + name + "] at host [" + pub_host + ":" + pub_port +
                                "]");

                    TcpTransport transport = new TcpTransport(PollManager.Instance.poll_set) {_topic = name};
                    if (transport.connect(pub_host, pub_port))
                    {
                        Connection connection = new Connection();
                        TransportPublisherLink pub_link = new TransportPublisherLink(this, xmlrpc_uri);

                        connection.initialize(transport, false, null);
                        pub_link.initialize(connection);

                        ConnectionManager.Instance.addConnection(connection);

                        lock (publisher_links_mutex)
                        {
                            addPublisherLink(pub_link);
                        }

                        Logger.LogDebug("Connected to publisher of topic [" + name + "] at  [" + pub_host + ":" + pub_port +
                                    "]");
                    }
                    else
                    {
                        Logger.LogError("Failed to connect to publisher of topic [" + name + "] at  [" + pub_host + ":" +
                                    pub_port + "]");
                    }
                }
                else
                {
                    Logger.LogError("The XmlRpc Server does not provide a supported protocol.");
                }
            }
        }

        public void headerReceived(PublisherLink link, Header header)
        {
            lock (md5sum_mutex)
            {
                if (md5sum == "*")
                    md5sum = link.md5sum;
            }
        }

        internal ulong handleMessage(RosMessage msg, bool ser, bool nocopy, IDictionary<string, string> connection_header,
            PublisherLink link)
        {
            RosMessage t = null;
            ulong drops = 0;
            TimeData receipt_time = ROS.GetTime().data;
            if (msg.Serialized != null) //will be null if self-subscribed
                msg.Deserialize(msg.Serialized);
            lock (callbacks_mutex)
            {
                foreach (ICallbackInfo info in callbacks)
                {
                    string ti = info.helper.type;
                    if (nocopy || ser)
                    {
                        t = msg;
                        t.connection_header = msg.connection_header;
                        t.Serialized = null;
                        bool was_full = false;
                        bool nonconst_need_copy = callbacks.Count > 1;
                        info.subscription_queue.AddToCallbackQueue(info.helper, t, nonconst_need_copy, ref was_full, receipt_time);
                        if (was_full)
                            ++drops;
                        else
                            info.callback.addCallback(info.subscription_queue, info.Get());
                    }
                }
            }

            if (t != null && link.Latched)
            {
                LatchInfo li = new LatchInfo
                {
                    message = t,
                    link = link,
                    connection_header = connection_header,
                    receipt_time = receipt_time
                };
                if (latched_messages.ContainsKey(link))
                    latched_messages[link] = li;
                else
                    latched_messages.Add(link, li);
            }

            return drops;
        }

        public void Dispose()
        {
            shutdown();
        }

        internal bool addCallback<M>(SubscriptionCallbackHelper<M> helper, string md5sum, CallbackQueueInterface queue,
            uint queue_size, bool allow_concurrent_callbacks, string topiclol) where M : RosMessage, new()
        {
            lock (md5sum_mutex)
            {
                if (this.md5sum == "*" && md5sum != "*")
                    this.md5sum = md5sum;
            }

            if (md5sum != "*" && md5sum != this.md5sum)
                return false;

            lock (callbacks_mutex)
            {
                CallbackInfo<M> info = new CallbackInfo<M> {helper = helper, callback = queue, subscription_queue = new Callback<M>(helper.Callback.SendEvent, topiclol, queue_size, allow_concurrent_callbacks)};
                //if (!helper.isConst())
                //{
                ++nonconst_callbacks;
                //}

                callbacks.Add(info);

                if (latched_messages.Count > 0)
                {
                    string ti = info.helper.type;
                    lock (publisher_links_mutex)
                    {
                        foreach (PublisherLink link in publisher_links)
                        {
                            if (link.Latched)
                            {
                                if (latched_messages.ContainsKey(link))
                                {
                                    LatchInfo latch_info = latched_messages[link];
                                    bool was_full = false;
                                    bool nonconst_need_copy = callbacks.Count > 1;
                                    info.subscription_queue.AddToCallbackQueue(info.helper, latched_messages[link].message, nonconst_need_copy, ref was_full, ROS.GetTime().data);
                                    if (!was_full)
                                        info.callback.addCallback(info.subscription_queue, info.Get());
                                }
                            }
                        }
                    }
                }
            }
            return true;
        }

        public void removeCallback(ISubscriptionCallbackHelper helper)
        {
            lock (callbacks_mutex)
            {
                foreach (ICallbackInfo info in callbacks)
                {
                    if (info.helper == helper)
                    {
                        info.subscription_queue.Clear();
                        info.callback.removeByID(info.Get());
                        callbacks.Remove(info);
                        //if (!helper.isConst())
                        --nonconst_callbacks;
                        break;
                    }
                }
            }
        }

        public void addLocalConnection(Publication pub)
        {
            lock (publisher_links_mutex)
            {
                if (_dropped) return;

                Logger.LogInformation("Creating intraprocess link for topic [{0}]", name);

                LocalPublisherLink pub_link = new LocalPublisherLink(this, XmlRpcManager.Instance.uri);
                LocalSubscriberLink sub_link = new LocalSubscriberLink(pub);
                pub_link.setPublisher(sub_link);
                sub_link.setSubscriber(pub_link);

                addPublisherLink(pub_link);
                pub.addSubscriberLink(sub_link);
            }
        }

        public void getPublishTypes(ref bool ser, ref bool nocopy, string typeInfo)
        {
            lock (callbacks_mutex)
            {
                foreach (ICallbackInfo info in callbacks)
                {
                    if (info.helper.type == typeInfo)
                        nocopy = true;
                    else
                        ser = true;
                    if (nocopy && ser)
                        return;
                }
            }
        }

        #region Nested type: CallbackInfo

        public class CallbackInfo<M> : ICallbackInfo where M : RosMessage, new()
        {
            public CallbackInfo()
            {
                helper = new SubscriptionCallbackHelper<M>(new M().MessageType);
            }
        }

        #endregion

        #region Nested type: ICallbackInfo

        public class ICallbackInfo
        {
            public CallbackQueueInterface callback;
            public ISubscriptionCallbackHelper helper;
            public CallbackInterface subscription_queue;

            public UInt64 Get()
            {
                return subscription_queue.Uid;
            }
        }

        #endregion

        #region Nested type: LatchInfo

        public class LatchInfo
        {
            public IDictionary<string, string> connection_header;
            public PublisherLink link;
            public RosMessage message;
            public TimeData receipt_time;
        }

        #endregion
    }
}
