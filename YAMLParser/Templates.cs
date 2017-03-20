using System;
using System.IO;

namespace YAMLParser
{
    public static class Templates
    {
        public static void LoadTemplateStrings(string templateProjectPath)
        {
            Templates.Interfaces = File.ReadAllText(Path.Combine(templateProjectPath, "Interfaces.cs"));
            Templates.MessagesProj = File.ReadAllText(Path.Combine(templateProjectPath, "Messages._csproj"));
            Templates.MsgPlaceHolder = File.ReadAllText(Path.Combine(templateProjectPath, "PlaceHolder._cs"));
            Templates.SrvPlaceHolder = File.ReadAllText(Path.Combine(templateProjectPath, "SrvPlaceHolder._cs"));
            Templates.ActionPlaceHolder = File.ReadAllText(Path.Combine(templateProjectPath, "ActionPlaceHolder._cs"));
        }

        internal static string Interfaces { get; set; }
        internal static string MessagesProj { get; set; }
        internal static string MsgPlaceHolder { get; set; }
        internal static string SrvPlaceHolder { get; set; }
        internal static string ActionPlaceHolder { get; set; }
    }
}
