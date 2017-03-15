using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Threading;

namespace Uml.Robotics.Ros
{
    public class PollSet : Poll_Signal
    {
        private static Dictionary<uint, Socket> socks = new Dictionary<uint, Socket>();

        #region Delegates

        public delegate void SocketUpdateFunc(int stufftodo);

        #endregion

        public PollSet()
            : base(null)
        {
            base.Op = update;
        }

        /// <summary>
        ///     Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
        /// </summary>
        public new void Dispose()
        {
            base.Dispose();
            if (DisposingEvent != null)
                DisposingEvent.Invoke();
        }

        public delegate void DisposingDelegate();

        public event DisposingDelegate DisposingEvent;
        
        public bool addSocket(Socket s, SocketUpdateFunc update_func)
        {
            return addSocket(s, update_func, null);
        }

        public bool addSocket(Socket s, SocketUpdateFunc update_func, TcpTransport trans)
        {
            s.Info = new SocketInfo { sock = s.FD, func = update_func, transport = trans };
            lock (socks)
            {
                socks.Add(s.FD, s);
            }
            return true;
        }

        public bool delSocket(Socket s)
        {
            lock (socks)
            {
                socks.Remove(s.FD);
            }
            s.Dispose();
            return true;
        }

        public bool addEvents(Socket s, int events)
        {
            if (s != null && s.Info != null)
                s.Info.events |= events;
            return true;
        }

        public bool delEvents(Socket s, int events)
        {
            if (s != null && s.Info != null)
                s.Info.events &= ~events;
            return true;
        }

        public void update()
        {
            List<System.Net.Sockets.Socket> checkWrite = new List<System.Net.Sockets.Socket>();
            List<System.Net.Sockets.Socket> checkRead = new List<System.Net.Sockets.Socket>();
            List<System.Net.Sockets.Socket> checkError = new List<System.Net.Sockets.Socket>();
            List<Uml.Robotics.Ros.Socket> lsocks = new List<Uml.Robotics.Ros.Socket>();
            lock (socks)
            {
                foreach (Socket s in socks.Values)
                {
                    lsocks.Add(s);
                    if ((s.Info.events & Socket.POLLIN) != 0)
                        checkRead.Add(s.realsocket);
                    if ((s.Info.events & Socket.POLLOUT) != 0)
                        checkWrite.Add(s.realsocket);
                    if ((s.Info.events & (Socket.POLLERR | Socket.POLLHUP | Socket.POLLNVAL)) != 0)
                        checkError.Add(s.realsocket);
                }
            }
            if (lsocks.Count == 0 || (checkRead.Count == 0 && checkWrite.Count == 0 && checkError.Count == 0))
                return;

            try
            {
                System.Net.Sockets.Socket.Select(checkRead, checkWrite, checkError, -1);
            }
            catch
            {
                return;
            }
            int nEvents = checkRead.Count + checkWrite.Count + checkError.Count;

            if (nEvents == 0)
                return;

            // Process events
            foreach (var record in lsocks)
            {
                int newmask = 0;
                if (checkRead.Contains(record.realsocket))
                    newmask |= Socket.POLLIN;
                if (checkWrite.Contains(record.realsocket))
                    newmask |= Socket.POLLOUT;
                if (checkError.Contains(record.realsocket))
                    newmask |= Socket.POLLERR;
                record._poll(newmask);
            }
        }
    }

    public class SocketInfo
    {
        public int events;
        public PollSet.SocketUpdateFunc func;
        public int revents;
        public uint sock;
        public TcpTransport transport;
    }
}
