using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using String = Messages.std_msgs.String;
using uint8 = System.Byte;

namespace Messages
{
    public class RosMessage
    {
        internal static Dictionary<MsgTypes, Func<MsgTypes, RosMessage>> constructors = new Dictionary<MsgTypes, Func<MsgTypes, RosMessage>>();
        private static Dictionary<MsgTypes, Type> _typeregistry = new Dictionary<MsgTypes, Type>();

        public static RosMessage generate(MsgTypes t)
        {
            lock (constructors)
            {
                if (constructors.ContainsKey(t))
                    return constructors[t].Invoke(t);

                Type thistype = typeof (RosMessage);
                foreach (Type othertype in thistype.GetTypeInfo().Assembly.GetTypes())
                {
                    if (thistype == othertype || !othertype.GetTypeInfo().IsSubclassOf(thistype))
                    {
                        continue;
                    }

                    RosMessage msg = Activator.CreateInstance(othertype) as RosMessage;
                    if (msg != null)
                    {
                        if (msg.msgtype() == MsgTypes.Unknown)
                            throw new Exception("Invalid message type. Message type field (msgtype) was not initialized correctly.");
                        if (!_typeregistry.ContainsKey(msg.msgtype()))
                            _typeregistry.Add(msg.msgtype(), msg.GetType());
                        if (!constructors.ContainsKey(msg.msgtype()))
                            constructors.Add(msg.msgtype(), T => Activator.CreateInstance(_typeregistry[T]) as RosMessage);
                    }
                }

                return constructors[t].Invoke(t);
            }
        }

        public virtual string MD5Sum() { return ""; }
        public virtual bool HasHeader() { return false; }
        public virtual bool IsMetaType() { return false; }
        public virtual string MessageDefinition() { return ""; }
        public byte[] Serialized;
        public virtual MsgTypes msgtype() { return MsgTypes.Unknown; }
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

    public delegate RosMessage RosServiceDelegate(RosMessage request);

    public class RosService
    {
        public virtual string MD5Sum() { return ""; }

        public virtual string ServiceDefinition() { return ""; }

        public virtual SrvTypes srvtype() { return SrvTypes.Unknown; }

        public MsgTypes msgtype_req
        {
            get { return RequestMessage.msgtype(); }
        }

        public MsgTypes msgtype_res
        {
            get { return ResponseMessage.msgtype(); }
        }

        public RosMessage RequestMessage, ResponseMessage;

        protected RosMessage GeneralInvoke(RosServiceDelegate invocation, RosMessage m)
        {
            return invocation.Invoke(m);
        }

        public RosService()
        {
        }

        protected void InitSubtypes(RosMessage request, RosMessage response)
        {
            RequestMessage = request;
            ResponseMessage = response;
        }

        internal static Dictionary<SrvTypes, Func<SrvTypes, RosService>> constructors = new Dictionary<SrvTypes, Func<SrvTypes, RosService>>();
        private static Dictionary<SrvTypes, Type> _typeregistry = new Dictionary<SrvTypes, Type>();

        public static RosService generate(SrvTypes t)
        {
            lock (constructors)
            {
                if (constructors.ContainsKey(t))
                    return constructors[t].Invoke(t);
                Type thistype = typeof (RosService);
                foreach (Type othertype in thistype.GetTypeInfo().Assembly.GetTypes())
                {
                    if (thistype == othertype || !othertype.GetTypeInfo().IsSubclassOf(thistype))
                    {
                        continue;
                    }

                    RosService srv = Activator.CreateInstance(othertype) as RosService;
                    if (srv != null)
                    {
                        if (srv.srvtype() == SrvTypes.Unknown)
                            throw new Exception("Invalid servive type. Service type field (srvtype) was not initialized correctly.");
                        if (!_typeregistry.ContainsKey(srv.srvtype()))
                            _typeregistry.Add(srv.srvtype(), srv.GetType());
                        if (!constructors.ContainsKey(srv.srvtype()))
                            constructors.Add(srv.srvtype(), T => Activator.CreateInstance(_typeregistry[T]) as RosService);
                        srv.RequestMessage = RosMessage.generate((MsgTypes) Enum.Parse(typeof (MsgTypes), srv.srvtype() + "__Request"));
                        srv.ResponseMessage = RosMessage.generate((MsgTypes) Enum.Parse(typeof (MsgTypes), srv.srvtype() + "__Response"));
                    }
                }

                return constructors[t].Invoke(t);
            }
        }
    }

    public enum ServiceMessageType
    {
        Not,
        Request,
        Response
    }

    public struct TimeData
    {
        public uint sec;
        public uint nsec;

        public TimeData(uint s, uint ns)
        {
            sec = s;
            nsec = ns;
        }

        public bool Equals(TimeData timer)
        {
            return (sec == timer.sec && nsec == timer.nsec);
        }
    }


    public interface IActionGoal
    {
        Messages.std_msgs.Header Header { get; set; }
        Messages.actionlib_msgs.GoalID GoalId { get; set; }
        RosMessage Goal { get; set; }
    }


    public interface IActionResult
    {
        Messages.std_msgs.Header Header { get; set; }
        Messages.actionlib_msgs.GoalStatus GoalStatus { get; set; }
        RosMessage Result { get; set; }
    }


    public interface IActionFeedback
    {
        Messages.std_msgs.Header Header { get; set; }
        Messages.actionlib_msgs.GoalStatus GoalStatus { get; set; }
        RosMessage Feedback { get; set; }
    }
}
