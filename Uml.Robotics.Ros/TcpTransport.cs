using System;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using Microsoft.Extensions.Logging;

namespace Uml.Robotics.Ros
{
    public class TcpTransport
    {
        public delegate void AcceptCallback(TcpTransport trans);
        public delegate void DisconnectFunc(TcpTransport trans);
        public delegate void HeaderReceivedFunc(TcpTransport trans, Header header);
        public delegate void ReadFinishedFunc(TcpTransport trans);
        public delegate void WriteFinishedFunc(TcpTransport trans);

        [Flags]
        public enum Flags
        {
            SYNCHRONOUS = 1 << 0
        }

        public static bool use_keepalive = true;

        private ILogger Logger { get; } = ApplicationLogging.CreateLogger<TcpTransport>();

        const int BITS_PER_BYTE = 8;
        const int POLLERR = 0x008;
        const int POLLHUP = 0x010;
        const int POLLNVAL = 0x020;
        const int POLLIN = 0x001;
        const int POLLOUT = 0x004;

        public IPEndPoint LocalEndPoint;
        public string _topic;
        public string connected_host;
        public int connected_port;
        public string cached_remote_host = "";

        private object close_mutex = new object();
        private bool closed;
        private bool expecting_read;
        private bool expecting_write;
        private int flags;
        private bool is_server;
        private PollSet poll_set;
        private int server_port = -1;

        private Socket sock;

        public TcpTransport()
        {
        }


        public TcpTransport(System.Net.Sockets.Socket s, PollSet pollset)
            : this(s, pollset, 0)
        {
        }

        public TcpTransport(System.Net.Sockets.Socket s, PollSet pollset, int flags)
            : this(pollset, flags)
        {
            setSocket(new Socket(s));
        }

        public TcpTransport(PollSet pollset)
            : this(pollset, 0)
        {
        }

        public TcpTransport(PollSet pollset, int flags)
        {
            if (pollset != null)
            {
                poll_set = pollset;
                poll_set.DisposingEvent += close;
            }
            else
            {
                Logger.LogError("Null pollset in tcptransport ctor");
            }
            this.flags = flags;
        }

        public string ClientUri
        {
            get
            {
                if (connected_host == null || connected_port == 0)
                    return "[NOT CONNECTED]";
                return "http://" + connected_host + ":" + connected_port + "/";
            }
        }

        public string Topic
        {
            get { return _topic != null ? _topic : "?!?!?!"; }
        }

        public virtual bool getRequiresHeader()
        {
            return true;
        }

        public event AcceptCallback accept_cb;
        public event DisconnectFunc disconnect_cb;
        public event WriteFinishedFunc write_cb;
        public event ReadFinishedFunc read_cb;

        public bool setNonBlocking()
        {
            if ((flags & (int) Flags.SYNCHRONOUS) == 0)
            {
                try
                {
                    sock.Blocking = false;
                }
                catch (Exception e)
                {
                    Logger.LogError(e.ToString());
                    close();
                    return false;
                }
            }

            return true;
        }

        public void setNoDelay(bool nd)
        {
            try
            {
                sock.NoDelay = nd;
            }
            catch (Exception e)
            {
                Logger.LogError(e.ToString());
            }
        }

        public void enableRead()
        {
            if (sock == null)
                return;
            if (!sock.Connected)
                close();
            lock (close_mutex)
            {
                if (closed)
                    return;
            }
            if (!expecting_read && poll_set != null)
            {
                //Console.WriteLine("ENABLE READ:   " + Topic + "(" + sock.FD + ")");
                expecting_read = true;
                poll_set.addEvents(sock, POLLIN);
            }
        }

        public void disableRead()
        {
            if (sock == null)
                return;
            if (!sock.Connected)
                close();
            lock (close_mutex)
            {
                if (closed)
                    return;
            }
            if (expecting_read && poll_set != null)
            {
                //Console.WriteLine("DISABLE READ:  " + Topic + "(" + sock.FD + ")");
                poll_set.delEvents(sock, POLLIN);
                expecting_read = false;
            }
        }

        public void enableWrite()
        {
            if (sock == null)
                return;
            if (!sock.Connected) close();
            lock (close_mutex)
            {
                if (closed)
                    return;
            }
            if (!expecting_write && poll_set != null)
            {
                //Console.WriteLine("ENABLE WRITE:  " + Topic + "(" + sock.FD + ")");
                expecting_write = true;
                poll_set.addEvents(sock, POLLOUT);
            }
        }

        public void disableWrite()
        {
            if (sock == null)
                return;
            if (!sock.Connected) close();
            lock (close_mutex)
            {
                if (closed)
                    return;
            }
            if (expecting_write && poll_set != null)
            {
                //Console.WriteLine("DISABLE WRITE: " + Topic + "(" + sock.FD + ")");
                poll_set.delEvents(sock, POLLOUT);
                expecting_write = false;
            }
        }

        public bool connect(string host, int port)
        {
            sock = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            connected_host = host;
            connected_port = port;
            if (!setNonBlocking())
                throw new Exception("Failed to make socket nonblocking");
            setNoDelay(true);

            IPAddress ip;
            if (!IPAddress.TryParse(host, out ip))
            {
                ip = Dns.GetHostAddressesAsync(host).Result.Where(x => !x.ToString().Contains(":")).FirstOrDefault();
                if (ip == null)
                {
                    close();
                    Logger.LogError("Couldn't resolve host name [{0}]", host);
                    return false;
                }
            }

            if (ip == null)
                return false;

            IPEndPoint ipep = new IPEndPoint(ip, port);
            LocalEndPoint = ipep;
            DateTime connectionAttempted = DateTime.UtcNow;
            IAsyncResult asyncres;

            asyncres = sock.BeginConnect(ipep, iar =>
            {
                lock(this)
                    if (sock != null)
                        try
                        {
                            sock.EndConnect(iar);
                        }
                        catch (Exception e)
                        {
                            Logger.LogError(e.ToString());
                        }
            }, null);

            bool completed = false;
            while (ROS.ok && !ROS.shutting_down)
            {
                completed = asyncres.AsyncWaitHandle.WaitOne(10);
                if (completed)
                    break;
                if (DateTime.UtcNow.Subtract(connectionAttempted).TotalSeconds >= 3)
                {
                    Logger.LogInformation("Trying to connect for " + DateTime.UtcNow.Subtract(connectionAttempted).TotalSeconds + "s\t: " + this);
                    if (!asyncres.AsyncWaitHandle.WaitOne(100))
                    {
                        sock.Close();
                        sock = null;
                    }
                }
            }

            if (!completed || sock == null || !sock.Connected)
            {
                return false;
            } else
            {
                Logger.LogDebug("TcpTransport connection established.");
            }
            return ROS.ok && initializeSocket();
        }

        public bool listen(int port, int backlog, AcceptCallback accept_cb)
        {
            is_server = true;
            this.accept_cb = accept_cb;

            sock = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            setNonBlocking();
            sock.Bind(new IPEndPoint(IPAddress.Any, port));
            server_port = ((IPEndPoint) sock.LocalEndPoint).Port;
            sock.Listen(backlog);
            if (!initializeSocket())
                return false;
            if ((flags & (int)Flags.SYNCHRONOUS) == 0)
                enableRead();
            return true;
        }

        public void parseHeader(Header header)
        {
            if (_topic == null)
            {
                if (header.Values.ContainsKey("topic"))
                    _topic = header.Values["topic"].ToString();
            }

            if (header.Values.ContainsKey("tcp_nodelay"))
            {
                var nodelay = (string)header.Values["tcp_nodelay"];
                if (nodelay == "1")
                {
                    setNoDelay(true);
                }
            }
        }

        private bool TrySetKeepAlive(Socket sock, uint time, uint interval)
        {
            try
            {
                sock.SetTcpKeepAlive(time, interval);
            }
            catch (Exception)
            {
                return false;
            }
            return true;
        }

        public void setKeepAlive(bool use, int idle, int interval, int count)
        {
            if (use)
            {
                if (!TrySetKeepAlive(sock, (uint)idle, (uint)interval))
                {
                    try
                    {
                        sock.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.KeepAlive, use);
                    }
                    catch (Exception e)
                    {
                        Logger.LogError(e.ToString());
                        return;
                    }
                }
            }
        }

        public int read(byte[] buffer, int pos, int length)
        {
            lock (close_mutex)
            {
                if (closed)
                    return -1;
            }
            int num_bytes = 0;
            SocketError err;
            num_bytes = sock.realsocket.Receive(buffer, pos, length, SocketFlags.None, out err);
            if (num_bytes < 0)
            {
                if (err == SocketError.TryAgain || err == SocketError.WouldBlock)
                    num_bytes = 0;
                else if (err != SocketError.InProgress && err != SocketError.IsConnected && err != SocketError.Success)
                {
                    close();
                    num_bytes = -1;
                }
            }
            return num_bytes;
        }

        public int write(byte[] buffer, int pos, int size)
        {
            lock (close_mutex)
            {
                if (closed)
                    return -1;
            }
            SocketError err;
            //Logger.LogDebug(ByteDumpCondensed(buffer));
            int num_bytes = sock.Send(buffer, pos, size, SocketFlags.None, out err);
            if (num_bytes <= 0)
            {
                if (err == SocketError.TryAgain || err == SocketError.WouldBlock)
                    num_bytes = 0;
                else if (err != SocketError.InProgress && err != SocketError.IsConnected && err != SocketError.Success)
                {
                    close();
                    return -1;
                }
                else
                    return 0;
            }
            return num_bytes;
        }

        private bool initializeSocket()
        {
            if (!setNonBlocking())
                return false;
            setNoDelay(true);
            setKeepAlive(use_keepalive, 60, 10, 9);

            if (string.IsNullOrEmpty(cached_remote_host))
            {
                if (is_server)
                    cached_remote_host = "TCPServer Socket";
                else
                    cached_remote_host = this.ClientUri + " on socket " + sock.realsocket.RemoteEndPoint.ToString();
            }

            if (poll_set != null)
            {
                poll_set.addSocket(sock, socketUpdate, this);
            }
            if (!is_server && !sock.Connected)
            {
                close();
                return false;
            }
            return true;
        }

        private bool setSocket(Socket s)
        {
            sock = s;
            return initializeSocket();
        }

        public TcpTransport accept()
        {
            SocketAsyncEventArgs args = new SocketAsyncEventArgs();
            if (sock == null || !sock.AcceptAsync(args))
                return null;
            if (args.AcceptSocket == null)
            {
                Logger.LogError("Nothing to accept, return null");
                return null;
            }
            Socket acc = new Socket(args.AcceptSocket);
            TcpTransport transport = new TcpTransport(poll_set, flags);
            if (!transport.setSocket(acc))
            {
                throw new InvalidOperationException("Could not add socket to transport");
            }
            return transport;
        }

        public override string ToString()
        {
            return "TCPROS connection to [" + sock + "]";
        }

        private void socketUpdate(int events)
        {
            lock (close_mutex)
            {
                if (closed)
                    return;
            }

            if (is_server)
            {
                TcpTransport transport = accept();
                if (transport != null)
                {
                    if (accept_cb == null) throw new NullReferenceException("Accept callback is null");
                    accept_cb(transport);
                }
            }
            else
            {
                if ((events & POLLIN) != 0 && expecting_read) //POLL IN FLAG
                {
                    if (read_cb != null)
                    {
                        read_cb(this);
                    }
                }

                if ((events & POLLOUT) != 0 && expecting_write)
                {
                    if (write_cb != null)
                        write_cb(this);
                }

                if ((events & POLLERR) != 0 || (events & POLLHUP) != 0 || (events & POLLNVAL) != 0)
                {
                    int error = 0;
                    try
                    {
                        error = (int) sock.GetSocketOption(SocketOptionLevel.Socket, SocketOptionName.Error);
                    }
                    catch (Exception e)
                    {
                        Logger.LogError("Failed to get sock options! (error: " + error + ")" + e);
                    }
                    if (error != 0)
                        Logger.LogError("Socket error = " + error);
                    close();
                }
            }
        }

        public void close()
        {
            DisconnectFunc disconnect_cb = null;
            lock (close_mutex)
            {
                if (!closed)
                {
                    closed = true;
                    if (poll_set != null)
                        poll_set.delSocket(sock);
                    if (sock.Connected)
                        sock.Shutdown(SocketShutdown.Both);
                    sock.Close();
                    sock = null;
                    disconnect_cb = this.disconnect_cb;
                    this.disconnect_cb = null;
                    read_cb = null;
                    write_cb = null;
                    accept_cb = null;
                }
            }
            if (disconnect_cb != null)
            {
                disconnect_cb(this);
            }
        }
    }
}
