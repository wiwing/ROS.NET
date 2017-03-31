using System;
using System.Collections.Generic;
using Microsoft.Extensions.Logging;

namespace Uml.Robotics.Ros
{
    public class NodeHandle : IDisposable
    {
        private ILogger Logger { get; } = ApplicationLogging.CreateLogger<NodeHandle>();
        private string Namespace = "", UnresolvedNamespace = "";
        private CallbackQueue _callback;
        private bool _ok = true;
        private NodeHandleBackingCollection collection = new NodeHandleBackingCollection();
        private int nh_refcount;
        private object nh_refcount_mutex = new object();
        private bool no_validate = false;
        private bool node_started_by_nh;

        private IDictionary<string, string> remappings = new Dictionary<string, string>();
        private IDictionary<string, string> unresolved_remappings = new Dictionary<string, string>();

        /// <summary>
        ///     Creates a new node
        /// </summary>
        /// <param name="ns">Namespace of node</param>
        /// <param name="remappings">any remappings</param>
        public NodeHandle(string ns, IDictionary<string, string> remappings = null)
        {
            if (ns != "" && ns[0] == '~')
                ns = names.resolve(ns);
            construct(ns, true);
            initRemappings(remappings);
        }

        /// <summary>
        ///     Create a new nodehandle that is a partial deep copy of another
        /// </summary>
        /// <param name="rhs">The nodehandle this new one aspires to be</param>
        public NodeHandle(NodeHandle rhs)
        {
            Callback = rhs.Callback;
            remappings = new Dictionary<string, string>(rhs.remappings);
            unresolved_remappings = new Dictionary<string, string>(rhs.unresolved_remappings);
            construct(rhs.Namespace, true);
            UnresolvedNamespace = rhs.UnresolvedNamespace;
        }

        /// <summary>
        ///     Creates a new child node
        /// </summary>
        /// <param name="parent">Parent node to attach</param>
        /// <param name="ns">Namespace of new node</param>
        public NodeHandle(NodeHandle parent, string ns)
        {
            Namespace = parent.Namespace;
            Callback = parent.Callback;
            remappings = new Dictionary<string, string>(parent.remappings);
            unresolved_remappings = new Dictionary<string, string>(parent.unresolved_remappings);
            construct(ns, false);
        }

        /// <summary>
        ///     Creates a new child node with remappings
        /// </summary>
        /// <param name="parent">Parent node to attach</param>
        /// <param name="ns">Namespace of new node</param>
        /// <param name="remappings">Remappings</param>
        public NodeHandle(NodeHandle parent, string ns, IDictionary<string, string> remappings)
        {
            Namespace = parent.Namespace;
            Callback = parent.Callback;
            this.remappings = new Dictionary<string, string>(remappings);
            construct(ns, false);
        }

        /// <summary>
        ///     Creates a new nodehandle
        /// </summary>
        public NodeHandle() : this(this_node.Namespace, null)
        {
        }

        /// <summary>
        ///     gets/sets this nodehandle's callbackqueue
        ///     get : if the private _callback is null it is set to ROS.GlobalCallbackQueue
        /// </summary>
        public CallbackQueue Callback
        {
            get
            {
                if (_callback == null)
                {
                    _callback = ROS.GlobalCallbackQueue;
                }

                return _callback;
            }
            set { _callback = value; }
        }

        /// <summary>
        ///     The conjunction of ROS.ok, and the ok-ness of this nodehandle
        /// </summary>
        public bool ok
        {
            get { return ROS.ok && _ok; }
            set { _ok = value; }
        }

        #region IDisposable Members

        /// <summary>
        ///     Like a piece of trash
        /// </summary>
        public void Dispose()
        {
            destruct();
        }

        #endregion

        ~NodeHandle()
        {
            Dispose();
        }

        /// <summary>
        ///     Unregister every subscriber and publisher in this node
        /// </summary>
        public void shutdown()
        {
            lock (collection.mutex)
            {
                foreach (ISubscriber sub in collection.subscribers)
                    sub.unsubscribe();
                foreach (IPublisher pub in collection.publishers)
                    pub.unadvertise();

                foreach (IServiceClient client in collection.serviceclients)
                    client.shutdown();
                foreach (ServiceServer srv in collection.serviceservers)
                    srv.shutdown();
            }
        }

        /// <summary>
        ///     Creates a publisher
        /// </summary>
        /// <typeparam name="M">Type of topic</typeparam>
        /// <param name="topic">Name of topic</param>
        /// <param name="q_size">How many messages to qeueue if asynchrinous</param>
        /// <returns>A publisher with the specified topic type, name and options</returns>
        public Publisher<M> advertise<M>(string topic, int q_size) where M : RosMessage, new()
        {
            return advertise<M>(topic, q_size, false);
        }

        /// <summary>
        ///     Creates a publisher, specify latching
        /// </summary>
        /// <typeparam name="M">Type of topic</typeparam>
        /// <param name="topic">Name of topic</param>
        /// <param name="q_size">How many messages to enqueue if asynchrinous</param>
        /// <param name="l">Boolean determines whether the given publisher will latch or not</param>
        /// <returns>A publisher with the specified topic type, name and options</returns>
        public Publisher<M> advertise<M>(string topic, int q_size, bool l) where M : RosMessage, new()
        {
            return advertise(new AdvertiseOptions<M>(topic, q_size) {latch = l});
        }

        /// <summary>
        ///     Creates a publisher with connect and disconnect callbacks
        /// </summary>
        /// <typeparam name="M">Type of topic</typeparam>
        /// <param name="topic">Name of topic</param>
        /// <param name="queue_size">How many messages to enqueue if asynchrinous</param>
        /// <param name="connectcallback">Callback to fire when this node connects</param>
        /// <param name="disconnectcallback">Callback to fire when this node disconnects</param>
        /// <returns>A publisher with the specified topic type, name and options</returns>
        public Publisher<M> advertise<M>(string topic, int queue_size, SubscriberStatusCallback connectcallback,
            SubscriberStatusCallback disconnectcallback)
            where M : RosMessage, new()
        {
            return advertise<M>(topic, queue_size, connectcallback, disconnectcallback, false);
        }

        /// <summary>
        ///     Creates a publisher with connect and disconnect callbacks, specify latching.
        /// </summary>
        /// <typeparam name="M">Type of topic</typeparam>
        /// <param name="topic">Name of topic</param>
        /// <param name="queue_size">How many messages to enqueue if asynchrinous</param>
        /// <param name="connectcallback">Callback to fire when this node connects</param>
        /// <param name="disconnectcallback">Callback to fire when this node disconnects</param>
        /// <param name="l">Boolean determines whether the given publisher will latch or not</param>
        /// <returns>A publisher with the specified topic type, name and options</returns>
        public Publisher<M> advertise<M>(string topic, int queue_size, SubscriberStatusCallback connectcallback,
            SubscriberStatusCallback disconnectcallback, bool l)
            where M : RosMessage, new()
        {
            return advertise(new AdvertiseOptions<M>(topic, queue_size, connectcallback, disconnectcallback) {latch = l});
        }

        /// <summary>
        ///     Creates a publisher with the given advertise options
        /// </summary>
        /// <typeparam name="M">Type of topic</typeparam>
        /// <param name="ops">Advertise options</param>
        /// <returns>A publisher with the specified options</returns>
        public Publisher<M> advertise<M>(AdvertiseOptions<M> ops) where M : RosMessage, new()
        {
            ops.topic = resolveName(ops.topic);
            if (ops.callback_queue == null)
            {
                ops.callback_queue = Callback;
            }
            SubscriberCallbacks callbacks = new SubscriberCallbacks(ops.connectCB, ops.disconnectCB, ops.callback_queue);
            if (TopicManager.Instance.advertise(ops, callbacks))
            {
                Publisher<M> pub = new Publisher<M>(ops.topic, ops.md5sum, ops.datatype, this, callbacks);
                lock (collection.mutex)
                {
                    collection.publishers.Add(pub);
                }
                return pub;
            }
            Logger.LogError("Advertisement of publisher has failed");
            return null;
        }

        /// <summary>
        ///     Creates a subscriber with the given topic name.
        /// </summary>
        /// <typeparam name="M">Type of the subscriber message</typeparam>
        /// <param name="topic">Topic name</param>
        /// <param name="queue_size">How many messages to qeueue</param>
        /// <param name="cb">Callback to fire when a message is receieved</param>
        /// <returns>A subscriber</returns>
        public Subscriber<M> subscribe<M>(string topic, uint queue_size, CallbackDelegate<M> cb) where M : RosMessage, new()
        {
            return subscribe<M>(topic, queue_size, new Callback<M>(cb), false);
        }

        /// <summary>
        ///     Creates a subscriber with the given topic name.
        /// </summary>
        /// <typeparam name="M">Type of the subscriber message</typeparam>
        /// <param name="topic">Topic name</param>
        /// <param name="queue_size">How many messages to qeueue</param>
        /// <param name="cb">Callback to fire when a message is receieved</param>
        /// <param name="allow_concurrent_callbacks">Probably breaks things when true</param>
        /// <returns>A subscriber</returns>
        public Subscriber<M> subscribe<M>(string topic, uint queue_size, CallbackDelegate<M> cb, bool allow_concurrent_callbacks) where M : RosMessage, new()
        {
            return subscribe<M>(topic, queue_size, new Callback<M>(cb), allow_concurrent_callbacks);
        }

        /// <summary>
        ///     Creates a subscriber
        /// </summary>
        /// <typeparam name="M">Topic type</typeparam>
        /// <param name="topic">Topic name</param>
        /// <param name="queue_size">How many messages to qeueue</param>
        /// <param name="cb">Function to fire when a message is recieved</param>
        /// <param name="allow_concurrent_callbacks">Probably breaks things when true</param>
        /// <returns>A subscriber</returns>
        public Subscriber<M> subscribe<M>(string topic, uint queue_size, CallbackInterface cb, bool allow_concurrent_callbacks)
            where M : RosMessage, new()
        {
            if (_callback == null)
            {
                _callback = ROS.GlobalCallbackQueue;
            }
            SubscribeOptions<M> ops = new SubscribeOptions<M>(topic, queue_size, cb.SendEvent)
            {callback_queue = _callback, allow_concurrent_callbacks=allow_concurrent_callbacks};
            ops.callback_queue.addCallback(cb);
            return subscribe(ops);
        }

        /// <summary>
        ///     Creates a subscriber with given subscriber options
        /// </summary>
        /// <typeparam name="M">Topic type</typeparam>
        /// <param name="ops">Subscriber options</param>
        /// <returns>A subscriber</returns>
        public Subscriber<M> subscribe<M>(SubscribeOptions<M> ops) where M : RosMessage, new()
        {
            ops.topic = resolveName(ops.topic);
            if (ops.callback_queue == null)
            {
                ops.callback_queue = Callback;
            }
            if (TopicManager.Instance.subscribe(ops))
            {
                Subscriber<M> sub = new Subscriber<M>(ops.topic, this, ops.helper);
                lock (collection.mutex)
                {
                    collection.subscribers.Add(sub);
                }
                return sub;
            }
            return new Subscriber<M>();
        }

        /// <summary>
        ///     Advertises a named ServiceServer
        /// </summary>
        /// <typeparam name="MReq">Request sub-srv type</typeparam>
        /// <typeparam name="MRes">Response sub-srv type</typeparam>
        /// <param name="service">The name of the service to advertise</param>
        /// <param name="srv_func">The handler for the service</param>
        /// <returns>The ServiceServer that will call the ServiceFunction on behalf of ServiceClients</returns>
        public ServiceServer advertiseService<MReq, MRes>(string service, ServiceFunction<MReq, MRes> srv_func)
            where MReq : RosMessage, new()
            where MRes : RosMessage, new()
        {
            return advertiseService(new AdvertiseServiceOptions<MReq, MRes>(service, srv_func));
        }

        /// <summary>
        ///     Advertises a ServiceServer with specified OPTIONS
        /// </summary>
        /// <typeparam name="MReq">Request sub-srv type</typeparam>
        /// <typeparam name="MRes">Response sub-srv type</typeparam>
        /// <param name="ops">isn't it obvious?</param>
        /// <returns>The ServiceServer that will call the ServiceFunction on behalf of ServiceClients</returns>
        public ServiceServer advertiseService<MReq, MRes>(AdvertiseServiceOptions<MReq, MRes> ops)
            where MReq : RosMessage, new()
            where MRes : RosMessage, new()
        {
            ops.service = resolveName(ops.service);
            if (ops.callback_queue == null)
            {
                ops.callback_queue = Callback;
            }
            if (ServiceManager.Instance.advertiseService(ops))
            {
                ServiceServer srv = new ServiceServer(ops.service, this);
                lock (collection.mutex)
                {
                    collection.serviceservers.Add(srv);
                }
                return srv;
            }
            throw new InvalidOperationException("Could not advertise service");
        }

        public ServiceClient<MReq, MRes> serviceClient<MReq, MRes>(string service_name)
            where MReq : RosMessage, new()
            where MRes : RosMessage, new()
        {
            return serviceClient<MReq, MRes>(new ServiceClientOptions(service_name, false, null));
        }

        public ServiceClient<MReq, MRes> serviceClient<MReq, MRes>(string service_name, bool persistent)
            where MReq : RosMessage, new()
            where MRes : RosMessage, new()
        {
            return serviceClient<MReq, MRes>(new ServiceClientOptions(service_name, persistent, null));
        }

        public ServiceClient<MReq, MRes> serviceClient<MReq, MRes>(string service_name, bool persistent,
            IDictionary<string, string> header_values)
            where MReq : RosMessage, new()
            where MRes : RosMessage, new()
        {
            return serviceClient<MReq, MRes>(new ServiceClientOptions(service_name, persistent, header_values));
        }

        public ServiceClient<MReq, MRes> serviceClient<MReq, MRes>(ServiceClientOptions ops)
            where MReq : RosMessage, new()
            where MRes : RosMessage, new()
        {
            ops.service = resolveName(ops.service);
            ops.md5sum = new MReq().MD5Sum();
            return new ServiceClient<MReq, MRes>(ops.service, ops.persistent, ops.header_values, ops.md5sum);
        }

        public ServiceClient<MSrv> serviceClient<MSrv>(string service_name)
            where MSrv : RosService, new()

        {
            return serviceClient<MSrv>(new ServiceClientOptions(service_name, false, null));
        }

        public ServiceClient<MSrv> serviceClient<MSrv>(string service_name, bool persistent)
            where MSrv : RosService, new()

        {
            return serviceClient<MSrv>(new ServiceClientOptions(service_name, persistent, null));
        }

        public ServiceClient<MSrv> serviceClient<MSrv>(string service_name, bool persistent,
            IDictionary<string, string> header_values)
            where MSrv : RosService, new()

        {
            return serviceClient<MSrv>(new ServiceClientOptions(service_name, persistent, header_values));
        }

        public ServiceClient<MSrv> serviceClient<MSrv>(ServiceClientOptions ops)
            where MSrv : RosService, new()

        {
            ops.service = resolveName(ops.service);
            ops.md5sum = new MSrv().RequestMessage.MD5Sum();
            return new ServiceClient<MSrv>(ops.service, ops.persistent, ops.header_values, ops.md5sum);
        }

        private void construct(string ns, bool validate_name)
        {
            if (!ROS.initialized)
                throw new Exception("You must call ROS.Init before instantiating the first nodehandle");
            collection = new NodeHandleBackingCollection();
            UnresolvedNamespace = ns;
            Namespace = validate_name ? resolveName(ns) : resolveName(ns, true, true);

            ok = true;
            lock (nh_refcount_mutex)
            {
                if (nh_refcount == 0 && !ROS.isStarted())
                {
                    node_started_by_nh = true;
                    ROS.start();
                }
                ++nh_refcount;
            }
        }

        private void destruct()
        {
            collection.Dispose();
            collection = null;
            lock (nh_refcount_mutex)
            {
                --nh_refcount;
            }
            _callback = null;
            if (nh_refcount == 0 && node_started_by_nh)
                ROS.shutdown();
        }

        private void initRemappings(IDictionary<string, string> rms)
        {
            if (rms == null) return;
            foreach (string k in rms.Keys)
            {
                string left = k;
                string right = rms[k];
                if (left != "" && left[0] != '_')
                {
                    string resolved_left = resolveName(left, false);
                    string resolved_right = resolveName(right, false);
                    remappings[resolved_left] = resolved_right;
                    unresolved_remappings[left] = right;
                }
            }
        }

        private string remapName(string name)
        {
            string resolved = resolveName(name, false);
            if (resolved == null)
                resolved = "";
            else if (remappings.ContainsKey(resolved))
                return (string) remappings[resolved];
            return names.remap(resolved);
        }

        private string resolveName(string name)
        {
            return resolveName(name, true);
        }

        private string resolveName(string name, bool remap)
        {
            string error = "";
            if (!names.validate(name, ref error))
                names.InvalidName(error);
            return resolveName(name, remap, no_validate);
        }

        private string resolveName(string name, bool remap, bool novalidate)
        {
            //Logger.LogDebug("resolveName(" + name + ")");
            if (name == "") return Namespace;
            string final = name;
            if (final[0] == '~')
                names.InvalidName("THERE'S A ~ IN THAT!");
            else if (final[0] != '/' && Namespace != "")
            {
                final = names.append(Namespace, final);
            }
            final = names.clean(final);
            if (remap)
            {
                final = remapName(final);
            }
            return names.resolve(final, false);
        }

        #region Nested type: NodeHandleBackingCollection

        public class NodeHandleBackingCollection : IDisposable
        {
            public readonly object mutex = new object();
            public List<IPublisher> publishers = new List<IPublisher>();

            public List<IServiceClient> serviceclients = new List<IServiceClient>();
            public List<ServiceServer> serviceservers = new List<ServiceServer>();
            public List<ISubscriber> subscribers = new List<ISubscriber>();

            #region IDisposable Members

            public void Dispose()
            {
                publishers.Clear();
                subscribers.Clear();

                serviceservers.Clear();
                serviceclients.Clear();
            }

            #endregion
        }

        #endregion
    }
}
