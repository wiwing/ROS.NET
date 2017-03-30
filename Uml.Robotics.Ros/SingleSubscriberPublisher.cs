namespace Uml.Robotics.Ros
{
    public class SingleSubscriberPublisher
    {
        public SubscriberLink link;

        public SingleSubscriberPublisher(SubscriberLink link)
        {
            this.link = link;
        }

        public string topic
        {
            get { return link.topic; }
        }

        public string subscriber_name
        {
            get { return link.destination_caller_id; }
        }

        public void publish<M>(M message) where M : RosMessage, new()
        {
            link.enqueueMessage(new MessageAndSerializerFunc(message, message.Serialize, true, true));
        }
    }
}
