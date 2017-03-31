using System;
using Uml.Robotics.XmlRpc;


namespace Uml.Robotics.Ros
{
    public class PendingConnection : IAsyncXmlRpcConnection, IDisposable
    {
        public string RemoteUri;
        private int _failures;
        private XmlRpcValue chk;
        public XmlRpcClient client;
        public Subscription parent;

        public PendingConnection(XmlRpcClient client, Subscription s, string uri, XmlRpcValue chk)
        {
            this.client = client;
            this.chk = chk;
            parent = s;
            RemoteUri = uri;
        }

        #region IDisposable Members

        public void Dispose()
        {
            chk = null; //.Dispose();
            client.Dispose();
            client = null;
        }

        #endregion

        public int failures
        {
            get { return _failures; }
            set { _failures = value; }
        }

        public void AddToDispatch(XmlRpcDispatch disp)
        {
            if (disp == null)
                return;
            if (Check())
                return;
            disp.AddSource(client, (XmlRpcDispatch.EventType.WritableEvent | XmlRpcDispatch.EventType.Exception));
        }

        public void RemoveFromDispatch(XmlRpcDispatch disp)
        {
            disp.RemoveSource(client);
        }

        public bool Check()
        {
            if (parent == null)
                return false;
            if (client.ExecuteCheckDone(chk))
            {
                parent.pendingConnectionDone(this, chk);
                return true;
            }
            return false;
        }
    }
}
