using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
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
        private TcpListener tcpserver_transport;


        public int TCPPort
        {
            get
            {
                if (tcpserver_transport == null || tcpserver_transport.LocalEndpoint == null)
                    return -1;
                return ((IPEndPoint) tcpserver_transport.LocalEndpoint).Port;
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
            acceptor.Stop();

            if (tcpserver_transport != null)
            {
                tcpserver_transport.Stop();
                tcpserver_transport = null;
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
                ret = sub_link.Initialize(conn);
                ret &= sub_link.HandleHeader(header);
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

        public void CheckAndAccept(object nothing)
        {
            while (tcpserver_transport != null && tcpserver_transport.Pending())
            {
                tcpRosAcceptConnection(new TcpTransport(tcpserver_transport.AcceptSocketAsync().Result, PollManager.Instance.poll_set));
            }
        }

        private WrappedTimer acceptor;

        public void Start()
        {
            PollManager.Instance.addPollThreadListener(removeDroppedConnections);

            tcpserver_transport = new TcpListener(IPAddress.Any, network.tcpros_server_port);
            tcpserver_transport.Start(10);
            acceptor = ROS.timer_manager.StartTimer(CheckAndAccept, 100, 100);

        }
    }
}
