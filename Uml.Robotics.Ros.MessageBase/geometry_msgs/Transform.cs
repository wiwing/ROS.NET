using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using Uml.Robotics.Ros;


namespace Messages.geometry_msgs
{
    public class Transform : RosMessage
    {

			public Messages.geometry_msgs.Vector3 translation = new Messages.geometry_msgs.Vector3();
			public Messages.geometry_msgs.Quaternion rotation = new Messages.geometry_msgs.Quaternion();


        public override string MD5Sum() { return "ac9eff44abf714214112b05d54a3cf9b"; }
        public override bool HasHeader() { return false; }
        public override bool IsMetaType() { return true; }
        public override string MessageDefinition() { return @"Vector3 translation
Quaternion rotation"; }
        public override string MessageType { get { return "geometry_msgs/Transform"; } }
        public override bool IsServiceComponent() { return false; }

        public Transform()
        {

        }

        public Transform(byte[] SERIALIZEDSTUFF)
        {
            Deserialize(SERIALIZEDSTUFF);
        }

        public Transform(byte[] SERIALIZEDSTUFF, ref int currentIndex)
        {
            Deserialize(SERIALIZEDSTUFF, ref currentIndex);
        }



        public override void Deserialize(byte[] SERIALIZEDSTUFF, ref int currentIndex)
        {
            int arraylength = -1;
            bool hasmetacomponents = false;
            object __thing;
            int piecesize = 0;
            byte[] thischunk, scratch1, scratch2;
            IntPtr h;

            //translation
            translation = new Messages.geometry_msgs.Vector3(SERIALIZEDSTUFF, ref currentIndex);
            //rotation
            rotation = new Messages.geometry_msgs.Quaternion(SERIALIZEDSTUFF, ref currentIndex);
        }

        public override byte[] Serialize(bool partofsomethingelse)
        {
            int currentIndex=0, length=0;
            bool hasmetacomponents = false;
            byte[] thischunk, scratch1, scratch2;
            List<byte[]> pieces = new List<byte[]>();
            GCHandle h;

            //translation
            if (translation == null)
                translation = new Messages.geometry_msgs.Vector3();
            pieces.Add(translation.Serialize(true));
            //rotation
            if (rotation == null)
                rotation = new Messages.geometry_msgs.Quaternion();
            pieces.Add(rotation.Serialize(true));
            // combine every array in pieces into one array and return it
            int __a_b__f = pieces.Sum((__a_b__c)=>__a_b__c.Length);
            int __a_b__e=0;
            byte[] __a_b__d = new byte[__a_b__f];
            foreach(var __p__ in pieces)
            {
                Array.Copy(__p__,0,__a_b__d,__a_b__e,__p__.Length);
                __a_b__e += __p__.Length;
            }
            return __a_b__d;
        }

        public override void Randomize()
        {
            int arraylength = -1;
            Random rand = new Random();
            int strlength;
            byte[] strbuf, myByte;

            //translation
            translation = new Messages.geometry_msgs.Vector3();
            translation.Randomize();
            //rotation
            rotation = new Messages.geometry_msgs.Quaternion();
            rotation.Randomize();
        }

        public override bool Equals(RosMessage ____other)
        {
            if (____other == null)
				return false;
            bool ret = true;
            geometry_msgs.Transform other = (Messages.geometry_msgs.Transform)____other;

            ret &= translation.Equals(other.translation);
            ret &= rotation.Equals(other.rotation);
            // for each SingleType st:
            //    ret &= {st.Name} == other.{st.Name};
            return ret;
        }
    }
}
