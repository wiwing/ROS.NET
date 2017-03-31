using System;
using System.Collections.Generic;
using System.Reflection;

namespace Uml.Robotics.Ros
{
    public delegate RosMessage RosServiceDelegate(RosMessage request);

    public class RosService
    {
        public virtual string MD5Sum() { return ""; }

        public virtual string ServiceDefinition() { return ""; }

        public virtual string ServiceType { get { return "xamla/unkown"; } }

        public string msgtype_req
        {
            get { return RequestMessage.MessageType; }
        }

        public string msgtype_res
        {
            get { return ResponseMessage.MessageType; }
        }

        public RosMessage RequestMessage, ResponseMessage;

        protected RosMessage GeneralInvoke(RosServiceDelegate invocation, RosMessage m)
        {
            return invocation.Invoke(m);
        }

        public RosService()
        {
        }

        protected void InitSubtypes(RosMessage request, RosMessage response)
        {
            RequestMessage = request;
            ResponseMessage = response;
        }

        internal static Dictionary<string, Func<string, RosService>> constructors = new Dictionary<string, Func<string, RosService>>();
        private static Dictionary<string, Type> _typeregistry = new Dictionary<string, Type>();

        public static RosService generate(string t)
        {
            lock (constructors)
            {
                if (constructors.ContainsKey(t))
                    return constructors[t].Invoke(t);
                Type thistype = typeof(RosService);
                foreach (Type othertype in thistype.GetTypeInfo().Assembly.GetTypes())
                {
                    if (thistype == othertype || !othertype.GetTypeInfo().IsSubclassOf(thistype))
                    {
                        continue;
                    }

                    RosService srv = Activator.CreateInstance(othertype) as RosService;
                    if (srv != null)
                    {
                        if (srv.ServiceType == "xamla/unkown")
                            throw new Exception("Invalid servive type. Service type field (srvtype) was not initialized correctly.");
                        if (!_typeregistry.ContainsKey(srv.ServiceType))
                            _typeregistry.Add(srv.ServiceType, srv.GetType());
                        if (!constructors.ContainsKey(srv.ServiceType))
                            constructors.Add(srv.ServiceType, T => Activator.CreateInstance(_typeregistry[T]) as RosService);
                        srv.RequestMessage = RosMessage.generate(srv.ServiceType + "__Request");
                        srv.ResponseMessage = RosMessage.generate(srv.ServiceType + "__Response");
                    }
                }

                return constructors[t].Invoke(t);
            }
        }
    }
}
