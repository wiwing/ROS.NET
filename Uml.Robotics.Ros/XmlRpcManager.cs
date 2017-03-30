using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using Uml.Robotics.XmlRpc;
using Microsoft.Extensions.Logging;

namespace Uml.Robotics.Ros
{
    public class XmlRpcManager : IDisposable
    {
        private class FunctionInfo
        {
            public XmlRpcFunc function;
            public string name;
            public XmlRpcServerMethod wrapper;
        }

        private static Lazy<XmlRpcManager> _instance = new Lazy<XmlRpcManager>(LazyThreadSafetyMode.ExecutionAndPublication);

        public static XmlRpcManager Instance
        {
            get { return _instance.Value; }
        }

        ILogger Logger { get; } = ApplicationLogging.CreateLogger<XmlRpcManager>();

        List<IAsyncXmlRpcConnection> addedConnections = new List<IAsyncXmlRpcConnection>();
        List<IAsyncXmlRpcConnection> removedConnections = new List<IAsyncXmlRpcConnection>();

        List<CachedXmlRpcClient> clients = new List<CachedXmlRpcClient>();
        List<IAsyncXmlRpcConnection> connections = new List<IAsyncXmlRpcConnection>();
        Dictionary<string, FunctionInfo> functions = new Dictionary<string, FunctionInfo>();

        object addedConnectionsGate = new object();
        object removedConnectionsGate = new object();
        object clientsGate = new object();
        object functionsGate = new object();

        XmlRpcFunc getPid;

        XmlRpcServer server;
        Thread serverThread;
        bool shuttingDown;
        bool unbindRequested;

        string uri = "";
        int port;

        public XmlRpcManager()
        {
            XmlRpcUtil.SetLogLevel(
#if !DEBUG
                XmlRpcUtil.XMLRPC_LOG_LEVEL.ERROR
#elif TRACE
                XmlRpcUtil.XMLRPC_LOG_LEVEL.INFO
#else
                XmlRpcUtil.XMLRPC_LOG_LEVEL.WARNING
#endif
            );

            this.server = new XmlRpcServer();
            this.getPid = (parms, result) => responseInt(1, "", Process.GetCurrentProcess().Id)(result);
        }

        public string Uri
        {
            get { return uri; }
        }

        public bool IsShuttingDown
        {
            get { return shuttingDown; }
        }

        #region IDisposable Members

        public void Dispose()
        {
            shutdown();
        }

        #endregion

        public void serverThreadFunc()
        {
            while (!shuttingDown)
            {
                if (server.Dispatch == null)
                {
                    throw new NullReferenceException("XmlRpcManager is not initialized yet!");
                }
                lock (addedConnectionsGate)
                {
                    foreach (var con in addedConnections)
                    {
                        //Logger.LogDebug("Completed ASYNC XmlRpc connection to: " + ((con as PendingConnection) != null ? ((PendingConnection) con).RemoteUri : "SOMEWHERE OVER THE RAINBOW"));
                        con.AddToDispatch(server.Dispatch);
                        connections.Add(con);
                    }
                    addedConnections.Clear();
                }

                lock (functionsGate)
                {
                    server.Work(0.1);
                }

                while (unbindRequested)
                {
                    Thread.Sleep(ROS.WallDuration);
                }

                foreach (var con in connections)
                {
                    if (con.Check())
                        removeAsyncXMLRPCClient(con);
                }

                lock (removedConnectionsGate)
                {
                    foreach (var con in removedConnections)
                    {
                        con.RemoveFromDispatch(server.Dispatch);
                        connections.Remove(con);
                    }
                    removedConnections.Clear();
                }
            }
        }

        public bool validateXmlRpcResponse(string method, XmlRpcValue response, XmlRpcValue payload)
        {
            if (response.Type != XmlRpcValue.ValueType.Array)
                return validateFailed(method, "didn't return an array -- {0}", response);
            if (response.Size != 3)
                return validateFailed(method, "didn't return a 3-element array -- {0}", response);
            if (response[0].Type != XmlRpcValue.ValueType.Int)
                return validateFailed(method, "didn't return an int as the 1st element -- {0}", response);
            int status_code = response[0].GetInt();
            if (response[1].Type != XmlRpcValue.ValueType.String)
                return validateFailed(method, "didn't return a string as the 2nd element -- {0}", response);
            string status_string = response[1].GetString();
            if (status_code != 1)
            {
                return validateFailed(method, "returned an error ({0}): [{1}] -- {2}", status_code, status_string, response);
            }

            switch (response[2].Type)
            {
                case XmlRpcValue.ValueType.Array:
                    {
                        payload.SetArray(0);
                        for (int i = 0; i < response[2].Length; i++)
                        {
                            payload.Set(i, response[2][i]);
                        }
                    }
                    break;
                case XmlRpcValue.ValueType.Int:
                case XmlRpcValue.ValueType.Double:
                case XmlRpcValue.ValueType.String:
                case XmlRpcValue.ValueType.Boolean:
                    payload.Copy(response[2]);
                    break;
                case XmlRpcValue.ValueType.Invalid:
                    break;
                default:
                    throw new ArgumentException("Unhandled valid xmlrpc payload type: " + response[2].Type, nameof(response));
            }
            return true;
        }

        private bool validateFailed(string method, string errorfmat, params object[] info)
        {
            Logger.LogDebug("XML-RPC Call [{0}] {1} failed validation", method, string.Format(errorfmat, info));
            return false;
        }

        public CachedXmlRpcClient getXMLRPCClient(string host, int port, string uri)
        {
            CachedXmlRpcClient c = null;
            lock (clientsGate)
            {
                List<CachedXmlRpcClient> zombies = new List<CachedXmlRpcClient>();
                foreach (CachedXmlRpcClient client in clients)
                {
                    if (!client.in_use)
                    {
                        if (DateTime.Now.Subtract(client.last_use_time).TotalSeconds > 30 || client.dead)
                        {
                            zombies.Add(client);
                        }
                        else if (client.CheckIdentity(host, port, uri))
                        {
                            c = client;
                            break;
                        }
                    }
                }
                foreach (CachedXmlRpcClient C in zombies)
                {
                    clients.Remove(C);
                    C.Dispose();
                }
                if (c == null)
                {
                    c = new CachedXmlRpcClient(host, port, uri);
                    clients.Add(c);
                }
            }
            c.AddRef();
            return c;
        }

        public void releaseXMLRPCClient(CachedXmlRpcClient client)
        {
            client.DelRef();
            client.Dispose();
        }

        public void addAsyncConnection(IAsyncXmlRpcConnection conn)
        {
            lock (addedConnectionsGate)
                addedConnections.Add(conn);
        }

        public void removeAsyncXMLRPCClient(IAsyncXmlRpcConnection conn)
        {
            lock (removedConnectionsGate)
                removedConnections.Add(conn);
        }

        public bool bind(string function_name, XmlRpcFunc cb)
        {
            lock (functionsGate)
            {
                if (functions.ContainsKey(function_name))
                    return false;
                functions.Add(function_name,
                    new FunctionInfo
                    {
                        name = function_name,
                        function = cb,
                        wrapper = new XmlRpcServerMethod(function_name, cb, server)
                    }
                );
            }
            return true;
        }

        public void unbind(string function_name)
        {
            unbindRequested = true;
            lock (functionsGate)
            {
                functions.Remove(function_name);
            }
            unbindRequested = false;
        }


        public Action<XmlRpcValue> responseStr(IntPtr target, int code, string msg, string response)
        {
            return (XmlRpcValue v) =>
            {
                v.Set(0, code);
                v.Set(1, msg);
                v.Set(2, response);
            };
        }

        public Action<XmlRpcValue> responseInt(int code, string msg, int response)
        {
            return (XmlRpcValue v) =>
            {
                v.Set(0, code);
                v.Set(1, msg);
                v.Set(2, response);
            };
        }

        public Action<XmlRpcValue> responseBool(int code, string msg, bool response)
        {
            return (XmlRpcValue v) =>
            {
                v.Set(0, code);
                v.Set(1, msg);
                v.Set(2, response);
            };
        }

        /// <summary>
        ///     <para> This function starts the XmlRpcServer used to handle inbound calls on this node </para>
        ///     <para>
        ///         The optional argument is used to force ROS to try to bind to a specific port.
        ///         Doing so should only be done when acting as the RosMaster.
        ///     </para>
        ///     <para> </para>
        ///     <para>
        ///         Jordan, this function used to have the following:
        ///         <list type="bullet">
        ///             <item>
        ///                 <description>A string argument named "host"</description>
        ///             </item>
        ///             <item>
        ///                 <description>that defaulted to "0"</description>
        ///             </item>
        ///             <item>
        ///                 <description>and it was used to determine the port.</description>
        ///             </item>
        ///         </list>
        ///     </para>
        /// </summary>
        /// <param name="p">The specific port number to bind to, if any</param>
        public void Start(int p = 0)
        {
            shuttingDown = false;

            bind("getPid", getPid);

            if (p != 0)
            {
                //if port isn't 0, then we better be the master,
                //      so let's grab this bull by the horns
                uri = ROS.ROS_MASTER_URI;
                port = p;
            }
            //if port is 0, then we need to get our hostname from ROS' network init,
            //   and we don't know our port until we're bound and listening

            bool bound = server.BindAndListen(port);
            if (!bound)
                throw new Exception("RPCServer bind failed");

            if (p == 0)
            {
                //if we weren't called with a port #, then we have to figure out what
                //    our port number is now that we're bound
                port = server.Port;
                if (port == 0)
                    throw new Exception("RPCServer's port is invalid");
                uri = "http://" + network.host + ":" + port + "/";
            }

            Logger.LogInformation("XmlRpc Server listening at " + uri);
            serverThread = new Thread(serverThreadFunc) { IsBackground = true };
            serverThread.Start();
        }

        internal void shutdown()
        {
            if (shuttingDown)
                return;
            shuttingDown = true;
            serverThread.Join();
            server.Shutdown();
            foreach (CachedXmlRpcClient c in clients)
            {
                while (c.in_use)
                    Thread.Sleep(ROS.WallDuration);
                c.Dispose();
            }
            clients.Clear();
            lock (functionsGate)
            {
                functions.Clear();
            }
            if (server != null)
            {
                foreach (var ass in connections)
                {
                    ass.RemoveFromDispatch(server.Dispatch);
                }
            }
            connections.Clear();
            lock (addedConnectionsGate)
            {
                addedConnections.Clear();
            }
            lock (removedConnectionsGate)
            {
                removedConnections.Clear();
            }

            Logger.LogDebug("XmlRpc Server shutted down.");
        }
    }

    public class CachedXmlRpcClient : IDisposable
    {
        private ILogger Logger { get; } = ApplicationLogging.CreateLogger<CachedXmlRpcClient>();
        private XmlRpcClient client;

        public bool in_use
        {
            get
            {
                lock (busyMutex)
                {
                    return refs != 0;
                }
            }
        }

        public DateTime last_use_time;

        private object busyMutex = new object();
        private object client_lock = new object();
        private volatile int refs;

        internal bool dead
        {
            get
            {
                lock (client_lock)
                {
                    return client == null;
                }
            }
        }

        public CachedXmlRpcClient(string host, int port, string uri)
            : this(new XmlRpcClient(host, port, uri))
        {
        }

        private CachedXmlRpcClient(XmlRpcClient c)
        {
            lock (client_lock)
            {
                client = c;
                client.Disposed += () =>
                {
                    client = null;
                };
            }
        }

        /// <summary>
        ///     Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
        /// </summary>
        public void Dispose()
        {
            lock (busyMutex)
            {
                if (refs != 0)
                    Logger.LogWarning("XmlRpcClient disposed with " + refs + " refs held");
            }

            lock (client_lock)
            {
                if (client != null)
                {
                    client.Dispose();
                    client = null;
                }
            }
        }

        internal void AddRef()
        {
            lock (busyMutex)
            {
                refs++;
            }
            last_use_time = DateTime.Now;
        }

        internal void DelRef()
        {
            lock (busyMutex)
            {
                refs--;
            }
        }

        public bool CheckIdentity(string host, int port, string uri)
        {
            lock (client_lock)
            {
                return client != null && port == client.Port
                    && (host == null || client.Host != null && string.Equals(host, client.Host)) && (uri == null || client.Uri != null && string.Equals(uri, client.Uri));
            }
        }

        #region XmlRpcClient passthrough functions and properties

        public bool Execute(string method, XmlRpcValue parameters, XmlRpcValue result)
        {
            lock (client_lock)
                return client.Execute(method, parameters, result);
        }

        public bool ExecuteNonBlock(string method, XmlRpcValue parameters)
        {
            lock (client_lock)
                return client.ExecuteNonBlock(method, parameters);
        }

        public bool ExecuteCheckDone(XmlRpcValue result)
        {
            lock (client_lock)
                return client.ExecuteCheckDone(result);
        }

        public bool IsConnected
        {
            get
            {
                lock (client_lock) return client != null && client.IsConnected;
            }
        }

        #endregion
    }
}
