using System;
using System.Collections.Generic;
using System.Reflection;
using Microsoft.Extensions.DependencyModel;
using System.Linq;
using System.Runtime.Loader;
using Microsoft.Extensions.Logging;

namespace Uml.Robotics.Ros
{
    public class MessageTypeRegistry
    {
        public static MessageTypeRegistry Default {
            get
            {
                if (instance == null)
                {
                    instance = new MessageTypeRegistry();
                }
                return instance;
            }
        }

        public Dictionary<string, Type> TypeRegistry { get; } = new Dictionary<string, Type>();
        public List<string> PackageNames { get; } = new List<string>();

        private static MessageTypeRegistry instance;
        private ILogger Logger { get; set; } = ApplicationLogging.CreateLogger<MessageTypeRegistry>();


        public RosMessage CreateMessage(string typeName)
        {
            RosMessage result = null;
            Type type = null;
            bool typeExist = TypeRegistry.TryGetValue(typeName, out type);
            if (typeExist)
            {
                result = Activator.CreateInstance(type) as RosMessage;
            }

            return result;
        }


        public IEnumerable<string> GetTypeNames()
        {
            return TypeRegistry.Keys;
        }


        public IEnumerable<Assembly> GetCandidateAssemblies(params string[] tagAssemblies)
        {
            if (tagAssemblies == null)
                throw new ArgumentNullException(nameof(tagAssemblies));
            if (tagAssemblies.Length == 0)
                throw new ArgumentException("At least one tag assembly name must be specified.", nameof(tagAssemblies));

            var context = DependencyContext.Load(Assembly.GetEntryAssembly());
            var loadContext = AssemblyLoadContext.Default;

            var referenceAssemblies = new HashSet<string>(tagAssemblies, StringComparer.OrdinalIgnoreCase);
            return context.RuntimeLibraries
                .Where(x => x.Dependencies.Any(d => referenceAssemblies.Contains(d.Name)))
                .SelectMany(x => x.GetDefaultAssemblyNames(context))
                .Select(x => loadContext.LoadFromAssemblyName(x));
        }


        public void ParseAssemblyAndRegisterRosMessages(Assembly assembly)
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

                var packageName = message.MessageType.Split('/')[0];
                if (!PackageNames.Contains(packageName))
                {
                    PackageNames.Add(packageName);
                }

                Logger.LogDebug($"Register {message.MessageType}");
                if (!TypeRegistry.ContainsKey(message.MessageType))
                {
                    TypeRegistry.Add(message.MessageType, message.GetType());
                }
            }
        }
    }
}
