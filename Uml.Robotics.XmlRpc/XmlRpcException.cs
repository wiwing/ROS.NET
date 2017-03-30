using System;


namespace Uml.Robotics.XmlRpc
{
    public class XmlRpcException : Exception
    {
        private int errorCode = -1;

        public XmlRpcException(string msg, int errorCode = -1)
            : base(msg)
        {
            this.errorCode = errorCode;
        }

        public int ErrorCode
        {
            get { return errorCode; }
        }
    }
}
