using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using YAMLParser;

namespace FauxMessages
{
    public class ActionFile
    {
        public string Name { get; }
        public MsgsFile GoalMessage { get { return goalMessage; } }
        public MsgsFile ResultMessage { get { return resultMessage; } }
        public MsgsFile FeedbackMessage { get { return feedbackMessage; } }

        private string generatedDictHelper;
        private bool hasHeader;
        private string fileNamespace = "Messages";
        private MsgsFile goalMessage;
        private MsgsFile resultMessage;
        private MsgsFile feedbackMessage;
        private List<SingleType> stuff = new List<SingleType>();
        private string backHalf;
        private string className;
        private string dimensions;
        private string frontHalf;
        private MsgFileLocation MsgFileLocation;
        private string messageTemplate;
        private bool meta;
        private List<string> linesOfActionFile = new List<string>();
        private string memoizedcontent;
        private string goalBackHalf;
        private string goalFrontHalf;
        private string resultBackHalf;
        private string feedbackBackHalf;


        public ActionFile(MsgFileLocation filename)
        {
            MsgFileLocation = filename;
            // Read in action file
            string[] lines = File.ReadAllLines(filename.Path);
            className = filename.basename;
            fileNamespace += "." + filename.package;
            Name = filename.package + "." + filename.basename;

            var parsedAction = ParseActionFile(lines);

            // Treat goal, result and feedback like three message files, each with a partial definition and additional information
            // tagged on to the classname
            goalMessage = new MsgsFile(new MsgFileLocation(filename.Path.Replace(".action", ".msg"), filename.searchroot),
                false, parsedAction.GoalParamters, "\t"
            );
            resultMessage = new MsgsFile(new MsgFileLocation(filename.Path.Replace(".action", ".msg"), filename.searchroot),
                false, parsedAction.ResultParameters, "\t"
            );
            feedbackMessage = new MsgsFile(new MsgFileLocation(filename.Path.Replace(".action", ".msg"), filename.searchroot),
                false, parsedAction.FeedbackParameters, "\t"
            );
        }


        public void ParseAndResolveTypes()
        {
            goalMessage.ParseAndResolveTypes();
            resultMessage.ParseAndResolveTypes();
            feedbackMessage.ParseAndResolveTypes();
        }


        public void Write(string outdir)
        {
            string[] chunks = Name.Split('.');
            for (int i = 0; i < chunks.Length - 1; i++)
                outdir = Path.Combine(outdir, chunks[i]);
            if (!Directory.Exists(outdir))
                Directory.CreateDirectory(outdir);
            string contents = GetString();
            if (contents != null)
                File.WriteAllText(Path.Combine(outdir, MsgFileLocation.basename + ".cs"),
                    contents.Replace("FauxMessages", "Messages")
                );
        }


        public string GetString()
        {
            if (goalFrontHalf == null)
            {
                goalFrontHalf = "";
                goalBackHalf = "";
                string[] lines = Templates.ActionPlaceHolder.Split('\n');
                int section = 0;
                for (int i = 0; i < lines.Length; i++)
                {
                    //read until you find public class request... do everything once.
                    //then, do it again response
                    if (lines[i].Contains("$$ENTRY_POINT_GOAL"))
                    {
                        section++;
                        continue;
                    }
                    if (lines[i].Contains("namespace"))
                    {
                        goalFrontHalf +=
                          "\nusing Messages.std_msgs;\nusing String=System.String;\nusing Messages.geometry_msgs;\n\n"; //\nusing Messages.roscsharp;
                        goalFrontHalf += "namespace " + fileNamespace + "\n";
                        continue;
                    }
                    if (lines[i].Contains("$$ENTRY_POINT_RESULT"))
                    {
                        section++;
                        continue;
                    }
                    if (lines[i].Contains("$$ENTRY_POINT_FEEDBACK"))
                    {
                        section++;
                        continue;
                    }
                    switch (section)
                    {
                        case 0:
                            goalFrontHalf += lines[i] + "\n";
                            break;
                        case 1:
                            goalBackHalf += lines[i] + "\n";
                            break;
                        case 2:
                            resultBackHalf += lines[i] + "\n";
                            break;
                        case 3:
                            feedbackBackHalf += lines[i] + "\n";
                            break;
                    }
                }
            }

            messageTemplate = goalFrontHalf + goalMessage.GetSrvHalf() + goalBackHalf +
                resultMessage.GetSrvHalf() + resultBackHalf +
                feedbackMessage.GetSrvHalf() + feedbackBackHalf;

            /***********************************/
            /*       CODE BLOCK DUMP           */
            /***********************************/

            #region definitions

            for (int i = 0; i < linesOfActionFile.Count; i++)
            {
                while (linesOfActionFile[i].Contains("\t"))
                    linesOfActionFile[i] = linesOfActionFile[i].Replace("\t", " ");
                while (linesOfActionFile[i].Contains("\n\n"))
                    linesOfActionFile[i] = linesOfActionFile[i].Replace("\n\n", "\n");
                linesOfActionFile[i] = linesOfActionFile[i].Replace('\t', ' ');
                while (linesOfActionFile[i].Contains("  "))
                    linesOfActionFile[i] = linesOfActionFile[i].Replace("  ", " ");
                linesOfActionFile[i] = linesOfActionFile[i].Replace(" = ", "=");
                linesOfActionFile[i] = linesOfActionFile[i].Replace("\"", "\"\"");
            }

            StringBuilder actionDefinition = new StringBuilder();
            StringBuilder goalDefinition = new StringBuilder();
            StringBuilder resultDefinition = new StringBuilder();
            StringBuilder feedbackDefinition = new StringBuilder();
            int foundDelimeter = 0;
            foreach (string s in linesOfActionFile)
            {
                if (s == "---")
                {
                    //only put this string in md, because the subclass defs don't contain it
                    actionDefinition.AppendLine(s);

                    //we've hit the middle... move from the request to the response by making responsedefinition not null.
                    foundDelimeter += 1;
                    continue;
                }

                //add every line to MessageDefinition for whole service
                actionDefinition.AppendLine(s);

                //before we hit ---, add lines to request Definition. Otherwise, add them to response.
                if (foundDelimeter == 0)
                {
                    goalDefinition.AppendLine(s);
                }
                else if (foundDelimeter == 1)
                {
                    resultDefinition.AppendLine(s);
                } else if (foundDelimeter == 2)
                {
                    feedbackDefinition.AppendLine(s);
                }

            }

            string actionDefinitionString = actionDefinition.ToString().Trim();
            string goalDefinitionString = goalDefinition.ToString().Trim();
            string resultDefinitionString = resultDefinition.ToString().Trim();
            string feedbackDefinitionString = feedbackDefinition.ToString().Trim();

            #endregion

            #region THE SERVICE

            //messageTemplate = messageTemplate.Replace("$WHATAMI", className);
            //messageTemplate = messageTemplate.Replace("$MYSRVTYPE", "SrvTypes." + fileNamespace.Replace("Messages.", "") + "__" + className);
            //messageTemplate = messageTemplate.Replace("$MYSERVICEDEFINITION", "@\"" + actionDefinitionString + "\"");

            #endregion

            #region REPLACE GOAL PLACEHOLDERS

            string goalDict = goalMessage.GenFields();
            meta = goalMessage.meta;
            string goalClassName = className + "Goal";
            messageTemplate = messageTemplate.Replace("$GOAL_CLASS", goalClassName);
            messageTemplate = messageTemplate.Replace("$GOAL_ISMETA", meta.ToString().ToLower());
            messageTemplate = messageTemplate.Replace("$GOAL_MSGTYPE", "MsgTypes." + fileNamespace.Replace("Messages.", "") + "__" + goalClassName);
            messageTemplate = messageTemplate.Replace("$GOAL_MESSAGEDEFINITION", "@\"" + goalDefinitionString + "\"");
            messageTemplate = messageTemplate.Replace("$GOAL_HASHEADER", goalMessage.HasHeader.ToString().ToLower());
            messageTemplate = messageTemplate.Replace("$GOAL_FIELDS", goalDict.Length > 5 ? "{{" + goalDict + "}}" : "()");
            messageTemplate = messageTemplate.Replace("$GOAL_NULLCONSTBODY", "");
            messageTemplate = messageTemplate.Replace("$GOAL_EXTRACONSTRUCTOR", "");

            #endregion

            #region REPLACE RESULT PLACEHOLDERS

            string resultDict = resultMessage.GenFields();
            string resultClassName = className + "Result";
            messageTemplate = messageTemplate.Replace("$RESULT_CLASS", resultClassName);
            messageTemplate = messageTemplate.Replace("$RESULT_ISMETA", resultMessage.meta.ToString().ToLower());
            messageTemplate = messageTemplate.Replace("$RESULT_MSGTYPE", "MsgTypes." + fileNamespace.Replace("Messages.", "") + "__" + resultClassName);
            messageTemplate = messageTemplate.Replace("$RESULT_MESSAGEDEFINITION", "@\"" + resultDefinitionString + "\"");
            messageTemplate = messageTemplate.Replace("$RESULT_HASHEADER", resultMessage.HasHeader.ToString().ToLower());
            messageTemplate = messageTemplate.Replace("$RESULT_FIELDS", resultDict.Length > 5 ? "{{" + resultDict + "}}" : "()");
            messageTemplate = messageTemplate.Replace("$RESULT_NULLCONSTBODY", "");
            messageTemplate = messageTemplate.Replace("$RESULT_EXTRACONSTRUCTOR", "");

            #endregion

            #region REPLACE FEEDBACK PLACEHOLDERS

            string feedbackDict = resultMessage.GenFields();
            string feedbackClassName = className + "Feedback";
            messageTemplate = messageTemplate.Replace("$FEEDBACK_CLASS", feedbackClassName);
            messageTemplate = messageTemplate.Replace("$FEEDBACK_ISMETA", feedbackMessage.meta.ToString().ToLower());
            messageTemplate = messageTemplate.Replace("$FEEDBACK_MSGTYPE", "MsgTypes." + fileNamespace.Replace("Messages.", "") + "__" + feedbackClassName);
            messageTemplate = messageTemplate.Replace("$FEEDBACK_MESSAGEDEFINITION", "@\"" + feedbackDefinitionString + "\"");
            messageTemplate = messageTemplate.Replace("$FEEDBACK_HASHEADER", feedbackMessage.HasHeader.ToString().ToLower());
            messageTemplate = messageTemplate.Replace("$FEEDBACK_FIELDS", feedbackDict.Length > 5 ? "{{" + feedbackDict + "}}" : "()");
            messageTemplate = messageTemplate.Replace("$FEEDBACK_NULLCONSTBODY", "");
            messageTemplate = messageTemplate.Replace("$FEEDBACK_EXTRACONSTRUCTOR", "");

            #endregion

            #region MD5

            messageTemplate = messageTemplate.Replace("$GOAL_MD5SUM", MD5.Sum(goalMessage));
            messageTemplate = messageTemplate.Replace("$RESULT_MD5SUM", MD5.Sum(resultMessage));
            messageTemplate = messageTemplate.Replace("$FEEDBACK_MD5SUM", MD5.Sum(feedbackMessage));

            #endregion

            string goalDeserializationCode = "";
            string goalSerializationCode = "";
            string goalRandomizationCode = "";
            string goalEqualizationCode = "";
            string resultDeserializationCode = "";
            string resultSerializationCode = "";
            string resultRandomizationCode = "";
            string resultEqualizationCode = "";
            string feedbackDeserializationCode = "";
            string feedbackSerializationCode = "";
            string feedbackRandomizationCode = "";
            string feedbackEqualizationCode = "";

            for (int i = 0; i < goalMessage.Stuff.Count; i++)
            {
                goalDeserializationCode += goalMessage.GenerateDeserializationCode(goalMessage.Stuff[i], 1);
                goalSerializationCode += goalMessage.GenerateSerializationCode(goalMessage.Stuff[i], 1);
                goalRandomizationCode += goalMessage.GenerateRandomizationCode(goalMessage.Stuff[i], 1);
                goalEqualizationCode += goalMessage.GenerateEqualityCode(goalMessage.Stuff[i], 1);
            }
            for (int i = 0; i < resultMessage.Stuff.Count; i++)
            {
                resultDeserializationCode += resultMessage.GenerateDeserializationCode(resultMessage.Stuff[i], 1);
                resultSerializationCode += resultMessage.GenerateSerializationCode(resultMessage.Stuff[i], 1);
                resultRandomizationCode += resultMessage.GenerateRandomizationCode(resultMessage.Stuff[i], 1);
                resultEqualizationCode += resultMessage.GenerateEqualityCode(resultMessage.Stuff[i], 1);
            }
            for (int i = 0; i < feedbackMessage.Stuff.Count; i++)
            {
                feedbackDeserializationCode += feedbackMessage.GenerateDeserializationCode(feedbackMessage.Stuff[i], 1);
                feedbackSerializationCode += feedbackMessage.GenerateSerializationCode(feedbackMessage.Stuff[i], 1);
                feedbackRandomizationCode += feedbackMessage.GenerateRandomizationCode(feedbackMessage.Stuff[i], 1);
                feedbackEqualizationCode += feedbackMessage.GenerateEqualityCode(feedbackMessage.Stuff[i], 1);
            }

            messageTemplate = messageTemplate.Replace("$GOAL_SERIALIZATIONCODE", goalSerializationCode);
            messageTemplate = messageTemplate.Replace("$GOAL_DESERIALIZATIONCODE", goalDeserializationCode);
            messageTemplate = messageTemplate.Replace("$GOAL_RANDOMIZATIONCODE", goalRandomizationCode);
            messageTemplate = messageTemplate.Replace("$GOAL_EQUALITYCODE", goalEqualizationCode);
            messageTemplate = messageTemplate.Replace("$RESULT_SERIALIZATIONCODE", resultSerializationCode);
            messageTemplate = messageTemplate.Replace("$RESULT_DESERIALIZATIONCODE", resultDeserializationCode);
            messageTemplate = messageTemplate.Replace("$RESULT_RANDOMIZATIONCODE", resultRandomizationCode);
            messageTemplate = messageTemplate.Replace("$RESULT_EQUALITYCODE", resultEqualizationCode);
            messageTemplate = messageTemplate.Replace("$FEEDBACK_SERIALIZATIONCODE", feedbackSerializationCode);
            messageTemplate = messageTemplate.Replace("$FEEDBACK_DESERIALIZATIONCODE", feedbackDeserializationCode);
            messageTemplate = messageTemplate.Replace("$FEEDBACK_RANDOMIZATIONCODE", feedbackRandomizationCode);
            messageTemplate = messageTemplate.Replace("$FEEDBACK_EQUALITYCODE", feedbackEqualizationCode);

            /*string md5 = MD5.Sum(this);
            if (md5 == null)
                return null;*/
            //messageTemplate = messageTemplate.Replace("$MYSRVMD5SUM", "");



            /********END BLOCK**********/
            return messageTemplate;
        }


        private (List<string> GoalParamters, List<string> ResultParameters, List<string> FeedbackParameters) ParseActionFile
            (string[] lines)
        {
            var goalParameters = new List<string>();
            var resultParameters = new List<string>();
            var feedbackParameters = new List<string>();

            linesOfActionFile = new List<string>();
            int foundDelimeters = 0;

            // Search through for the "---" separator between request and response
            for (int lineNumber = 0; lineNumber < lines.Length; lineNumber++)
            {
                lines[lineNumber] = lines[lineNumber].Replace("\"", "\\\"");
                if (lines[lineNumber].Contains('#'))
                {
                    lines[lineNumber] = lines[lineNumber].Substring(0, lines[lineNumber].IndexOf('#'));
                }
                lines[lineNumber] = lines[lineNumber].Trim();

                if (lines[lineNumber].Length == 0)
                {
                    continue;
                }
                linesOfActionFile.Add(lines[lineNumber]);

                if (lines[lineNumber].Contains("---"))
                {
                    foundDelimeters += 1;
                    continue;
                }

                if (foundDelimeters == 0)
                {
                    if (goalParameters.Count == 0)
                    {
                        goalParameters.Add("Header header");
                        goalParameters.Add("actionlib_msgs/GoalID goal_id");
                    }
                    if (!lines[lineNumber].Contains("Header"))
                    {
                        goalParameters.Add(lines[lineNumber]);
                    }
                }
                else if (foundDelimeters == 1)
                {
                    if (resultParameters.Count == 0)
                    {
                        resultParameters.Add("Header header");
                        resultParameters.Add("actionlib_msgs/GoalStatus status");
                    }
                    if (!lines[lineNumber].Contains("Header"))
                    {
                        resultParameters.Add(lines[lineNumber]);
                    }
                }
                else if (foundDelimeters == 2)
                {
                    if (feedbackParameters.Count == 0)
                    {
                        feedbackParameters.Add("Header header");
                        feedbackParameters.Add("actionlib_msgs/GoalStatus status");
                    }
                    if (!lines[lineNumber].Contains("Header"))
                    {
                        feedbackParameters.Add(lines[lineNumber]);
                    }
                } else
                {
                    throw new InvalidOperationException($"Action file has an unexpected amount of --- delimeters.");
                }
            }

            return (goalParameters, resultParameters, feedbackParameters);
        }
    }
}
