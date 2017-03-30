using System;
using System.Collections.Generic;
using System.Reflection;

namespace Uml.Robotics.Ros
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

                Console.WriteLine($"Register {message.MessageType} {message.GetType().ToString()}");
                if (!_typeregistry.ContainsKey(message.MessageType))
                {
                    _typeregistry.Add(message.MessageType, message.GetType());
                }
                if (!constructors.ContainsKey(message.MessageType))
                {
                    constructors.Add(message.MessageType, T => Activator.CreateInstance(_typeregistry[T]) as RosMessage);
                }
            }
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
