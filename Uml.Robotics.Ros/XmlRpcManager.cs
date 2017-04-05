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

        private static Lazy<XmlRpcManager> instance = new Lazy<XmlRpcManager>(LazyThreadSafetyMode.ExecutionAndPublication);

        public static XmlRpcManager Instance
        {
            get { return instance.Value; }
        }

        ILogger Logger { get; } = ApplicationLogging.CreateLogger<XmlRpcManager>();

        List<IAsyncXmlRpcConnection> addedConnections = new List<IAsyncXmlRpcConnection>();
        List<IAsyncXmlRpcConnection> removedConnections = new List<IAsyncXmlRpcConnection>();

        List<IAsyncXmlRpcConnection> connections = new List<IAsyncXmlRpcConnection>();
        Dictionary<string, FunctionInfo> functions = new Dictionary<string, FunctionInfo>();

        object addedConnectionsGate = new object();
        object removedConnectionsGate = new object();
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
            if (response.Type != XmlRpcType.Array)
                return validateFailed(method, "didn't return an array -- {0}", response);
            if (response.Count != 3)
                return validateFailed(method, "didn't return a 3-element array -- {0}", response);
            if (response[0].Type != XmlRpcType.Int)
                return validateFailed(method, "didn't return an int as the 1st element -- {0}", response);
            int status_code = response[0].GetInt();
            if (response[1].Type != XmlRpcType.String)
                return validateFailed(method, "didn't return a string as the 2nd element -- {0}", response);

            string status_string = response[1].GetString();
            if (status_code != 1)
            {
                return validateFailed(method, "returned an error ({0}): [{1}] -- {2}", status_code, status_string, response);
            }

            switch (response[2].Type)
            {
                case XmlRpcType.Array:
                    {
                        payload.SetArray(0);
                        for (int i = 0; i < response[2].Count; i++)
                        {
                            payload.Set(i, response[2][i]);
                        }
                    }
                    break;
                case XmlRpcType.Int:
                case XmlRpcType.Double:
                case XmlRpcType.String:
                case XmlRpcType.Boolean:
                    payload.Copy(response[2]);
                    break;
                case XmlRpcType.Empty:
                    break;
                default:
                    throw new ArgumentException("Unhandled valid XML-RPC payload type: " + response[2].Type, nameof(response));
            }
            return true;
        }

        private bool validateFailed(string method, string errorFormat, params object[] args)
        {
            Logger.LogDebug("XML-RPC Call [{0}] {1} failed validation", method, string.Format(errorFormat, args));
            return false;
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


        public static Action<XmlRpcValue> responseStr(IntPtr target, int code, string msg, string response)
        {
            return (XmlRpcValue v) =>
            {
                v.Set(0, code);
                v.Set(1, msg);
                v.Set(2, response);
            };
        }

        public static Action<XmlRpcValue> responseInt(int code, string msg, int response)
        {
            return (XmlRpcValue v) =>
            {
                v.Set(0, code);
                v.Set(1, msg);
                v.Set(2, response);
            };
        }

        public static Action<XmlRpcValue> responseBool(int code, string msg, bool response)
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
}
