#define TCPSERVER

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using m = Messages.std_msgs;
using gm = Messages.geometry_msgs;
using nm = Messages.nav_msgs;
using System.Threading;
using Microsoft.Extensions.Logging;

namespace Uml.Robotics.Ros
{
    public class ConnectionManager
    {
        private ILogger Logger { get; } = ApplicationLogging.CreateLogger<ConnectionManager>(); 
        private static Lazy<ConnectionManager> _instance = new Lazy<ConnectionManager>(LazyThreadSafetyMode.ExecutionAndPublication);

        public static ConnectionManager Instance
        {
            get { return _instance.Value; }
        }

        private uint connection_id_counter;
        private object connection_id_counter_mutex = new object();
        private List<Connection> connections = new List<Connection>();
        private object connections_mutex = new object();
        private List<Connection> dropped_connections = new List<Connection>();
        private object dropped_connections_mutex = new object();
#if TCPSERVER
        public TcpListener tcpserver_transport;
#else
        public TcpTransport tcpserver_transport;
#endif

        public int TCPPort
        {
            get
            {
#if TCPSERVER
                if (tcpserver_transport == null || tcpserver_transport.LocalEndpoint == null)
                    return -1;
                return ((IPEndPoint) tcpserver_transport.LocalEndpoint).Port;
#else
                if (tcpserver_transport == null || tcpserver_transport.LocalEndPoint == null)
                    return -1;
                return ((IPEndPoint)tcpserver_transport.LocalEndPoint).Port;
#endif
            }
        }

        public uint GetNewConnectionID()
        {
            lock (connection_id_counter_mutex)
            {
                return connection_id_counter++;
            }
        }

        public void addConnection(Connection connection)
        {
            lock (connections_mutex)
            {
                connections.Add(connection);
                connection.DroppedEvent += onConnectionDropped;
            }
        }

        public void Clear(Connection.DropReason reason)
        {
            removeDroppedConnections();
            List<Connection> local_connections = null;
            lock (connections_mutex)
            {
                local_connections = new List<Connection>(connections);
                connections.Clear();
            }
            foreach (Connection c in local_connections)
            {
                if (!c.dropped)
                    c.drop(reason);
            }
            lock (dropped_connections_mutex)
                dropped_connections.Clear();
        }

        private void onConnectionDropped(Connection conn, Connection.DropReason r)
        {
            lock (dropped_connections_mutex)
                dropped_connections.Add(conn);
        }

        private void removeDroppedConnections()
        {
            List<Connection> local_dropped = null;
            lock (dropped_connections_mutex)
            {
                local_dropped = new List<Connection>(dropped_connections);
                dropped_connections.Clear();
            }
            lock (connections_mutex)
            {
                foreach (Connection c in local_dropped)
                {
                    Logger.LogDebug("Removing dropped connection: " + c.CallerID);
                    connections.Remove(c);
                }
            }
        }

        public void shutdown()
        {
#if TCPSERVER
            acceptor.Stop();
#endif
            if (tcpserver_transport != null)
            {
#if TCPSERVER
                tcpserver_transport.Stop();
                tcpserver_transport = null;
#else
                tcpserver_transport.close();
                tcpserver_transport = null;
#endif
            }
            PollManager.Instance.removePollThreadListener(removeDroppedConnections);

            Clear(Connection.DropReason.Destructing);
        }

        public void tcpRosAcceptConnection(TcpTransport transport)
        {
            Connection conn = new Connection();
            addConnection(conn);
            conn.initialize(transport, true, onConnectionHeaderReceived);
        }

        public bool onConnectionHeaderReceived(Connection conn, Header header)
        {
            bool ret = false;
            if (header.Values.ContainsKey("topic"))
            {
                TransportSubscriberLink sub_link = new TransportSubscriberLink();
                ret = sub_link.initialize(conn);
                ret &= sub_link.handleHeader(header);
            }
            else if (header.Values.ContainsKey("service"))
            {
                IServiceClientLink iscl = new IServiceClientLink();
                ret = iscl.initialize(conn);
                ret &= iscl.handleHeader(header);
            }
            else
            {
                Logger.LogWarning("Got a connection for a type other than topic or service from [" + conn.RemoteString +
                              "].");
                return false;
            }
            //Logger.LogDebug("CONNECTED [" + val + "]. WIN.");
            return ret;
        }

#if TCPSERVER
        public void CheckAndAccept(object nothing)
        {
            while (tcpserver_transport != null && tcpserver_transport.Pending())
            {
                tcpRosAcceptConnection(new TcpTransport(tcpserver_transport.AcceptSocketAsync().Result, PollManager.Instance.poll_set));
            }
        }

        private WrappedTimer acceptor;
#endif

        public void Start()
        {
            PollManager.Instance.addPollThreadListener(removeDroppedConnections);

#if TCPSERVER
            tcpserver_transport = new TcpListener(IPAddress.Any, network.tcpros_server_port);
            tcpserver_transport.Start(10);
            acceptor = ROS.timer_manager.StartTimer(CheckAndAccept, 100, 100);
#else
            tcpserver_transport = new TcpTransport(PollManager.Instance.poll_set);
            tcpserver_transport.listen(network.tcpros_server_port, 10, (t)=> {
                tcpRosAcceptConnection(t.accept());
            });
#endif
        }
    }
}
