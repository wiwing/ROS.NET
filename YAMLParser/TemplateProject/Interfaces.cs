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
        internal static Dictionary<string, Func<string, RosMessage>> constructors = new Dictionary<string, Func<string, RosMessage>>();
        private static Dictionary<string, Type> _typeregistry = new Dictionary<string, Type>();

        public static RosMessage generate(string t)
        {
            lock (constructors)
            {
                if (constructors.ContainsKey(t))
                {
                    return constructors[t].Invoke(t);
                }
                else
                {
                    throw new ArgumentException($"Could not find a RosMessage for {t}", nameof(t));
                }
            }
        }


        public static void ParseAssemblyAndRegisterRosMessages(Assembly assembly)
        {
            foreach (Type othertype in assembly.GetTypes())
            {
                var messageInfo = othertype.GetTypeInfo();
                if (othertype == typeof(RosMessage) || !messageInfo.IsSubclassOf(typeof(RosMessage)) || othertype == typeof(InnerActionMessage))
                {
                    continue;
                }

                var goalAttribute = messageInfo.GetCustomAttribute<ActionGoalMessageAttribute>();
                var resultAttribute = messageInfo.GetCustomAttribute<ActionResultMessageAttribute>();
                var feedbackAttribute = messageInfo.GetCustomAttribute<ActionFeedbackMessageAttribute>();
                var ignoreAttribute = messageInfo.GetCustomAttribute<IgnoreRosMessageAttribute>();
                RosMessage message;
                if ((goalAttribute != null) || (resultAttribute != null) || (feedbackAttribute != null) || (ignoreAttribute != null))
                {
                    Type actionType;
                    if (goalAttribute != null)
                    {
                        actionType = typeof(GoalActionMessage<>);
                    }
                    else if (resultAttribute != null)
                    {
                        actionType = typeof(ResultActionMessage<>);
                    }
                    else if (feedbackAttribute != null)
                    {
                        actionType = typeof(FeedbackActionMessage<>);
                    }
                    else if (ignoreAttribute != null)
                    {
                        continue;
                    }
                    else
                    {
                        throw new InvalidOperationException($"Could create Action Message for {othertype}");
                    }
                    Type[] innerType = { othertype };
                    var goalMessageType = actionType.MakeGenericType(innerType);
                    message = (Activator.CreateInstance(goalMessageType)) as RosMessage;
                }
                else
                {
                    message = Activator.CreateInstance(othertype) as RosMessage;
                    if ((message != null) && (message.MessageType == "xamla/unkown"))
                    {
                        throw new Exception("Invalid message type. Message type field (msgtype) was not initialized correctly.");
                    }
                }

                if (!_typeregistry.ContainsKey(message.MessageType))
                {
                    _typeregistry.Add(message.MessageType, message.GetType());
                }
                if (!constructors.ContainsKey(message.MessageType))
                {
                    constructors.Add(message.MessageType, T => Activator.CreateInstance(_typeregistry[T]) as RosMessage);
                }
            }
            var test = 1;
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

    public delegate RosMessage RosServiceDelegate(RosMessage request);

    public class RosService
    {
        public virtual string MD5Sum() { return ""; }

        public virtual string ServiceDefinition() { return ""; }

        public virtual string ServiceType { get { return "xamla/unkown"; } }

        public string msgtype_req
        {
            get { return RequestMessage.MessageType; }
        }

        public string msgtype_res
        {
            get { return ResponseMessage.MessageType; }
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

        internal static Dictionary<string, Func<string, RosService>> constructors = new Dictionary<string, Func<string, RosService>>();
        private static Dictionary<string, Type> _typeregistry = new Dictionary<string, Type>();

        public static RosService generate(string t)
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
                        if (srv.ServiceType == "xamla/unkown")
                            throw new Exception("Invalid servive type. Service type field (srvtype) was not initialized correctly.");
                        if (!_typeregistry.ContainsKey(srv.ServiceType))
                            _typeregistry.Add(srv.ServiceType, srv.GetType());
                        if (!constructors.ContainsKey(srv.ServiceType))
                            constructors.Add(srv.ServiceType, T => Activator.CreateInstance(_typeregistry[T]) as RosService);
                        srv.RequestMessage = RosMessage.generate(srv.ServiceType + "__Request");
                        srv.ResponseMessage = RosMessage.generate(srv.ServiceType + "__Response");
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


    [IgnoreRosMessage]
    public class InnerActionMessage : RosMessage
    {

    }

    [IgnoreRosMessage]
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


    [IgnoreRosMessage]
    public class GoalActionMessage<TGoal> : RosMessage where TGoal : InnerActionMessage, new()
    {
        public Messages.std_msgs.Header Header { get; set; }
        public Messages.actionlib_msgs.GoalID GoalId { get; set; }
        public TGoal Goal { get; set; }
        public override string MessageType
        {
            get
            {
                var typeName = typeof(TGoal).ToString().Replace("Messages.", "").Replace(".", "/");
                var front = typeName.Substring(0, typeName.Length - 4);
                var back = typeName.Substring(typeName.Length - 4);
                typeName = front + "Action" + back;
                return typeName;
            }
        }


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
            var definition = $"Header header\nactionlib_msgs/GoalID goal_id\n{this.MessageType} goal";

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


    [IgnoreRosMessage]
    public class ResultActionMessage<TResult> : WrappedFeedbackMessage<TResult> where TResult : InnerActionMessage, new()
    {
        public TResult Result { get { return Content; } set { Content = value; } }
        public override string MessageType
        {
            get
            {
                var typeName = typeof(TResult).ToString().Replace("Messages.", "").Replace(".", "/");
                var front = typeName.Substring(0, typeName.Length - 6);
                var back = typeName.Substring(typeName.Length - 6);
                typeName = front + "Action" + back;
                return typeName;
            }
        }


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


        public override string MessageDefinition()
        {
            return $"Header header\nactionlib_msgs/GoalStatus status\n{this.MessageType} result";
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


    [IgnoreRosMessage]
    public class FeedbackActionMessage<TFeedback> : WrappedFeedbackMessage<TFeedback> where TFeedback : InnerActionMessage, new()
    {
        public TFeedback Feedback { get { return base.Content; } set { base.Content = value; } }
        public override string MessageType
        {
            get
            {
                var typeName = typeof(TFeedback).ToString().Replace("Messages.", "").Replace(".", "/");
                var front = typeName.Substring(0, typeName.Length - 8);
                var back = typeName.Substring(typeName.Length - 8);
                typeName = front + "Action" + back;
                return typeName;
            }
        }


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


        public override string MessageDefinition()
        {
            return $"Header header\nactionlib_msgs/GoalStatus status\n{this.MessageType} feedback";
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


    public class ActionGoalMessageAttribute : Attribute
    {

    }


    public class ActionResultMessageAttribute : Attribute
    {

    }


    public class ActionFeedbackMessageAttribute : Attribute
    {

    }


    public class IgnoreRosMessageAttribute : Attribute
    {

    }
}
