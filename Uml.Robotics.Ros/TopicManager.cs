using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using Messages;
using System.Linq;
using System.Threading;
using Uml.Robotics.XmlRpc;

namespace Uml.Robotics.Ros
{
    public class TopicManager
    {
        #region Delegates

        public delegate byte[] SerializeFunc();

        #endregion

        private static Lazy<TopicManager> _instance = new Lazy<TopicManager>(LazyThreadSafetyMode.ExecutionAndPublication);

        public static TopicManager Instance
        {
            get { return _instance.Value; }
        }

        private ILogger Logger { get; } = ApplicationLogging.CreateLogger<TopicManager>();
        private List<Publication> advertised_topics = new List<Publication>();
        private object advertised_topics_mutex = new object();
        private bool shutting_down;
        private object shutting_down_mutex = new object();
        private object subs_mutex = new object();
        private List<Subscription> subscriptions = new List<Subscription>();

        /// <summary>
        ///     Binds the XmlRpc requests to callback functions, signal to start
        /// </summary>
        public void Start()
        {
            lock (shutting_down_mutex)
            {
                shutting_down = false;

                XmlRpcManager.Instance.bind("publisherUpdate", pubUpdateCallback);
                XmlRpcManager.Instance.bind("requestTopic", requestTopicCallback);
                XmlRpcManager.Instance.bind("getBusStats", getBusStatusCallback);
                XmlRpcManager.Instance.bind("getBusInfo", getBusInfoCallback);
                XmlRpcManager.Instance.bind("getSubscriptions", getSubscriptionsCallback);
                XmlRpcManager.Instance.bind("getPublications", getPublicationsCallback);
            }
        }

        /// <summary>
        ///     unbinds the XmlRpc requests to callback functions, signal to shutdown
        /// </summary>
        public void shutdown()
        {
            lock (shutting_down_mutex)
            {
                if (shutting_down)
                    return;

                lock (subs_mutex)
                {
                    shutting_down = true;
                }

                XmlRpcManager.Instance.unbind("publisherUpdate");
                XmlRpcManager.Instance.unbind("requestTopic");
                XmlRpcManager.Instance.unbind("getBusStats");
                XmlRpcManager.Instance.unbind("getBusInfo");
                XmlRpcManager.Instance.unbind("getSubscriptions");
                XmlRpcManager.Instance.unbind("getPublications");

                bool failedOnceToUnadvertise = false;
                lock (advertised_topics_mutex)
                {
                    foreach (Publication p in advertised_topics)
                    {
                        if (!p.Dropped && !failedOnceToUnadvertise)
                        {
                            failedOnceToUnadvertise = !unregisterPublisher(p.Name);
                        }
                        p.drop();
                    }
                    advertised_topics.Clear();
                }

                bool failedOnceToUnsubscribe = false;
                lock (subs_mutex)
                {
                    foreach (Subscription s in subscriptions)
                    {
                        if (!s.IsDropped && !failedOnceToUnsubscribe)
                        {
                            failedOnceToUnsubscribe = !unregisterSubscriber(s.name);
                        }
                        s.shutdown();
                    }
                    subscriptions.Clear();
                }
            }
        }

        /// <summary>
        ///     gets the list of advertised topics.
        /// </summary>
        /// <param name="topics">List of topics to update</param>
        public void getAdvertisedTopics(out string[] topics)
        {
            lock (advertised_topics_mutex)
            {
                topics = advertised_topics.Select(a => a.Name).ToArray();
            }
        }

        /// <summary>
        ///     gets the list of subscribed topics.
        /// </summary>
        /// <param name="topics"></param>
        public void getSubscribedTopics(out string[] topics)
        {
            lock (subs_mutex)
            {
                topics = subscriptions.Select(s => s.name).ToArray();
            }
        }

        /// <summary>
        ///     Looks up all current publishers on a given topic
        /// </summary>
        /// <param name="topic">Topic name to look up</param>
        /// <returns></returns>
        public Publication lookupPublication(string topic)
        {
            lock (advertised_topics_mutex)
            {
                return lookupPublicationWithoutLock(topic);
            }
        }

        /// <summary>
        ///     Checks if the given topic is valid.
        /// </summary>
        /// <typeparam name="T">Advertise Options </typeparam>
        /// <param name="ops"></param>
        /// <returns></returns>
        private bool isValid<T>(AdvertiseOptions<T> ops) where T : RosMessage, new()
        {
            if (ops.datatype == "*")
                throw new Exception("Advertising with * as the datatype is not allowed.  Topic [" + ops.topic + "]");
            if (ops.md5sum == "*")
                throw new Exception("Advertising with * as the md5sum is not allowed.  Topic [" + ops.topic + "]");
            if (ops.md5sum == "")
                throw new Exception("Advertising on topic [" + ops.topic + "] with an empty md5sum");
            if (ops.datatype == "")
                throw new Exception("Advertising on topic [" + ops.topic + "] with an empty datatype");
            if (string.IsNullOrEmpty(ops.message_definition))
            {
                this.Logger.LogWarning(
                    "Advertising on topic [" + ops.topic +
                     "] with an empty message definition. Some tools may not work correctly"
                );
            }
            return true;
        }

        /// <summary>
        ///     Register as a publisher on a topic.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="ops"></param>
        /// <param name="callbacks"></param>
        /// <returns></returns>
        public bool advertise<T>(AdvertiseOptions<T> ops, SubscriberCallbacks callbacks) where T : RosMessage, new()
        {
            if (!isValid(ops))
                return false;

            Publication pub = null;
            lock (advertised_topics_mutex)
            {
                if (shutting_down)
                    return false;
                pub = lookupPublicationWithoutLock(ops.topic);
                if (pub != null)
                {
                    if (pub.Md5sum != ops.md5sum)
                    {
                        this.Logger.LogError(
                            "Tried to advertise on topic [{0}] with md5sum [{1}] and datatype [{2}], but the topic is already advertised as md5sum [{3}] and datatype [{4}]",
                            ops.topic, ops.md5sum,
                            ops.datatype, pub.Md5sum, pub.DataType
                        );
                        return false;
                    }
                }
                else
                    pub = new Publication(ops.topic, ops.datatype, ops.md5sum, ops.message_definition, ops.queue_size,
                        ops.latch, ops.has_header);
                pub.addCallbacks(callbacks);
                advertised_topics.Add(pub);
            }

            bool found = false;
            Subscription sub = null;
            lock (subs_mutex)
            {
                foreach (Subscription s in subscriptions)
                {
                    if (s.name == ops.topic && md5sumsMatch(s.md5sum, ops.md5sum) && !s.IsDropped)
                    {
                        found = true;
                        sub = s;
                        break;
                    }
                }
            }

            if (found)
                sub.addLocalConnection(pub);

            var args = new XmlRpcValue(this_node.Name, ops.topic, ops.datatype, XmlRpcManager.Instance.Uri);
            var result = new XmlRpcValue();
            var payload = new XmlRpcValue();

            if (!master.execute("registerPublisher", args, result, payload, true))
            {
                this.Logger.LogError("RPC \"registerService\" for service " + ops.topic + " failed.");
                return false;
            }

            return true;
        }

        public bool subscribe<T>(SubscribeOptions<T> ops) where T : RosMessage, new()
        {
            lock (subs_mutex)
            {
                if (addSubCallback(ops))
                    return true;
                if (shutting_down)
                    return false;
            }
            if (ops.md5sum == "")
                throw subscribeFail(ops, "with an empty md5sum");
            if (ops.datatype == "")
                throw subscribeFail(ops, "with an empty datatype");
            if (ops.helper == null)
                throw subscribeFail(ops, "without a callback");
            string md5sum = ops.md5sum;
            string datatype = ops.datatype;
            Subscription s = new Subscription(ops.topic, md5sum, datatype);
            s.addCallback(ops.helper, ops.md5sum, ops.callback_queue, ops.queue_size, ops.allow_concurrent_callbacks, ops.topic);
            if (!registerSubscriber(s, ops.datatype))
            {
                this.Logger.LogError("Couldn't register subscriber on topic [{0}]", ops.topic);
                s.shutdown();
                return false;
            }

            lock (subs_mutex)
            {
                subscriptions.Add(s);
            }
            return true;
        }

        public Exception subscribeFail<T>(SubscribeOptions<T> ops, string reason) where T : RosMessage, new()
        {
            return new Exception("Subscribing to topic [" + ops.topic + "] " + reason);
        }

        public bool unsubscribe(string topic, ISubscriptionCallbackHelper sbch)
        {
            Subscription sub = null;
            lock (subs_mutex)
            {
                if (shutting_down) return false;
                foreach (Subscription s in subscriptions)
                {
                    if (s.name == topic)
                    {
                        sub = s;
                        break;
                    }
                }
            }

            if (sub == null)
                return false;

            sub.removeCallback(sbch);
            if (sub.NumCallbacks == 0)
            {
                lock (subs_mutex)
                {
                    subscriptions.Remove(sub);
                }

                if (!unregisterSubscriber(topic))
                    this.Logger.LogWarning("Couldn't unregister subscriber for topic [" + topic + "]");

                sub.shutdown();
                return true;
            }
            return true;
        }

        internal Subscription getSubscription(string topic)
        {
            lock (subs_mutex)
            {
                if (shutting_down)
                    return null;

                foreach (Subscription t in subscriptions)
                {
                    if (!t.IsDropped && t.name == topic)
                        return t;
                }
            }
            return null;
        }

        public int getNumSubscriptions()
        {
            lock (subs_mutex)
            {
                return subscriptions.Count;
            }
        }

        public void publish(Publication p, RosMessage msg, SerializeFunc serfunc = null)
        {
            if (msg == null)
                return;
            if (serfunc == null)
                serfunc = msg.Serialize;
            if (p.connection_header == null)
            {
                p.connection_header = new Header {Values = new Dictionary<string, string>()};
                p.connection_header.Values["type"] = p.DataType;
                p.connection_header.Values["md5sum"] = p.Md5sum;
                p.connection_header.Values["message_definition"] = p.MessageDefinition;
                p.connection_header.Values["callerid"] = this_node.Name;
                p.connection_header.Values["latching"] = Convert.ToString(p.Latch);
            }

            if (!ROS.ok || shutting_down)
                return;

            if (p.HasSubscribers || p.Latch)
            {
                bool nocopy = false;
                bool serialize = false;
                if (msg != null && msg.MessageType != "xamla/unkown")
                {
                    p.getPublishTypes(ref serialize, ref nocopy, msg.MessageType);
                }
                else
                {
                    serialize = true;
                }

                p.publish(new MessageAndSerializerFunc(msg, serfunc, serialize, nocopy));

                if (serialize)
                    PollManager.Instance.poll_set.signal();
            }
            else
            {
                p.incrementSequence();
            }
        }

        public void incrementSequence(string topic)
        {
            Publication pub = lookupPublication(topic);
            if (pub != null)
                pub.incrementSequence();
        }

        public bool isLatched(string topic)
        {
            Publication pub = lookupPublication(topic);
            if (pub != null)
                return pub.Latch;
            return false;
        }

        public bool md5sumsMatch(string lhs, string rhs)
        {
            return (lhs == "*" || rhs == "*" || lhs == rhs);
        }

        public bool addSubCallback<M>(SubscribeOptions<M> ops) where M : RosMessage, new()
        {
            bool found = false;
            bool found_topic = false;
            Subscription sub = null;

            if (shutting_down)
                return false;

            foreach (Subscription s in subscriptions)
            {
                sub = s;
                if (!sub.IsDropped && sub.name == ops.topic)
                {
                    found_topic = true;
                    if (md5sumsMatch(ops.md5sum, sub.md5sum))
                        found = true;
                    break;
                }
            }
            if (found_topic && !found)
            {
                throw new Exception
                    ("Tried to subscribe to a topic with the same name but different md5sum as a topic that was already subscribed [" +
                     ops.datatype + "/" + ops.md5sum + " vs. " + sub.datatype + "/" +
                     sub.md5sum + "]");
            }
            if (found)
            {
                if (!sub.addCallback(ops.helper, ops.md5sum, ops.callback_queue, ops.queue_size,
                        ops.allow_concurrent_callbacks, ops.topic))
                {
                    return false;
                }
            }
            return found;
        }

        public bool requestTopic(string topic, XmlRpcValue protos, ref XmlRpcValue ret)
        {
            for (int proto_idx = 0; proto_idx < protos.Size; proto_idx++)
            {
                XmlRpcValue proto = protos[proto_idx];
                if (proto.Type != XmlRpcType.Array)
                {
                    this.Logger.LogError("requestTopic protocol list was not a list of lists");
                    return false;
                }
                if (proto[0].Type != XmlRpcType.String)
                {
                    this.Logger.LogError(
                        "requestTopic received a protocol list in which a sublist did not start with a string");
                    return false;
                }

                string proto_name = proto[0].GetString();

                if (proto_name == "TCPROS")
                {
                    var tcpRosParams = new XmlRpcValue("TCPROS", network.host, ConnectionManager.Instance.TCPPort);
                    ret.Set(0, 1);
                    ret.Set(1, "");
                    ret.Set(2, tcpRosParams);
                    return true;
                }
                if (proto_name == "UDPROS")
                {
                    this.Logger.LogWarning("Ignoring topics with UdpRos as protocol");
                }
                else
                {
                    this.Logger.LogWarning("An unsupported protocol was offered: [{0}]", proto_name);
                }
            }

            this.Logger.LogError("No supported protocol was provided");
            return false;
        }

        public bool isTopicAdvertised(string topic)
        {
            return advertised_topics.Count(o => o.Name == topic) > 0;
        }

        public bool registerSubscriber(Subscription s, string datatype)
        {
            string uri = XmlRpcManager.Instance.Uri;

            XmlRpcValue args = new XmlRpcValue(this_node.Name, s.name, datatype, uri);
            XmlRpcValue result = new XmlRpcValue();
            XmlRpcValue payload = new XmlRpcValue();
            if (!master.execute("registerSubscriber", args, result, payload, true))
            {
                Logger.LogError("RPC \"registerSubscriber\" for service " + s.name + " failed.");
                return false;
            }
            List<string> pub_uris = new List<string>();
            for (int i = 0; i < payload.Size; i++)
            {
                XmlRpcValue load = payload[i];
                string pubed = load.GetString();
                if (pubed != uri && !pub_uris.Contains(pubed))
                {
                    pub_uris.Add(pubed);
                }
            }
            bool self_subscribed = false;
            Publication pub = null;
            string sub_md5sum = s.md5sum;
            lock (advertised_topics_mutex)
            {
                foreach (Publication p in advertised_topics)
                {
                    pub = p;
                    string pub_md5sum = pub.Md5sum;
                    if (pub.Name == s.name && md5sumsMatch(pub_md5sum, sub_md5sum) && !pub.Dropped)
                    {
                        self_subscribed = true;
                        break;
                    }
                }
            }

            s.pubUpdate(pub_uris);
            if (self_subscribed)
                s.addLocalConnection(pub);
            return true;
        }

        public bool unregisterSubscriber(string topic)
        {
            var args = new XmlRpcValue(this_node.Name, topic, XmlRpcManager.Instance.Uri);
            var result = new XmlRpcValue();
            var payload = new XmlRpcValue();

            bool unregisterSuccess = false;
            try
            {
                unregisterSuccess = master.execute("unregisterSubscriber", args, result, payload, false) && result.IsValid;
            }
            // Ignore exception during unregister
            catch (Exception e)
            {
                // Logger.LogError(e.Message);
            }
            return unregisterSuccess;
        }

        public bool unregisterPublisher(string topic)
        {
            var args = new XmlRpcValue(this_node.Name, topic, XmlRpcManager.Instance.Uri);
            var result = new XmlRpcValue();
            var payload = new XmlRpcValue();

            bool unregisterSuccess = false;
            try
            {
                unregisterSuccess = master.execute("unregisterPublisher", args, result, payload, false) && result.IsValid;
            }
            // Ignore exception during unregister
            catch (Exception e)
            {
                // Logger.LogError(e.Message);
            }
            return unregisterSuccess;
        }

        public Publication lookupPublicationWithoutLock(string topic)
        {
            return advertised_topics.FirstOrDefault(p => p.Name == topic && !p.Dropped);
        }

        public void getBusStats(XmlRpcValue stats)
        {
            var publish_stats = new XmlRpcValue();
            var subscribe_stats = new XmlRpcValue();
            var service_stats = new XmlRpcValue();

            publish_stats.SetArray(0); //.Size = 0;
            subscribe_stats.SetArray(0); //subscribe_stats.Size = 0;
            service_stats.SetArray(0); //service_stats.Size = 0;
            int pidx = 0;
            lock (advertised_topics_mutex)
            {
                foreach (Publication t in advertised_topics)
                {
                    publish_stats.Set(pidx++, t.GetStats());
                }
            }
            int sidx = 0;
            lock (subs_mutex)
            {
                foreach (Subscription t in subscriptions)
                {
                    subscribe_stats.Set(sidx++, t.getStats());
                }
            }

            //TODO: fix for services

            stats.Set(0, publish_stats);
            stats.Set(1, subscribe_stats);
            stats.Set(2, service_stats);
        }

        public void getBusInfo(XmlRpcValue info)
        {
            info.SetArray(0);
            lock (advertised_topics_mutex)
            {
                foreach (Publication t in advertised_topics)
                {
                    t.getInfo(info);
                }
            }
            lock (subs_mutex)
            {
                foreach (Subscription t in subscriptions)
                {
                    t.getInfo(info);
                }
            }
        }

        public void getSubscriptions(ref XmlRpcValue subs)
        {
            subs.SetArray(0);
            lock (subs_mutex)
            {
                int sidx = 0;
                foreach (Subscription t in subscriptions)
                {
                    subs.Set(sidx++, new XmlRpcValue(t.name, t.datatype));
                }
            }
        }

        public void getPublications(ref XmlRpcValue pubs)
        {
            pubs.SetArray(0);
            lock (advertised_topics_mutex)
            {
                int sidx = 0;
                foreach (Publication t in advertised_topics)
                {
                    XmlRpcValue pub = new XmlRpcValue();
                    pub.Set(0, t.Name);
                    pub.Set(1, t.DataType);
                    pubs.Set(sidx++, pub);
                }
            }
        }

        public bool pubUpdate(string topic, List<string> pubs)
        {
            using (this.Logger.BeginScope ($"{ nameof(pubUpdate) }"))
            {
                this.Logger.LogDebug("TopicManager is updating publishers for " + topic);

                Subscription sub = null;
                lock (subs_mutex)
                {
                    if (shutting_down)
                        return false;

                    foreach (Subscription s in subscriptions)
                    {
                        if (s.name != topic || s.IsDropped)
                            continue;
                        sub = s;
                        break;
                    }
                }
                if (sub != null)
                    return sub.pubUpdate(pubs);
                this.Logger.LogInformation(
                    "Request for updating publishers of topic " + topic + ", which has no subscribers."
                );
                return false;
            }
        }

        //public void pubUpdateCallback([In] [Out] IntPtr parms, [In] [Out] IntPtr result)
        public void pubUpdateCallback(XmlRpcValue parm, XmlRpcValue result)
        {
            //XmlRpcValue parm = XmlRpcValue.Create(ref parms);
            List<string> pubs = new List<string>();
            for (int idx = 0; idx < parm[2].Size; idx++)
                pubs.Add(parm[2][idx].GetString());
            if (pubUpdate(parm[1].GetString(), pubs))
                XmlRpcManager.Instance.responseInt(1, "", 0)(result);
            else
            {
                const string error = "Unknown error while handling XmlRpc call to pubUpdate";
                this.Logger.LogError(error);
                XmlRpcManager.Instance.responseInt(0, error, 0)(result);
            }
        }

        //public void requestTopicCallback([In] [Out] IntPtr parms, [In] [Out] IntPtr result)
        public void requestTopicCallback(XmlRpcValue parm, XmlRpcValue res)
        {
            //XmlRpcValue res = XmlRpcValue.Create(ref result)
            //	, parm = XmlRpcValue.Create(ref parms);
            //result = res.instance;
            if (!requestTopic(parm[1].GetString(), parm[2], ref res))
            {
                const string error = "Unknown error while handling XmlRpc call to requestTopic";
                this.Logger.LogError(error);
                XmlRpcManager.Instance.responseInt(0, error, 0)(res);
            }
        }

        //public void getBusStatusCallback([In] [Out] IntPtr parms, [In] [Out] IntPtr result)
        public void getBusStatusCallback(XmlRpcValue parm, XmlRpcValue res)
        {
            //XmlRpcValue res = XmlRpcValue.Create(ref result);
            res.Set(0, 1);
            res.Set(1, "");
            var response = new XmlRpcValue();
            getBusStats(response);
            res.Set(2, response);
        }

        //public void getBusInfoCallback([In] [Out] IntPtr parms, [In] [Out] IntPtr result)
        public void getBusInfoCallback(XmlRpcValue parm, XmlRpcValue res)
        {
            //XmlRpcValue res = XmlRpcValue.Create(ref result);
            res.Set(0, 1);
            res.Set(1, "");
            var response = new XmlRpcValue();
            //IntPtr resp = response.instance;
            getBusInfo(response);
            res.Set(2, response);
        }

        //public void getSubscriptionsCallback([In] [Out] IntPtr parms, [In] [Out] IntPtr result)
        public void getSubscriptionsCallback(XmlRpcValue parm, XmlRpcValue res)
        {
            //XmlRpcValue res = XmlRpcValue.Create(ref result);
            res.Set(0, 1);
            res.Set(1, "subscriptions");
            var response = new XmlRpcValue();
            getSubscriptions(ref response);
            res.Set(2, response);
        }

        //public void getPublicationsCallback([In] [Out] IntPtr parms, [In] [Out] IntPtr result)
        public void getPublicationsCallback(XmlRpcValue parm, XmlRpcValue res)
        {
            //XmlRpcValue res = XmlRpcValue.Create(ref result);
            res.Set(0, 1);
            res.Set(1, "publications");
            var response = new XmlRpcValue();
            getPublications(ref response);
            res.Set(2, response);
        }

        public bool unadvertise(string topic, SubscriberCallbacks callbacks)
        {
            Publication pub = null;
            lock (advertised_topics_mutex)
            {
                foreach (Publication p in advertised_topics)
                {
                    if (p.Name == topic && !p.Dropped)
                    {
                        pub = p;
                        break;
                    }
                }
            }
            if (pub == null)
                return false;

            pub.removeCallbacks(callbacks);
            lock (advertised_topics_mutex)
            {
                if (pub.NumCallbacks == 0)
                {
                    unregisterPublisher(pub.Name);
                    pub.drop();
                    advertised_topics.Remove(pub);
                }
            }
            return true;
        }
    }
}
