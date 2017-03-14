// File: Program.cs
// Project: YAMLParser
// 
// ROS.NET
// Eric McCann <emccann@cs.uml.edu>
// UMass Lowell Robotics Laboratory
// 
// Reimplementation of the ROS (ros.org) ros_cpp client in C#.
// 
// Created: 04/28/2015
// Updated: 10/07/2015

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using FauxMessages;


namespace YAMLParser
{
    internal class Program
    {
        public static List<MsgsFile> msgsFiles = new List<MsgsFile>();
        public static List<SrvsFile> srvFiles = new List<SrvsFile>();
        public static string backhalf;
        public static string fronthalf;
        public static string name = "Messages";
        public static string outputdir = "Messages";
        private static string configuration = "Debug"; //Debug, Release, etc.

        private static void Main(string[] args)
        {
            string solutiondir;
            bool interactive = false; //wait for ENTER press when complete
            int firstarg = 0;
            if (args.Length >= 1)
            {
                if (args[firstarg].Trim().Equals("-i"))
                {
                    interactive = true;
                    firstarg++;
                }
                if (firstarg < args.Length - 1)
                {
                    configuration = args[firstarg++];
                }
                if (firstarg < args.Length-1 && args[firstarg].Trim().Equals("-i"))
                {
                    interactive = true;
                    firstarg++;
                }
            }

            if (args.Length - firstarg >= 1)
            {
                solutiondir = new DirectoryInfo(Path.GetFullPath(args[firstarg])).FullName;
            }
            else
            {
                string yamlparser_parent = "";
                DirectoryInfo di = new DirectoryInfo(Directory.GetCurrentDirectory());
                while (di != null && di.Name != "YAMLParser")
                {
                    di = Directory.GetParent(di.FullName);
                }
                if (di == null)
                    throw new Exception("Not started from within YAMLParser directory.");
                di = Directory.GetParent(di.FullName);
                yamlparser_parent = di.FullName;
                solutiondir = yamlparser_parent;
            }

            Templates.LoadTemplateStrings(Path.Combine(solutiondir, "YAMLParser", "TemplateProject"));

            outputdir = Path.Combine(solutiondir, outputdir);
            var paths = new List<MsgFileLocation>();
            var pathssrv = new List<MsgFileLocation>();
            Console.WriteLine("Generatinc C# classes for ROS Messages...\n");
            for (int i = firstarg; i < args.Length; i++)
            {
                string d = new DirectoryInfo(Path.GetFullPath(args[i])).FullName;
                Console.WriteLine("Looking in " + d);
                MsgFileLocator.findMessages(paths, pathssrv, d);
            }

            // first pass: create all msg files (and register them in static resolver dictionary)
            foreach (MsgFileLocation path   in paths)
            {
                msgsFiles.Add(new MsgsFile(path));
            }
            foreach (MsgFileLocation path in pathssrv)
            {
                srvFiles.Add(new SrvsFile(path));
            }

            // secend pass: parse and resolve types
            foreach (var msg in msgsFiles)
            {
                msg.ParseAndResolveTypes();
            }
            foreach (var srv in srvFiles)
            {
                srv.ParseAndResolveTypes();
            }

            if (paths.Count + pathssrv.Count > 0)
            {
                MakeTempDir();
                GenerateFiles(msgsFiles, srvFiles);
                GenerateProject(msgsFiles, srvFiles);
                BuildProject();
            }
            else
            {
                Console.WriteLine("Usage:         YAMLParser.exe <SolutionFolder> [... other directories to search]\n      The Messages dll will be output to <SolutionFolder>/Messages/Messages.dll");
                if (interactive)
                    Console.ReadLine();
                Environment.Exit(1);
            }
            if (interactive)
            {
                Console.WriteLine("Finished. Press enter.");
                Console.ReadLine();
            }
        }

        public static void MakeTempDir()
        {
            if (!Directory.Exists(outputdir))
                Directory.CreateDirectory(outputdir);
            else
            {
                foreach (string s in Directory.GetFiles(outputdir, "*.cs"))
                {
                    try
                    {
                        File.Delete(s);
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine(e);
                    }
                }
                foreach (string s in Directory.GetDirectories(outputdir))
                {
                    if (s != "Properties")
                    {
                        try
                        {
                            Directory.Delete(s, true);
                        }
                        catch (Exception e)
                        {
                            Console.WriteLine(e);
                        }
                    }
                }
            }
            if (!Directory.Exists(outputdir))
                Directory.CreateDirectory(outputdir);
            else
            {
                foreach (string s in Directory.GetFiles(outputdir, "*.cs"))
                {
                    try
                    {
                        File.Delete(s);
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine(e);
                    }
                }
                foreach (string s in Directory.GetDirectories(outputdir))
                {
                    if (s != "Properties")
                    {
                        try
                        {
                            Directory.Delete(s, true);
                        }
                        catch (Exception e)
                        {
                            Console.WriteLine(e);
                        }
                    }
                }
            }
        }

        public static void GenerateFiles(List<MsgsFile> files, List<SrvsFile> srvfiles)
        {
            List<MsgsFile> mresolved = new List<MsgsFile>();
            List<SrvsFile> sresolved = new List<SrvsFile>();
            while (files.Except(mresolved).Any())
            {
                Debug.WriteLine("MSG: Running for " + files.Count + "/" + mresolved.Count + "\n" + files.Except(mresolved).Aggregate("\t", (o, n) => "" + o + "\n\t" + n.Name));
                foreach (MsgsFile m in files.Except(mresolved))
                {
                    string md5 = null;
                    string typename = null;
                    md5 = MD5.Sum(m);
                    typename = m.Name;
                    if (md5 != null && !md5.StartsWith("$") && !md5.EndsWith("MYMD5SUM"))
                    {
                        mresolved.Add(m);
                    }
                    else
                    {
                        Debug.WriteLine("Waiting for children of " + typename + " to have sums");
                    }
                }
                if (files.Except(mresolved).Any())
                {
                    Debug.WriteLine("MSG: Rerunning sums for remaining " + files.Except(mresolved).Count() + " definitions");
                }
            }
            while (srvfiles.Except(sresolved).Any())
            {
                Debug.WriteLine("SRV: Running for " + srvfiles.Count + "/" + sresolved.Count + "\n" + srvfiles.Except(sresolved).Aggregate("\t", (o, n) => "" + o + "\n\t" + n.Name));
                foreach (SrvsFile s in srvfiles.Except(sresolved))
                {
                    string md5 = null;
                    string typename = null;
                    s.Request.Stuff.ForEach(a => s.Request.resolve(a));
                    s.Response.Stuff.ForEach(a => s.Request.resolve(a));
                    md5 = MD5.Sum(s);
                    typename = s.Name;
                    if (md5 != null && !md5.StartsWith("$") && !md5.EndsWith("MYMD5SUM"))
                    {
                        sresolved.Add(s);
                    }
                    else
                    {
                        Debug.WriteLine("Waiting for children of " + typename + " to have sums");
                    }
                }
                if (srvfiles.Except(sresolved).Any())
                {
                    Debug.WriteLine("SRV: Rerunning sums for remaining " + srvfiles.Except(sresolved).Count() + " definitions");
                }
            }
            foreach (MsgsFile file in files)
            {
                file.Write(outputdir);
            }
            foreach (SrvsFile file in srvfiles)
            {
                file.Write(outputdir);
            }
            File.WriteAllText(Path.Combine(outputdir, "MessageTypes.cs"), ToString().Replace("FauxMessages", "Messages"));
        }

        public static void GenerateProject(List<MsgsFile> files, List<SrvsFile> srvfiles)
        {
            File.WriteAllText(Path.Combine(outputdir, "Interfaces.cs"), Templates.Interfaces);
            string[] lines = Templates.MessagesProj.Split('\n');
            string output = "";
            for (int i = 0; i < lines.Length; i++)
            {
                output += "" + lines[i] + "\n";
                /*if (lines[i].Contains("<Compile Include="))
                {
                    foreach (MsgsFile m in files)
                    {
                        output += "\t<Compile Include=\"" + m.Name.Replace('.', '\\') + ".cs\" />\n";
                    }
                    foreach (SrvsFile m in srvfiles)
                    {
                        output += "\t<Compile Include=\"" + m.Name.Replace('.', '\\') + ".cs\" />\n";
                    }
                    output += "\t<Compile Include=\"SerializationHelper.cs\" />\n";
                    output += "\t<Compile Include=\"Interfaces.cs\" />\n";
                    output += "\t<Compile Include=\"MessageTypes.cs\" />\n";
                }*/
            }
            File.WriteAllText(Path.Combine(outputdir, name + ".csproj"), output);
            File.WriteAllText(Path.Combine(outputdir, ".gitignore"), "*");
        }

        public static void BuildProject()
        {
            BuildProject("BUILDING GENERATED PROJECT WITH MSBUILD!");
        }

        static Process RunDotNet(string args)
        {
            string fn = "dotnet";
            var proc = new Process();
            proc.StartInfo.RedirectStandardOutput = true;
            proc.StartInfo.RedirectStandardError = true;
            proc.StartInfo.UseShellExecute = false;
            proc.StartInfo.CreateNoWindow = true;
            proc.StartInfo.FileName = fn;
            proc.StartInfo.Arguments = args;
            proc.Start();
            return proc;
        }

        public static void BuildProject(string spam)
        {
            Console.WriteLine("\n\n" + spam);

            string output, error;

            Console.WriteLine("Running .NET dependency restorer...");
            string restoreArgs = "restore \"" + Path.Combine(outputdir, name) + ".csproj\"";
            var proc = RunDotNet(restoreArgs);
            output = proc.StandardOutput.ReadToEnd();
            error = proc.StandardError.ReadToEnd();
            if (output.Length > 0)
                Console.WriteLine(output);
            if (error.Length > 0)
                Console.WriteLine(error);

            Console.WriteLine("Running .NET Builder...");
            string buildArgs = "build \"" + Path.Combine(outputdir, name) + ".csproj\" -c " + configuration;
            proc = RunDotNet(buildArgs);

            output = proc.StandardOutput.ReadToEnd();
            error = proc.StandardError.ReadToEnd();
            if (File.Exists(Path.Combine(outputdir, "bin", configuration, name + ".dll")))
            {
                Console.WriteLine("\n\nGenerated DLL has been copied to:\n\t" + Path.Combine(outputdir, name + ".dll") + "\n\n");
                File.Copy(Path.Combine(outputdir,  "bin", configuration, name + ".dll"), Path.Combine(outputdir, name + ".dll"), true);
                Thread.Sleep(100);
            }
            else
            {
                if (output.Length > 0)
                    Console.WriteLine(output);
                if (error.Length > 0)
                    Console.WriteLine(error);
                Console.WriteLine("AMG BUILD FAIL!");
            }
        }

        private static string uberpwnage;

        public new static string ToString()
        {
            if (uberpwnage == null)
            {
                if (fronthalf == null)
                {
                    fronthalf = "using Messages;\n\nnamespace Messages\n{\n";
                    backhalf = "\n}";
                }

                List<MsgsFile> everything = new List<MsgsFile>(msgsFiles);
                foreach (SrvsFile sf in srvFiles)
                {
                    everything.Add(sf.Request);
                    everything.Add(sf.Response);
                }
                fronthalf += "\n\tpublic enum MsgTypes\n\t{";
                fronthalf += "\n\t\tUnknown,";
                string srvs = "\n\t\tUnknown,";
                for (int i = 0; i < everything.Count; i++)
                {
                    fronthalf += "\n\t\t";
                    if (everything[i].classname == "Request" || everything[i].classname == "Response")
                    {
                        if (everything[i].classname == "Request")
                        {
                            srvs += "\n\t\t" + everything[i].Name.Replace(".", "__") + ",";
                        }
                        everything[i].Name += "." + everything[i].classname;
                    }
                    fronthalf += everything[i].Name.Replace(".", "__");
                    if (i < everything.Count - 1)
                        fronthalf += ",";
                }
                fronthalf += "\n\t}\n";
                srvs = srvs.TrimEnd(',');
                fronthalf += "\n\tpublic enum SrvTypes\n\t{";
                fronthalf += srvs + "\n\t}\n";
                uberpwnage = fronthalf + backhalf;
            }
            return uberpwnage;
        }
    }
}