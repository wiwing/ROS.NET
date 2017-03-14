namespace Uml.Robotics.Ros
{
    public class TopicInfo
    {
        public TopicInfo(string name, string data_type)
        {
            this.name = name;
            this.data_type = data_type;
        }

        public string data_type { get; set; }
        public string name { get; set; }
    }
}
