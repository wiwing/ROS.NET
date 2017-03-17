using System;
using System.Collections.Generic;

namespace Uml.Robotics.Ros
{
    public static class network
    {
        public static string host;
        public static int tcpros_server_port;

        public static bool splitURI(string uri, ref string host, ref int port)
        {
            if (String.IsNullOrEmpty(uri))
                return false;//throw new ArgumentNullException(nameof(uri));
            if (uri.Substring(0, 7) == "http://")
                host = uri.Substring(7);
            else if (uri.Substring(0, 9) == "rosrpc://")
                host = uri.Substring(9);
            string[] split = host.Split(':');
            if (split.Length < 2) return false;
            string port_str = split[1];
            port_str = port_str.Trim('/');
            port = int.Parse(port_str);
            host = split[0];
            return true;
        }

        public static bool isPrivateIp(string ip)
        {
            return String.CompareOrdinal("192.168", ip) >= 7
                || String.CompareOrdinal("10.", ip) > 3
                || String.CompareOrdinal("169.253", ip) > 7;
        }

        public static string determineHost()
        {
            return Environment.MachineName;
        }

        public static void init(IDictionary<string, string> remappings)
        {
            if (remappings.ContainsKey("__hostname"))
            {
                host = remappings["__hostname"];
            }
            else
            {
                if (remappings.ContainsKey("__ip"))
                    host = remappings["__ip"];
            }

            if (remappings.ContainsKey("__tcpros_server_port"))
            {
                tcpros_server_port = int.Parse(remappings["__tcpros_server_port"]);
            }

            if (string.IsNullOrEmpty(host))
                host = determineHost();
        }
    }
}
