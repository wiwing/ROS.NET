namespace Uml.Robotics.XmlRpc
{
    public class XmlRpcServerMethod
    {
        private XMLRPCFunc _FUNC;

        public string name;
        public XmlRpcServer server;

        public XmlRpcServerMethod(string function_name, XMLRPCFunc func, XmlRpcServer server)
        {
            name = function_name;
            this.server = server;
            //SegFault();
            FUNC = func;
            if (server != null)
                server.AddMethod(this);
        }


        public XMLRPCFunc FUNC
        {
            get { return _FUNC; }
            set { SetFunc((_FUNC = value)); }
        }

        public void SetFunc(XMLRPCFunc func)
        {
            _FUNC = func;
        }

        public void Execute(XmlRpcValue parms, XmlRpcValue reseseses)
        {
            _FUNC(parms, reseseses);
        }

        public virtual string Help()
        {
            return "no help";
        }
    }

    //[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    public delegate void XMLRPCFunc(XmlRpcValue parms, XmlRpcValue reseseses);
}
