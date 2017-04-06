using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using Uml.Robotics.XmlRpc;
using Microsoft.Extensions.Logging;

namespace Uml.Robotics.Ros
{
    public class ServiceManager
    {
        private ILogger Logger { get; } = ApplicationLogging.CreateLogger<ServiceManager>();
        private static Lazy<ServiceManager> _instance = new Lazy<ServiceManager>(LazyThreadSafetyMode.ExecutionAndPublication);

        public static ServiceManager Instance
        {
            get { return _instance.Value; }
        }

        private ConnectionManager connection_manager;
        private PollManager poll_manager;
        private List<IServicePublication> service_publications = new List<IServicePublication>();
        private object service_publications_mutex = new object();
        private List<IServiceServerLink> service_server_links = new List<IServiceServerLink>();
        private object service_server_links_mutex = new object();
        private bool shutting_down;
        private object shutting_down_mutex = new object();
        private XmlRpcManager xmlrpc_manager;

        ~ServiceManager()
        {
            shutdown();
        }

        internal IServicePublication lookupServicePublication(string name)
        {
            lock (service_publications_mutex)
            {
                foreach (IServicePublication sp in service_publications)
                {
                    if (sp.name == name)
                        return sp;
                }
            }
            return null;
        }

        internal ServiceServerLink<S> createServiceServerLink<S>(string service, bool persistent, string request_md5sum, string response_md5sum, IDictionary<string, string> header_values)
            where S : RosService, new()
        {
            lock (shutting_down_mutex)
            {
                if (shutting_down)
                    return null;
            }

            int serv_port = -1;
            string serv_host = "";
            if (!lookupService(service, ref serv_host, ref serv_port))
                return null;

            TcpTransport transport = new TcpTransport(poll_manager.poll_set);
            if (transport.connect(serv_host, serv_port))
            {
                Connection connection = new Connection();
                connection_manager.addConnection(connection);
                ServiceServerLink<S> client = new ServiceServerLink<S>(service, persistent, request_md5sum, response_md5sum, header_values);
                lock (service_server_links_mutex)
                    service_server_links.Add(client);
                connection.initialize(transport, false, null);
                client.initialize(connection);
                return client;
            }
            return null;
        }

        internal ServiceServerLink<M, T> createServiceServerLink<M, T>(string service, bool persistent, string request_md5sum,
                                                                       string response_md5sum, IDictionary<string, string> header_values)
            where M : RosMessage, new()
            where T : RosMessage, new()
        {
            lock (shutting_down_mutex)
            {
                if (shutting_down)
                    return null;
            }

            int serv_port = -1;
            string serv_host = "";
            if (!lookupService(service, ref serv_host, ref serv_port))
                return null;
            TcpTransport transport = new TcpTransport(poll_manager.poll_set);
            if (transport.connect(serv_host, serv_port))
            {
                Connection connection = new Connection();
                connection_manager.addConnection(connection);
                ServiceServerLink<M, T> client = new ServiceServerLink<M, T>(service, persistent, request_md5sum, response_md5sum, header_values);
                lock (service_server_links_mutex)
                    service_server_links.Add(client);
                connection.initialize(transport, false, null);
                client.initialize(connection);
                return client;
            }
            return null;
        }

        internal void removeServiceServerLink<M, T>(ServiceServerLink<M, T> issl)
            where M : RosMessage, new()
            where T : RosMessage, new()
        {
            removeServiceServerLink((IServiceServerLink) issl);
        }

        internal void removeServiceServerLink<S>(ServiceServerLink<S> issl)
            where S : RosService, new()
        {
            removeServiceServerLink((IServiceServerLink) issl);
        }

        internal void removeServiceServerLink(IServiceServerLink issl)
        {
            if (shutting_down)
                return;
            lock (service_server_links_mutex)
            {
                if (service_server_links.Contains(issl))
                    service_server_links.Remove(issl);
            }
        }

        internal bool advertiseService<MReq, MRes>(AdvertiseServiceOptions<MReq, MRes> ops) where MReq : RosMessage, new() where MRes : RosMessage, new()
        {
            lock (shutting_down_mutex)
            {
                if (shutting_down)
                    return false;
            }
            lock (service_publications_mutex)
            {
                if (isServiceAdvertised(ops.service))
                {
                    Logger.LogWarning("Tried to advertise  a service that is already advertised in this node [{0}]", ops.service);
                    return false;
                }
                if (ops.helper == null)
                    ops.helper = new ServiceCallbackHelper<MReq, MRes>(ops.srv_func);
                ServicePublication<MReq, MRes> pub = new ServicePublication<MReq, MRes>(ops.service, ops.md5sum, ops.datatype, ops.req_datatype, ops.res_datatype, ops.helper, ops.callback_queue, ops.tracked_object);
                service_publications.Add(pub);
            }

            XmlRpcValue args = new XmlRpcValue(), result = new XmlRpcValue(), payload = new XmlRpcValue();
            args.Set(0, this_node.Name);
            args.Set(1, ops.service);
            args.Set(2, string.Format("rosrpc://{0}:{1}", network.host, connection_manager.TCPPort));
            args.Set(3, xmlrpc_manager.Uri);
            if (!master.execute("registerService", args, result, payload, true))
            {
                throw new RosException("RPC \"registerService\" for service " + ops.service + " failed.");
            }
            return true;
        }

        internal bool unadvertiseService(string service)
        {
            lock (shutting_down_mutex)
            {
                if (shutting_down)
                    return false;
            }
            IServicePublication pub = null;
            lock (service_publications_mutex)
            {
                foreach (IServicePublication sp in service_publications)
                {
                    if (sp.name == service && !sp.isDropped)
                    {
                        pub = sp;
                        service_publications.Remove(sp);
                        break;
                    }
                }
            }
            if (pub != null)
            {
                unregisterService(pub.name);
                pub.drop();
                return true;
            }
            return false;
        }

        internal void shutdown()
        {
            lock (shutting_down_mutex)
            {
                if (shutting_down)
                    return;
            }
            shutting_down = true;
            lock (service_publications_mutex)
            {
                foreach (IServicePublication sp in service_publications)
                {
                    unregisterService(sp.name);
                    sp.drop();
                }
                service_publications.Clear();
            }
            List<IServiceServerLink> local_service_clients;
            lock (service_server_links)
            {
                local_service_clients = new List<IServiceServerLink>(service_server_links);
                service_server_links.Clear();
            }
            foreach (IServiceServerLink issl in local_service_clients)
            {
                issl.connection.drop(Connection.DropReason.Destructing);
            }
            local_service_clients.Clear();
        }

        public void Start()
        {
            shutting_down = false;
            poll_manager = PollManager.Instance;
            connection_manager = ConnectionManager.Instance;
            xmlrpc_manager = XmlRpcManager.Instance;
        }

        private bool isServiceAdvertised(string serv_name)
        {
            List<IServicePublication> sp = new List<IServicePublication>(service_publications);
            return sp.Any(s => s.name == serv_name && !s.isDropped);
        }

        private bool unregisterService(string service)
        {
            XmlRpcValue args = new XmlRpcValue(), result = new XmlRpcValue(), payload = new XmlRpcValue();
            args.Set(0, this_node.Name);
            args.Set(1, service);
            args.Set(2, string.Format("rosrpc://{0}:{1}", network.host, connection_manager.TCPPort));

            bool unregisterSuccess = false;
            try
            {
                unregisterSuccess = master.execute("unregisterService", args, result, payload, false);
            }
            catch
            {
                // ignore exception during unregister
            }
            return unregisterSuccess;
        }

        internal bool lookupService(string name, ref string serv_host, ref int serv_port)
        {
            XmlRpcValue args = new XmlRpcValue(), result = new XmlRpcValue(), payload = new XmlRpcValue();
            args.Set(0, this_node.Name);
            args.Set(1, name);
            if (!master.execute("lookupService", args, result, payload, false))
            {
                Logger.LogWarning("Service [{0}]: Not available at ROS master", name);
                return false;
            }
            string serv_uri = payload.GetString();
            if (serv_uri.Length == 0)
            {
                Logger.LogError("Service [{0}]: Empty server URI returned from master", name);
                return false;
            }
            if (!network.splitURI(serv_uri, out serv_host, out serv_port))
            {
                Logger.LogError("Service [{0}]: Bad service uri [{0}]", name, serv_uri);
                return false;
            }
            return true;
        }

        internal bool lookUpService(string mapped_name, string host, int port)
        {
            return lookupService(mapped_name, ref host, ref port);
        }

        internal bool lookUpService(string mapped_name, ref string host, ref int port)
        {
            return lookupService(mapped_name, ref host, ref port);
        }
    }
}
