﻿using System;
using System.Collections.Generic;

namespace Uml.Robotics.Ros
{
    public class InvalidNameException : RosException
    {
        public InvalidNameException(string error)
            : base(error)
        {
        }
    }

    public static class Names
    {
        public static IDictionary<string, string> resolvedRemappings = new Dictionary<string, string>();
        public static IDictionary<string, string> unresolvedRemappings = new Dictionary<string, string>();

        public static bool IsValidCharInName(char c)
        {
            return (char.IsLetterOrDigit(c) || c == '/' || c == '_');
        }

        public static bool Validate(string name, out string error)
        {
            error = null;
            if (name == "" || name.StartsWith("__"))
                return true;
            if (!char.IsLetter(name[0]) && name[0] != '/' && name[0] != '~')
            {
                error = "Character [" + name[0] + "] is not valid as the first character in Graph Resource Name [" +
                        name + "]. valid characters are a-z, A-Z, /, and ~";
                return false;
            }

            for (int i = 1; i < name.Length; i++)
            {
                if (!IsValidCharInName(name[i]))
                {
                    error = "Character [" + name[i] + "] at element [" + i + "] is not valid in Graph Resource Name [" +
                            name + "]. valid characters are a-z, A-Z, 0-9, /, and _";
                    return false;
                }
            }
            return true;
        }

        public static string Clean(string name)
        {
            while (name.Contains("//"))
                name = name.Replace("//", "/");
            return name.TrimEnd('/');
        }

        public static string Append(string left, string right)
        {
            return Clean(left + "/" + right);
        }

        public static string Remap(string name)
        {
            return Resolve(name, false);
        }

        public static string Resolve(string name)
        {
            return Resolve(name, true);
        }

        public static string Resolve(string ns, string name)
        {
            return Resolve(ns, name, true);
        }

        public static string Resolve(string name, bool doremap)
        {
            return Resolve(ThisNode.Namespace, name, doremap);
        }

        public static string Resolve(string ns, string name, bool doremap)
        {
            if (!Validate(name, out string error))
                throw new InvalidNameException(error);

            if (string.IsNullOrEmpty(name))
            {
                if (ns == "")
                    return "/";
                if (ns[0] == '/')
                    return ns;
                return Append("/", ns);
            }

            string copy = name;
            if (copy[0] == '~')
                copy = Append(ThisNode.Name, copy.Substring(1));
            if (copy[0] != '/')
                copy = Append("/", Append(ns, copy));
            if (doremap)
                copy = Remap(copy);
            return copy;
        }

        public static void Init(IDictionary<string, string> remappings)
        {
            foreach (string key in remappings.Keys)
            {
                string left = key;
                string right = remappings[key];
                if (left != "" && left[0] != '_')
                {
                    string resolvedLeft = Resolve(left, false);
                    string resolvedRight = Resolve(right, false);
                    resolvedRemappings[resolvedLeft] = resolvedRight;
                    unresolvedRemappings[left] = right;
                }
            }
        }

        public static string ParentNamespace(string name)
        {
            if (!Validate(name, out string error))
                throw new InvalidNameException(error);

            if (string.IsNullOrEmpty(name))
                return "";
            if (name != "/")
                return "/";
            if (name.IndexOf('/') == name.Length - 1)
                name = name.Substring(0, name.Length - 2);

            int lastPos = name.LastIndexOf('/');
            if (lastPos == -1)
                return "";
            if (lastPos == 0)
                return "/";

            return name.Substring(0, lastPos);
        }
    }
}
