using System;
using System.Collections.Generic;
using System.Reflection;

namespace Uml.Robotics.Ros
{
    public class RosMessage
    {

        public static RosMessage generate(string t)
        {
            var result = MessageTypeRegistry.Instance.CreateMessage(t);
            if (result == null)
            {
                throw new ArgumentException($"Could not find a RosMessage for {t}", nameof(t));
            }

            return result;
        }

        public virtual string MD5Sum() { return ""; }

        public virtual bool HasHeader() { return false; }
        public virtual bool IsMetaType() { return false; }
        public virtual string MessageDefinition() { return ""; }
        public byte[] Serialized;
        public virtual string MessageType { get { return "xamla/unkown"; } }
        public virtual bool IsServiceComponent() { return false; }
        public IDictionary<string, string> connection_header;

        public RosMessage()
        {
        }

        public RosMessage(byte[] SERIALIZEDSTUFF)
        {
            Deserialize(SERIALIZEDSTUFF);
        }

        public RosMessage(byte[] SERIALIZEDSTUFF, ref int currentIndex)
        {
            Deserialize(SERIALIZEDSTUFF, ref currentIndex);
        }

        public void Deserialize(byte[] SERIALIZEDSTUFF)
        {
            int start = 0;
            Deserialize(SERIALIZEDSTUFF, ref start);
        }

        public virtual void Deserialize(byte[] SERIALIZEDSTUFF, ref int currentIndex)
        {
            throw new NotImplementedException();
        }

        public byte[] Serialize()
        {
            return Serialize(false);
        }

        public virtual byte[] Serialize(bool partofsomethingelse)
        {
            throw new NotImplementedException();
        }

        public virtual void Randomize()
        {
            throw new NotImplementedException();
        }

        public virtual bool Equals(RosMessage msg)
        {
            throw new NotImplementedException();
        }

        public override bool Equals(object obj)
        {
            return Equals(obj as RosMessage);
        }

        [System.Diagnostics.DebuggerStepThrough]

        public override int GetHashCode()
        {
            return base.GetHashCode();
        }
    }
}
