namespace Uml.Robotics.XmlRpc
{
    public abstract class AsyncXmlRpcConnection
    {
        public abstract void addToDispatch(XmlRpcDispatch disp);
        public abstract void removeFromDispatch(XmlRpcDispatch disp);
        public abstract bool check();
    }
}
