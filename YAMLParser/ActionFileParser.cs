using FauxMessages;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Text;

namespace YAMLParser
{
    public class ActionFileParser
    {
        private ILogger Logger { get; } = ApplicationLogging.CreateLogger<ActionFileParser>();
        List<MsgFileLocation> actionFileLocations;


        public ActionFileParser(List<MsgFileLocation> actionFileLocations)
        {
            this.actionFileLocations = actionFileLocations;
        }


        public void GenerateRosMessageClasses()
        {
            var actionMessages = GenerateMessageFiles();
        }


        private List<ActionFile> GenerateMessageFiles()
        {
            var result = new List<ActionFile>();

            // Generate message files
            foreach(var fileLocation in actionFileLocations)
            {
                result.Add(new ActionFile(fileLocation));
            }

            // Resolve type dependencies between message files
            foreach(var messageFile in result)
            {
                Logger.LogInformation($"Parse message file {messageFile.Name}");
                messageFile.ParseAndResolveTypes();
            }

            return result;
        }
    }
}
