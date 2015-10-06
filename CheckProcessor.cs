using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using NLog;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using sensu_client.Command;
using sensu_client.Configuration;
using sensu_client.Connection;
using RabbitMQ.Client.Framing.v0_9_1;
using sensu_client.Helpers;
using System.Diagnostics;

namespace sensu_client
{
    public interface ICheckProcessor
    {
        void ProcessCheck(JObject check);
        void PublishResult(JObject checkResult);
        void PublishCheckResult(JObject checkResult);
        
    }

    public class CheckProcessor : ICheckProcessor
    {
        private readonly ISensuRabbitMqConnectionFactory _connectionFactory;
        private readonly ISensuClientConfigurationReader _sensuClientConfigurationReader;
        private static readonly Logger Log = LogManager.GetCurrentClassLogger();
        private static readonly List<string> ChecksInProgress = new List<string>();
        private static readonly JsonSerializerSettings SerializerSettings = new JsonSerializerSettings { Formatting = Formatting.None };

        public CheckProcessor(ISensuRabbitMqConnectionFactory connectionFactory, ISensuClientConfigurationReader sensuClientConfigurationReader)
        {
            _connectionFactory = connectionFactory;
            _sensuClientConfigurationReader = sensuClientConfigurationReader;
        }
        public void ProcessCheck(JObject check)
        {
            try {
                Log.Debug("Processing check {0}", JsonConvert.SerializeObject(check, SerializerSettings));
                JToken command;
                if (check.TryGetValue("command", out command))
                {
                    check = _sensuClientConfigurationReader.MergeCheckWithLocalCheck(check);
                    if (!ShouldRunInSafeMode(check))
                        ExecuteCheckCommand(check);
                }
                else
                {
                    Log.Warn("Unknown check exception: {0}", check);
                }
            } catch (Exception e)
            {
                Log.Warn(e, "Unknown check exception: {0}", check);
            }
        }

        private bool ShouldRunInSafeMode(JObject check)
        {
            var safemode = _sensuClientConfigurationReader.SensuClientConfig.Client.SafeMode;

            if (!safemode) return false;

            var checks = _sensuClientConfigurationReader.SensuClientConfig.Checks;

            if (checks != null && checks.Exists(c => c.Name == check["name"].ToString()))
            {
                return false;
            }
            check["output"] = "Check is not locally defined (safemode)";
            check["status"] = 3;
            check["handle"] = false;

            PublishCheckResult(check);

            return true;
        }


        public void PublishCheckResult(JObject check)
        {
            var payload = new JObject();
            payload["check"] = check;
            payload["client"] = _sensuClientConfigurationReader.SensuClientConfig.Client.Name;
            payload["check"]["executed"] = SensuClientHelper.CreateTimeStamp();

            try
            {
                PublishResult(payload);

                if (_sensuClientConfigurationReader.SensuClientConfig.Client.SendMetricWithCheck && ((string)check["type"]).Equals("standard"))
                {
                    // publish the same result as metric too
                    payload["check"]["type"] = "metric";
                    PublishResult(payload);
                }
            } catch (Exception e)
            {
                Log.Warn(e, "Error publishing check Result");
            }
            finally
            {
                var name = check["name"].ToString();
                if (ChecksInProgress.Contains(name))
                    ChecksInProgress.Remove(name);
            }
        }

        public void PublishResult(JObject payload)
        {
            var json = JsonConvert.SerializeObject(payload);
            Log.Info("Publishing Check to sensu server {0} \n", json);
            using (var ch = _connectionFactory.GetRabbitConnection().CreateModel())
            {
                var properties = new BasicProperties
                    {
                        ContentType = "application/octet-stream",
                        Priority = 0,
                        DeliveryMode = 1
                    };
                ch.BasicPublish("", "results", properties, Encoding.UTF8.GetBytes(json));
            }
        }

        public void ExecuteCheckCommand(JObject check)
        {
            Log.Debug("Attempting to execute check command {0}", JsonConvert.SerializeObject(check, SerializerSettings));
            if (check["name"] == null)
            {
                CheckDidNotHaveValidName(check);
                return;
            }
            var checkName = check["name"].ToString();

            if (CheckIsInProgress(checkName))
            {
                Log.Warn("Previous check command execution in progress {0}", checkName);
                return;
            }
            var commandParseErrors = "";
            check["command"] = SensuClientHelper.SubstitueCommandTokens(
                check, out commandParseErrors,(JObject) _sensuClientConfigurationReader.Configuration.Config["client"]);

            if (!String.IsNullOrEmpty(commandParseErrors))
            {
                CheckDidNotHaveValidParameters(check, commandParseErrors);
                return;
            }

            Log.Debug("Preparing check to be launched: {0}", checkName);
            ChecksInProgress.Add(checkName);

            try {
                int? timeout = null;
                if (check["timeout"] != null)
                    timeout = SensuClientHelper.TryParseNullable(check["timeout"].ToString());

                var commandToExcecute = CommandFactory.Create(
                                                        new CommandConfiguration()
                                                        {
                                                            Plugins = _sensuClientConfigurationReader.SensuClientConfig.Client.Plugins,
                                                            TimeOut = timeout
                                                        }, check["command"].ToString());

                Log.Debug("About to run command: " + commandToExcecute.Arguments);

                Task<JObject> executingTask = ExecuteCheck(check, commandToExcecute);
                executingTask.ContinueWith(ReportCheckResultAfterCompletion);
            } catch (Exception e)
            {
                Log.Error(e, "Error preparing check {0}", checkName, e);
                ChecksInProgress.Remove(checkName);
            }
        }

        private static Task<JObject> ExecuteCheck(JObject check,Command.Command command)
        {

            return Task.Factory.StartNew(() =>
            {
                var stopwatch = new Stopwatch();
                stopwatch.Start();
                try
                {
                    var result = command.Execute();
                    check["output"] = result.Output;
                    check["status"] = result.Status;
                } catch (Exception e)
                {
                    Log.Warn(e, "Error running check {0}", check.ToString());
                    check["output"] = "";
                    check["status"] = 2;
                }
                stopwatch.Stop();
                check["duration"] = ((float)stopwatch.ElapsedMilliseconds) / 1000;
                return check;
            });
        }

        private void ReportCheckResultAfterCompletion(Task<JObject> executedTask)
        {
            var check = executedTask.Result;
            Log.Debug("ReportCheckResultAfterCompletion | about to report result for check : " + check["name"]);
            PublishCheckResult(check);
        }

        private static bool CheckIsInProgress(string checkName)
        {
            return ChecksInProgress.Contains(checkName);
        }

        private void CheckDidNotHaveValidParameters(JObject check, string commandErrors)
        {
            check["output"] = "Check didn't have valid command: " + commandErrors;
            check["status"] = 3;
            check["handle"] = false;
            PublishCheckResult(check);
        }

        private void CheckDidNotHaveValidName(JObject check)
        {
            check["output"] = "Check didn't have a valid name";
            check["status"] = 3;
            check["handle"] = false;
            PublishCheckResult(check);
        }
      
    }
}
