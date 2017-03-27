using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using String = Messages.std_msgs.String;
using System.Security.Cryptography;
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

                Type thistype = typeof(RosMessage);
                foreach (Type othertype in thistype.GetTypeInfo().Assembly.GetTypes())
                {
                    if (thistype == othertype || !othertype.GetTypeInfo().IsSubclassOf(thistype))
                    {
                        continue;
                    }

                    var othertypeInfo = othertype.GetTypeInfo();
                    if (othertype == typeof(InnerActionMessage) || othertypeInfo.ContainsGenericParameters)
                    {
                        continue;
                    }

                    RosMessage msg = Activator.CreateInstance(othertype) as RosMessage;
                    if (msg != null)
                    {
                        if (msg.msgtype() == MsgTypes.Unknown)
                        {
                            throw new Exception("Invalid message type. Message type field (msgtype) was not initialized correctly.");
                        }
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
                Type thistype = typeof(RosService);
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
                        srv.RequestMessage = RosMessage.generate((MsgTypes)Enum.Parse(typeof(MsgTypes), srv.srvtype() + "__Request"));
                        srv.ResponseMessage = RosMessage.generate((MsgTypes)Enum.Parse(typeof(MsgTypes), srv.srvtype() + "__Response"));
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


    public class InnerActionMessage : RosMessage
    {

    }


    public class WrappedFeedbackMessage<T>: RosMessage where T : InnerActionMessage, new()
    {
        public Messages.std_msgs.Header Header { get; set; }
        public Messages.actionlib_msgs.GoalStatus GoalStatus { get; set; }
        protected T Content { get; set; }


        public WrappedFeedbackMessage() : base()
        {
        }


        public WrappedFeedbackMessage(byte[] serializedMessage)
        {
            Deserialize(serializedMessage);
        }


        public WrappedFeedbackMessage(byte[] serializedMessage, ref int currentIndex)
        {
            Deserialize(serializedMessage, ref currentIndex);
        }


        public override void Deserialize(byte[] serializedMessage, ref int currentIndex)
        {
            Header = new Messages.std_msgs.Header(serializedMessage, ref currentIndex);
            GoalStatus = new Messages.actionlib_msgs.GoalStatus(serializedMessage, ref currentIndex);
            Content = (T)Activator.CreateInstance(typeof(T), serializedMessage, currentIndex);
        }


        public override byte[] Serialize(bool partofsomethingelse)
        {
            List<byte[]> pieces = new List<byte[]>();

            if (Header == null)
                Header = new Messages.std_msgs.Header();
            pieces.Add(Header.Serialize(true));

            if (GoalStatus == null)
                GoalStatus = new Messages.actionlib_msgs.GoalStatus();
            pieces.Add(GoalStatus.Serialize(true));

            if (Content == null)
                Content = new T();
            pieces.Add(Content.Serialize(true));

            // combine every array in pieces into one array and return it
            int __a_b__f = pieces.Sum((__a_b__c) => __a_b__c.Length);
            int __a_b__e = 0;
            byte[] __a_b__d = new byte[__a_b__f];
            foreach (var __p__ in pieces)
            {
                Array.Copy(__p__, 0, __a_b__d, __a_b__e, __p__.Length);
                __a_b__e += __p__.Length;
            }
            return __a_b__d;
        }


        public bool Equals(WrappedFeedbackMessage<T> message)
        {
            if (message == null)
            {
                return false;
            }

            bool result = true;
            result &= Header.Equals(message.Header);
            result &= GoalStatus.Equals(message.GoalStatus);
            result &= Content.Equals(message.Content);

            return result;
        }


        protected string CalcMd5(string hashText)
        {
            string md5sum;
            using (var md5 = MD5.Create())
            {
                var md5Hash = md5.ComputeHash(Encoding.ASCII.GetBytes(hashText));
                StringBuilder hashBuilder = new StringBuilder();
                for (int i = 0; i < md5Hash.Length; i++)
                {
                    hashBuilder.Append(md5Hash[i].ToString("x2"));
                }
                md5sum = hashBuilder.ToString();
            }

            return md5sum;
        }
    }


    public class GoalActionMessage<TGoal> : RosMessage where TGoal : InnerActionMessage, new()
    {
        public Messages.std_msgs.Header Header { get; set; }
        public Messages.actionlib_msgs.GoalID GoalId { get; set; }
        public TGoal Goal { get; set; }


        public GoalActionMessage() : base()
        {
        }


        public GoalActionMessage(byte[] serializedMessage)
        {
            Deserialize(serializedMessage);
        }


        public GoalActionMessage(byte[] serializedMessage, ref int currentIndex)
        {
            Deserialize(serializedMessage, ref currentIndex);
        }


        public override void Deserialize(byte[] serializedMessage, ref int currentIndex)
        {
            Header = new Messages.std_msgs.Header(serializedMessage, ref currentIndex);
            GoalId = new Messages.actionlib_msgs.GoalID(serializedMessage, ref currentIndex);
            Goal = (TGoal)Activator.CreateInstance(typeof(TGoal), serializedMessage, currentIndex);
        }


        public override string MessageDefinition()
        {
            string definition = "Header header\nactionlib_msgs/GoalID goal_id\n";
            definition += typeof(TGoal).ToString().Replace("Messages.", "").Replace(".", "/");
            definition += " goal";

            return definition;
        }


        public override string MD5Sum()
        {
            var messageDefinition = new List<string>();
            messageDefinition.Add((new Messages.std_msgs.Header()).MD5Sum() + " header");
            messageDefinition.Add((new Messages.actionlib_msgs.GoalID()).MD5Sum() + " goal_id");
            messageDefinition.Add((new TGoal()).MD5Sum() + " goal");

            var hashText = string.Join("\n", messageDefinition);
            var md5sum = CalcMd5(hashText);
            return md5sum;
        }


        public override byte[] Serialize(bool partofsomethingelse)
        {
            List<byte[]> pieces = new List<byte[]>();

            if (Header == null)
                Header = new Messages.std_msgs.Header();
            pieces.Add(Header.Serialize(true));

            if (GoalId == null)
                GoalId = new Messages.actionlib_msgs.GoalID();
            pieces.Add(GoalId.Serialize(true));

            if (Goal == null)
                Goal = new TGoal();
            pieces.Add(Goal.Serialize(true));

            // combine every array in pieces into one array and return it
            int __a_b__f = pieces.Sum((__a_b__c) => __a_b__c.Length);
            int __a_b__e = 0;
            byte[] __a_b__d = new byte[__a_b__f];
            foreach (var __p__ in pieces)
            {
                Array.Copy(__p__, 0, __a_b__d, __a_b__e, __p__.Length);
                __a_b__e += __p__.Length;
            }
            return __a_b__d;
        }


        public bool Equals(GoalActionMessage<TGoal> message)
        {
            if (message == null)
            {
                return false;
            }

            bool result = true;
            result &= Header.Equals(message.Header);
            result &= GoalId.Equals(message.GoalId);
            result &= Goal.Equals(message.Goal);

            return result;
        }


        private string CalcMd5(string hashText)
        {
            string md5sum;
            using (var md5 = MD5.Create())
            {
                var md5Hash = md5.ComputeHash(Encoding.ASCII.GetBytes(hashText));
                StringBuilder hashBuilder = new StringBuilder();
                for (int i = 0; i < md5Hash.Length; i++)
                {
                    hashBuilder.Append(md5Hash[i].ToString("x2"));
                }
                md5sum = hashBuilder.ToString();
            }

            return md5sum;
        }
    }


    public class ResultActionMessage<TResult> : WrappedFeedbackMessage<TResult> where TResult : InnerActionMessage, new()
    {
        public TResult Result { get { return Content; } set { Content = value; } }


        public ResultActionMessage() : base()
        {
        }


        public ResultActionMessage(byte[] serializedMessage) : base(serializedMessage)
        {
        }


        public ResultActionMessage(byte[] serializedMessage, ref int currentIndex) : base(serializedMessage, ref currentIndex)
        {
        }


        public bool Equals(ResultActionMessage<TResult> message)
        {
            return base.Equals(message);
        }


        public override string MD5Sum()
        {
            var messageDefinition = new List<string>();
            messageDefinition.Add((new Messages.std_msgs.Header()).MD5Sum() + " header");
            messageDefinition.Add((new Messages.actionlib_msgs.GoalStatus()).MD5Sum() + " status");
            messageDefinition.Add((new TResult()).MD5Sum() + " result");

            var hashText = string.Join("\n", messageDefinition);
            Console.WriteLine(hashText);
            var md5sum = CalcMd5(hashText);
            return md5sum;
        }
    }


    public class FeedbackActionMessage<TFeedback> : WrappedFeedbackMessage<TFeedback> where TFeedback : InnerActionMessage, new()
    {
        public TFeedback Feedback { get { return base.Content; } set { base.Content = value; } }


        public FeedbackActionMessage() : base()
        {
        }


        public FeedbackActionMessage(byte[] serializedMessage) : base(serializedMessage)
        {
        }


        public FeedbackActionMessage(byte[] serializedMessage, ref int currentIndex) : base(serializedMessage, ref currentIndex)
        {
        }


        public bool Equals(FeedbackActionMessage<TFeedback> message)
        {
            return base.Equals(message);
        }


        public override string MD5Sum()
        {
            var messageDefinition = new List<string>();
            messageDefinition.Add((new Messages.std_msgs.Header()).MD5Sum() + " header");
            messageDefinition.Add((new Messages.actionlib_msgs.GoalStatus()).MD5Sum() + " status");
            messageDefinition.Add((new TFeedback()).MD5Sum() + " feedback");

            var hashText = string.Join("\n", messageDefinition);
            Console.WriteLine(hashText);
            var md5sum = CalcMd5(hashText);
            return md5sum;
        }
    }



}
