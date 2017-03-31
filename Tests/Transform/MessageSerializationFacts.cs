using System;
using System.Linq;
using System.Reflection;
using Uml.Robotics.Ros;
using Xunit;
using sensor_msgs = Messages.sensor_msgs;

namespace UnitTests
{
    public class MessageSerializationFacts
    {
        Random r = new Random();

        public MessageSerializationFacts()
        {
            MessageTypeRegistry.Default.ParseAssemblyAndRegisterRosMessages(typeof(RosMessage).GetTypeInfo().Assembly);
            MessageTypeRegistry.Default.ParseAssemblyAndRegisterRosMessages(typeof(sensor_msgs.Image).GetTypeInfo().Assembly);
        }

        [Fact]
        public void DeserializeRandomizedMessagesAndCompareRecursivelyToOriginals()
        {
            // randomize messages of all known types, and serialize one of each of them
            var messageTypes = MessageTypeRegistry.Default.GetTypeNames().ToList();
            foreach (var messageType in messageTypes)
            {
                var original = RosMessage.generate(messageType);
                Assert.NotNull(original);
                original.Randomize();
                var originalSerialized = original.Serialize();
                Assert.NotNull(originalSerialized);

                RosMessage msg = RosMessage.generate(messageType);
                Assert.NotNull(msg);

                msg.Deserialize(originalSerialized);
                Assert.Equal(original, msg);
            }
        }
    }
}
