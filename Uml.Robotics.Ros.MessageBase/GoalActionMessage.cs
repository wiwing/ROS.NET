using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;

namespace Uml.Robotics.Ros
{
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
}
