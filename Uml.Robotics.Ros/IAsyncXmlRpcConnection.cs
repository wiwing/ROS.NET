using Uml.Robotics.XmlRpc;

namespace Uml.Robotics.Ros
{
    public interface IAsyncXmlRpcConnection
    {
        void AddToDispatch(XmlRpcDispatch disp);
        void RemoveFromDispatch(XmlRpcDispatch disp);
        bool Check();
    }
}
