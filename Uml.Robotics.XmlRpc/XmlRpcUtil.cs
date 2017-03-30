using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;

namespace Uml.Robotics.XmlRpc
{
    public static class XmlRpcUtil
    {
        public enum XMLRPC_LOG_LEVEL
        {
            CRITICAL = 0,
            ERROR = 1,
            WARNING = 2,
            INFO = 3,
            DEBUG = 4,
            MAX = 5
        }

        public static string XMLRPC_VERSION = "XMLRPC++ 0.7";
        private static XMLRPC_LOG_LEVEL MINIMUM_LOG_LEVEL = XMLRPC_LOG_LEVEL.ERROR;

        public static void SetLogLevel(XMLRPC_LOG_LEVEL level)
        {
            MINIMUM_LOG_LEVEL = level;
        }

        public static void SetLogLevel(int level)
        {
            SetLogLevel((XMLRPC_LOG_LEVEL) level);
        }

        public static void error(string format, params object[] list)
        {
            Debug.WriteLine(String.Format(format, list));
        }

        public static void log(int level, string format, params object[] list)
        {
            log((XMLRPC_LOG_LEVEL) level, format, list);
        }

        public static void log(XMLRPC_LOG_LEVEL level, string format, params object[] list)
        {
            if (level <= MINIMUM_LOG_LEVEL)
            {
                Debug.WriteLine(String.Format(format, list));
            }
        }
    }
}
