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
        public static XmlRpcManager Instance
        {
            get { return instance.Value; }
        }


        private static Lazy<XmlRpcManager> instance = new Lazy<XmlRpcManager>(LazyThreadSafetyMode.ExecutionAndPublication);
        private ILogger Logger { get; } = ApplicationLogging.CreateLogger<XmlRpcManager>();

        private class FunctionInfo
        {
            public XmlRpcFunc function;
            public string name;
            public XmlRpcServerMethod wrapper;
        }

        private Dictionary<string, FunctionInfo> functions = new Dictionary<string, FunctionInfo>();
        private object functionsGate = new object();
        private XmlRpcFunc getPid;
        private XmlRpcServer server;
        private Thread serverThread;
        private bool shuttingDown;
        private bool unbindRequested;
        private string uri = "";
        private int port;


        public static Action<XmlRpcValue> ResponseStr(IntPtr target, int code, string msg, string response)
        {
            return (XmlRpcValue v) =>
            {
                v.Set(0, code);
                v.Set(1, msg);
                v.Set(2, response);
            };
        }


        public static Action<XmlRpcValue> ResponseInt(int code, string msg, int response)
        {
            return (XmlRpcValue v) =>
            {
                v.Set(0, code);
                v.Set(1, msg);
                v.Set(2, response);
            };
        }


        public static Action<XmlRpcValue> ResponseBool(int code, string msg, bool response)
        {
            return (XmlRpcValue v) =>
            {
                v.Set(0, code);
                v.Set(1, msg);
                v.Set(2, response);
            };
        }


        public static void Terminate()
        {
            XmlRpcManager.Instance.Shutdown();
        }


        public static void Reset()
        {
            instance = new Lazy<XmlRpcManager>(LazyThreadSafetyMode.ExecutionAndPublication);
        }


        public XmlRpcManager()
        {
            this.server = new XmlRpcServer();
            this.getPid = (parms, result) => ResponseInt(1, "", Process.GetCurrentProcess().Id)(result);
        }


        public string Uri
        {
            get { return uri; }
        }


        public bool IsShuttingDown
        {
            get { return shuttingDown; }
        }


        public void Dispose()
        {
            Shutdown();
        }


        public void ServerThreadFunc()
        {
            while (!shuttingDown)
            {
                if (server.Dispatch == null)
                {
                    throw new NullReferenceException("XmlRpcManager is not initialized yet!");
                }

                lock (functionsGate)
                {
                    server.Work(0.1);
                }

                while (unbindRequested)
                {
                    Thread.Sleep(ROS.WallDuration);
                }
            }
        }


        public bool ValidateXmlRpcResponse(string method, XmlRpcValue response, XmlRpcValue payload)
        {
            if (response.Type != XmlRpcType.Array)
                return ValidateFailed(method, "didn't return an array -- {0}", response);
            if (response.Count != 3)
                return ValidateFailed(method, "didn't return a 3-element array -- {0}", response);
            if (response[0].Type != XmlRpcType.Int)
                return ValidateFailed(method, "didn't return an int as the 1st element -- {0}", response);
            int status_code = response[0].GetInt();
            if (response[1].Type != XmlRpcType.String)
                return ValidateFailed(method, "didn't return a string as the 2nd element -- {0}", response);

            string status_string = response[1].GetString();
            if (status_code != 1)
            {
                return ValidateFailed(method, "returned an error ({0}): [{1}] -- {2}", status_code, status_string, response);
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


        private bool ValidateFailed(string method, string errorFormat, params object[] args)
        {
            Logger.LogDebug("XML-RPC Call [{0}] {1} failed validation", method, string.Format(errorFormat, args));
            return false;
        }


        public bool Bind(string function_name, XmlRpcFunc cb)
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


        public void Unbind(string function_name)
        {
            unbindRequested = true;
            lock (functionsGate)
            {
                functions.Remove(function_name);
            }
            unbindRequested = false;
        }



        /// <summary>
        /// This function starts the XmlRpcServer used to handle inbound calls on this node
        /// </summary>
        /// <param name="p">The optional argument is used to force ROS to try to bind to a specific port.
        /// Doing so should only be done when acting as the RosMaster.</param>
        public void Start(int p = 0)
        {
            shuttingDown = false;

            Bind("getPid", getPid);

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
            serverThread = new Thread(ServerThreadFunc) { IsBackground = true };
            serverThread.Start();
        }


        internal void Shutdown()
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

            Logger.LogDebug("XmlRpc Server shutted down.");
        }
    }
}
