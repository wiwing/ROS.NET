using System;
using System.Collections.Generic;
using System.Text;

namespace Uml.Robotics.Ros
{
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
}
