using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Security.Policy;
using System.Text;
using System.Text.RegularExpressions;
using NLog;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Schema;

namespace sensu_client.Helpers
{
    public static class SensuClientHelper
    {
        public const string ErroTextDefaultValueMissing = "Default missing:";
        public const string ErroTextDefaultDividerMissing = "Default divider missing";
        private static readonly Logger Log = LogManager.GetCurrentClassLogger();
        public static long CreateTimeStamp()
        {
            return Convert.ToInt64(Math.Round((DateTime.UtcNow - new DateTime(1970, 1, 1, 0, 0, 0)).TotalSeconds, MidpointRounding.AwayFromZero));
        }

        public static int? TryParseNullable(string val)
        {
            int outValue;
            return Int32.TryParse(val, out outValue) ? (int?)outValue : null;
        }

        public static string GetFQDN()
        {
            string domainName = IPGlobalProperties.GetIPGlobalProperties().DomainName;
            string hostName = Dns.GetHostName();

            if (!hostName.EndsWith(domainName))  // if hostname does not already include domain name
            {
                hostName += "." + domainName;   // add the domain name part
            }

            return hostName;                    // return the fully qualified name
        }


        public static List<string> GetRedactlist(JObject check)
        {
            List<string> redactlist = null;

            if ((check["client"] == null || check["client"]["redact"] == null)) return redactlist;

            var redact = check["client"]["redact"];

            if (redact == null || string.IsNullOrEmpty(redact.ToString())) return redactlist;

            var redactArray = redact.ToString().Split(' ');
            redactlist = new List<string>(redactArray);

            return redactlist;
        }

        private static JToken RedactInformationRecursive(JToken check, List<string> keys = null)
        {

            if (keys == null) keys = new List<string>(){"password", "passwd", 
                                                        "pass","api_key","api_token","access_key", 
                                                        "secret_key", "private_key","secret"};

            if (check.Children().Any())
            {
                foreach (var child in check.Children())
                {

                    RedactInformationRecursive(child, keys);

                    if (child.Type == JTokenType.Property)
                    {
                        var property = child as Newtonsoft.Json.Linq.JProperty;
                        
                        if (keys.Contains(property.Name))
                        {
                            property.Value = "REDACTED";
                        }      
                    }
                    
                }    
            }
            return check;
        }

        public static JToken RedactSensitiveInformaton(JToken check, List<string> keys = null)
        {
            var clonedCheck = check.DeepClone();

            return RedactInformationRecursive(clonedCheck, keys);
        }


        public static string SubstitueCommandTokens(JObject check, out string errors, JObject client)
        {
            errors = "";
            var tempErrors = new List<string>();
            var command = check["command"].ToString();

            if (!command.Contains(":::"))
                return command;

            if (!command.Contains("|"))
            {
                errors = ErroTextDefaultDividerMissing;
                return command;
            }
            
            var commandRegexp = new Regex(":::(.*?):::", RegexOptions.Compiled);

            command = commandRegexp.Replace(command, match => MatchCommandArguments(client, match, tempErrors));
            if (tempErrors.Count > 0)
            {
                errors = ErroTextDefaultValueMissing;
                errors += string.Join(" , ", tempErrors.ToArray());   
            }
            return command;
        }

        private static string MatchCommandArguments(JObject client, Match match, List<string> tempErrors)
        {
            var argumentValue = "";
            var commandArgument = match.Value.Replace(":::", "").Split(('|'));

            var matchedOrDefault = FindClientAttribute(client, commandArgument[0].Split('.').ToList(), commandArgument[1]);
            
            if (CommandArgumentHasValue(commandArgument)){tempErrors.Add(commandArgument[0]);}
            
            if (CommandArgumentIsNotNull(commandArgument)){argumentValue += matchedOrDefault;}
            
            return argumentValue;
        }

        private static bool CommandArgumentIsNotNull(string[] p)
        {
            return p[0] != null;
        }

        private static bool CommandArgumentHasValue(string[] p)
        {
            return p[1] == null || String.IsNullOrEmpty(p[1]);
        }

        private static string FindClientAttribute(JToken tree, ICollection<string> path, string defaultValue)
        {
            var attribute = tree[path.First()];
            path.Remove(path.First());
            if (attribute == null) return defaultValue;
            if (attribute.Children().Any())
            {
               return  FindClientAttribute(attribute, path, defaultValue);
            }
           
            return attribute.Value<string>() ?? defaultValue;
           
        }

        public static bool ValidateCheckResult(JObject check)
        {
            var regexItem = new Regex(@"/^[\w\.-]+$/",RegexOptions.Compiled);
           
            if (regexItem.IsMatch(check["name"].ToString()))return false;
            if (check["output"].Type != JTokenType.String) return false;
            if (check["status"].Type != JTokenType.Integer) return false;

            return true;
        }

        public static bool TryParseData(String data, out JObject result)
        {
            bool parseResult = false;
            JObject json = null;

            try
            {
                json = JObject.Parse(data);
                parseResult = true;
            }
            catch (Exception)
            {
                Log.Warn("Failed to parse to JObject: {0}",data);
            }

            result = json;
            return parseResult;

        }

        public static JObject GetCheckByName(JObject check, JObject checks)
        {
            return checks.Values<JToken>().Values<JObject>().
                Where(n => ((JProperty)n.Parent).Name == check["name"].Value<string>()).
                Select(n => n).FirstOrDefault();
        
        }

        public static List<JObject> GetStandAloneChecks(JObject obj)
        {
            var  standAloneChecks = new List<JObject>();

            try
            {
                standAloneChecks = obj.Values<JToken>().Values<JObject>()
                     .Where(n => n["standalone"].Value<bool>()).
                     Select(n => JObject.FromObject(new { name = ((JProperty)n.Parent).Name, command = n["command"], interval = n["interval"] })).ToList();
            }
            catch (Exception)
            {
                Log.Debug("No standalone checks fount!");
            }
            return standAloneChecks;

        }
        public static string CreateQueueName()
        {
            return String.Format("{0}-{1}-{2}", GetFQDN(), CoreAssembly.Version, CreateTimeStamp());
        }
    }
}
