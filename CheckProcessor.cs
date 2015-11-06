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

    class  CheckData
    {
        public DateTime datetime;
        public Task<JObject> task = null;

        public CheckData()
        {
            this.datetime = DateTime.UtcNow;
        }
    }

    public class InProgressCheck
    {
        private static readonly Dictionary<string, CheckData> checksInProgress = new Dictionary<string, CheckData>();
        private static readonly Logger log = LogManager.GetCurrentClassLogger();

        public bool Lock(string name)
        {
            lock (checksInProgress)
            {
                if (checksInProgress.ContainsKey(name))
                {
                    var check = checksInProgress[name];
                    var elapsed = DateTime.UtcNow - check.datetime;
                    var task = check.task;
                    if (task == null)
                    {
                        log.Warn("Previous check command execution in progress {0} for {1} milliseconds", name, elapsed.TotalMilliseconds);
                        return false;
                    }

                    log.Warn("Previous check command execution in progress {0} for {1} milliseconds, with current status: {2}", name, elapsed.TotalMilliseconds, task.Status);
                    if ( ! task.IsCompleted)
                        return false;

                    log.Warn("Task {0} was completed but not removed, so lock will be allowed.", name);
                }
                log.Debug("Locking {0}", name);
                checksInProgress[name] = new CheckData();
                return true;
            }
        }

        public void UnlockAnyway(string name)
        {
            lock (checksInProgress)
            {
                if (!checksInProgress.ContainsKey(name))
                    return;

                checksInProgress.Remove(name);
                log.Debug("Unlocking {0}", name);
            }
        }

        public void Unlock(string name)
        {
            lock (checksInProgress)
            {
                if (!checksInProgress.ContainsKey(name))
                    return;

                var check = checksInProgress[name];
                if (check.task != null && check.task.IsCompleted)
                {
                    log.Debug("Unlocking {0}", name);
                    checksInProgress.Remove(name);
                }
                else
                {
                    log.Warn("Trying to unlock a task with status {1} for check {0}", name, check.task.Status);
                }
            }
        }

        public void SetTask(string name, Task<JObject> task)
        {
            lock (checksInProgress)
            {
                if (checksInProgress.ContainsKey(name))
                    checksInProgress[name].task = task;
            }
        }
    }

    public class CheckProcessor : ICheckProcessor
    {
        private readonly ISensuRabbitMqConnectionFactory _connectionFactory;
        private readonly ISensuClientConfigurationReader _sensuClientConfigurationReader;
        private static readonly Logger Log = LogManager.GetCurrentClassLogger();
        private static readonly InProgressCheck checksInProgress = new InProgressCheck();
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
            if (_sensuClientConfigurationReader.SensuClientConfig.Client.MailTo.Count > 0)
                payload["check"]["mail_to"] = string.Join(",", _sensuClientConfigurationReader.SensuClientConfig.Client.MailTo);
            payload["check"]["executed"] = SensuClientHelper.CreateTimeStamp();
            payload["check"]["issued"] = payload["check"]["executed"];

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

            if (!checksInProgress.Lock(checkName))
                return;

            try {
                var commandParseErrors = "";
                check["command"] = SensuClientHelper.SubstitueCommandTokens(
                    check, out commandParseErrors, (JObject)_sensuClientConfigurationReader.Configuration.Config["client"]);

                if (!String.IsNullOrEmpty(commandParseErrors))
                {
                    Log.Warn("Errors parsing the command: {0}", commandParseErrors);
                    CheckDidNotHaveValidParameters(check, commandParseErrors);
                    throw new Exception(String.Format("Errors parsing the command {0}", commandParseErrors));
                }

                Log.Debug("Preparing check to be launched: {0}", checkName);

                int? timeout = null;
                if (check["timeout"] != null)
                    timeout = SensuClientHelper.TryParseNullable(check["timeout"].ToString());

                var commandToExcecute = CommandFactory.Create(
                                                        new CommandConfiguration()
                                                        {
                                                            Plugins = _sensuClientConfigurationReader.SensuClientConfig.Client.Plugins,
                                                            TimeOut = timeout
                                                        }, check["command"].ToString());

                Log.Debug("About to run command: " + checkName);
                Task<JObject> executingTask = ExecuteCheck(check, commandToExcecute);
                checksInProgress.SetTask(checkName, executingTask);
                executingTask.ContinueWith(ReportCheckResultAfterCompletion).ContinueWith(CheckCompleted);
            } catch (Exception e)
            {
                Log.Error(e, "Error preparing check {0}", checkName);
                checksInProgress.UnlockAnyway(checkName);
            }
        }

        private void CheckCompleted(Task<JObject> executedTask)
        {
            var check = executedTask.Result;
            var name = check["name"].ToString();
            checksInProgress.Unlock(name);
        }

        private static Task<JObject> ExecuteCheck(JObject check, Command.Command command)
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

        private JObject ReportCheckResultAfterCompletion(Task<JObject> executedTask)
        {
            var check = executedTask.Result;
            try {
                Log.Debug("ReportCheckResultAfterCompletion | about to report result for check : " + check["name"]);
                PublishCheckResult(check);
            } catch( Exception e)
            {
                Log.Warn(e, "Error on check {0}", check);
            }
            return check;
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
