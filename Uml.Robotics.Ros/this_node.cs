using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace Uml.Robotics.Ros
{
    public static class this_node
    {
        public static string Name = "empty";
        public static string Namespace = "";

        public static void Init(string n, IDictionary<string, string> remappings)
        {
            Init(n, remappings, 0);
        }

        public static void Init(string n, IDictionary<string, string> remappings, int options)
        {
            Name = n;
            bool disable_anon = false;
            if (remappings.ContainsKey("__name"))
            {
                Name = remappings["__name"];
                disable_anon = true;
            }
            if (remappings.ContainsKey("__ns"))
            {
                Namespace = remappings["__ns"];
            }
            if (Namespace == "")
            {
                Namespace = "/";
            } 

            long walltime = DateTime.Now.Subtract(Process.GetCurrentProcess().StartTime).Ticks;
            names.Init(remappings);
            if (Name.Contains("/"))
                throw new ArgumentException("Slashes '/' are not allowed in names", nameof(n));
            if (Name.Contains("~"))
                throw new ArgumentException("Tildes '~' are not allowed in names", nameof(n));
            try
            {
                Name = names.resolve(Namespace, Name);
            }
            catch (Exception e)
            {
                EDB.WriteLine(e);
            }
            if ((options & (int) InitOption.AnonymousName) == (int) InitOption.AnonymousName && !disable_anon)
            {
                int lbefore = Name.Length;
                Name += "_" + walltime;
                if (Name.Length - lbefore > 201)
                    Name = Name.Remove(lbefore + 201);
            }
        }
    }
}
