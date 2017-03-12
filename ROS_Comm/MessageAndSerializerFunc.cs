using Messages;

namespace Uml.Robotics.Ros
{
    internal class MessageAndSerializerFunc
    {
        internal IRosMessage msg;
        internal bool nocopy;
        internal TopicManager.SerializeFunc serfunc;
        internal bool serialize;

        internal MessageAndSerializerFunc(IRosMessage msg, TopicManager.SerializeFunc serfunc, bool ser, bool nc)
        {
            this.msg = msg;
            this.serfunc = serfunc;
            serialize = ser;
            nocopy = nc;
        }
    }
}
