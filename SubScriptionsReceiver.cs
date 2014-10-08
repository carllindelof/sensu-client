using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Text;
using System.Threading;
using NLog;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using RabbitMQ.Client.Framing.v0_9_1;
using sensu_client.Command;
using sensu_client.Configuration;
using sensu_client.Connection;

namespace sensu_client
{
    public class SubScriptionsReceiver : ISubScriptionsReceiver
    {
        private static readonly Logger Log = LogManager.GetCurrentClassLogger();
        private static readonly object MonitorObject = new object();
        private const int KeepAliveTimeout = 20000;
        private readonly ManualResetEvent _stopEvent = new ManualResetEvent(false);
        private static readonly List<string> ChecksInProgress = new List<string>();

        private readonly ISensuClientConfigurationReader _sensuClientConfigurationReader;
        private readonly ISensuRabbitMqConnectionFactory _sensuRabbitMqConnectionFactory;
        private static readonly JsonSerializerSettings SerializerSettings = new JsonSerializerSettings { Formatting = Formatting.None };


        public SubScriptionsReceiver(ISensuClientConfigurationReader sensuClientConfigurationReader,ISensuRabbitMqConnectionFactory sensuRabbitMqConnectionFactory)
        {
            _sensuClientConfigurationReader = sensuClientConfigurationReader;
            _sensuRabbitMqConnectionFactory = sensuRabbitMqConnectionFactory;
        }

        private bool Running
        {
            get { return !_stopEvent.WaitOne(0); }
        }


        public void Stop()
        {
            _stopEvent.Set();
        }

        public void Subscriptions()
        {
            Log.Debug("Subscribing to client subscriptions");
            IModel ch = null;
            QueueingBasicConsumer consumer = null;
            while (Running)
            {
                if (ch != null && ch.IsOpen && consumer.IsRunning)
                {
                    GetPayloadFromOpenChannel(consumer);
                }
                else
                {
                    ch = CreateChannelAndConsumer(ch, ref consumer);
                }
                //Lets us quit while we're still sleeping.
                lock (MonitorObject)
                {
                    if (!Running)
                    {
                        Log.Warn("Quitloop set, exiting main loop");
                        break;
                    }
                    Monitor.Wait(MonitorObject, KeepAliveTimeout);
                    if (!Running)
                    {
                        Log.Warn("Quitloop set, exiting main loop");
                        break;
                    }
                }
            }
        }


        private IModel CreateChannelAndConsumer(IModel ch, ref QueueingBasicConsumer consumer)
        {
            Log.Error("rMQ Q is closed, Getting connection");
            var connection = _sensuRabbitMqConnectionFactory.GetRabbitConnection();
            if (connection == null)
            {
                //Do nothing - we'll loop around the while loop again with everything null and retry the connection.
            }
            else
            {
                ch = connection.CreateModel();
                var q = ch.QueueDeclare("", false, false, true, null);
                foreach (var subscription in _sensuClientConfigurationReader.SensuClientConfig.Client.Subscriptions)
                {
                    Log.Debug("Binding queue {0} to exchange {1}", q.QueueName, subscription);
                    try
                    {
                        ch.QueueBind(q.QueueName, subscription, "");
                    }
                    catch (Exception exception)
                    {
                        Log.Warn(String.Format("Could not bind to subscription: {0}", subscription),  exception);
                    }
                }
                consumer = new QueueingBasicConsumer(ch);
                try
                {
                    ch.BasicConsume(q.QueueName, true, consumer);
                }
                catch (Exception)
                {
                    Log.Warn("Could not consume queue: {0}", q.QueueName);
                }
            }
            return ch;
        }

        private void GetPayloadFromOpenChannel(QueueingBasicConsumer consumer)
        {
            BasicDeliverEventArgs msg;
            consumer.Queue.Dequeue(100, out msg);
            if (msg != null)
            {
                var payload = Encoding.UTF8.GetString(((BasicDeliverEventArgs)msg).Body);
                try
                {
                    var check = JObject.Parse(payload);
                    Log.Debug("Received check request: {0}",JsonConvert.SerializeObject(check, SerializerSettings));
                    ProcessCheck(check);
                }
                catch (JsonReaderException)
                {
                    Log.Error("Malformed Check: {0}", payload);
                }
            }
        }

        public void ProcessCheck(JObject check)
        {
            Log.Debug("Processing check {0}", JsonConvert.SerializeObject(check, SerializerSettings));
            JToken command;
            if (check.TryGetValue("command", out command))
            {
                check = _sensuClientConfigurationReader.MergeCheckWithLocalCheck(check);
                if(!ShouldRunInSafeMode(check))
                ExecuteCheckCommand(check);
            }
            else
            {
                Log.Warn("Unknown check exception: {0}", check);
            }
        }

        private bool ShouldRunInSafeMode(JObject check)
        {
            var safemode = _sensuClientConfigurationReader.SensuClientConfig.Client.SafeMode;
            
            if (!safemode) return false;

            var checks = _sensuClientConfigurationReader.SensuClientConfig.Checks;

            if (checks != null && checks.Exists(c=> c.Name == check["name"].ToString()))
            {
                return false;
            }
            check["output"] = "Check is not locally defined (safemode)";
            check["status"] = 3;
            check["handle"] = false;
            PublishResult(check);
           
            return true;
        }
       

        private void PublishResult(JObject check)
        {
            var payload = new JObject();
            payload["check"] = check;
            payload["client"] = _sensuClientConfigurationReader.SensuClientConfig.Client.Name;
            payload["check"]["executed"] = CreateTimeStamp();

            Log.Info("Publishing Check to sensu server {0} \n", JsonConvert.SerializeObject(payload, SerializerSettings));
            using (var ch = _sensuRabbitMqConnectionFactory.GetRabbitConnection().CreateModel())
            {
                var properties = new BasicProperties
                    {
                        ContentType = "application/octet-stream",
                        Priority = 0,
                        DeliveryMode = 1
                    };
                ch.BasicPublish("", "results", properties, Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(payload)));
            }
            ChecksInProgress.Remove(check["name"].ToString());
        }

        public void ExecuteCheckCommand(JObject check)
        {
            Log.Debug("Attempting to execute check command {0}", JsonConvert.SerializeObject(check, SerializerSettings));
            if (check["name"] == null)
            {
                CheckDidNotHaveValidName(check);
                return;
            }

            if(CheckIsInProgress(check))
            {
                Log.Warn("Previous check command execution in progress {0}", check["command"]);
                return;
            }

            ChecksInProgress.Add(check["name"].ToString());
            int? timeout = null;
            if (check["timeout"] != null)
                timeout = TryParseNullable(check["timeout"].ToString());

            var commandToExcecute =CommandFactory.Create(
                                                    new CommandConfiguration()
                                                        {
                                                            Plugins = _sensuClientConfigurationReader.SensuClientConfig.Client.Plugins,
                                                            TimeOut = timeout
                                                        }, check["command"].ToString());

            Log.Debug("About to run command: " + commandToExcecute.Arguments);
            var result = commandToExcecute.Execute();

            check["output"] = result.Output;
            check["status"] = result.Status;
            check["duration"] = result.Duration;

            PublishResult(check);
        }

        private static bool CheckIsInProgress(JObject check)
        {
            return ChecksInProgress.Contains(check["name"].ToString());
        }

        private void CheckDidNotHaveValidName(JObject check)
        {
            check["output"] = "Check didn't have a valid name";
            check["status"] = 3;
            check["handle"] = false;
            PublishResult(check);
        }

        public int? TryParseNullable(string val)
        {
            int outValue;
            return int.TryParse(val, out outValue) ? (int?)outValue : null;
        }


        private static long CreateTimeStamp()
        {
            return Convert.ToInt64(Math.Round((DateTime.UtcNow - new DateTime(1970, 1, 1, 0, 0, 0)).TotalSeconds, MidpointRounding.AwayFromZero));
        }

        private static string SubstitueCommandTokens(JObject check, out List<string> unmatchedTokens)
        {
            var temptokens = new List<string>();
            var command = check["command"].ToString();
            //var substitutionRegex = new Regex(":::(.*?):::", RegexOptions.Compiled);
            //command = substitutionRegex.Replace(command, match =>
            //    {
            //        var matched = "";
            //        foreach (var p in match.Value.Split('.'))
            //        {
            //            if (_configsettings.Client[p] != null)
            //            {
            //                matched += _configsettings["client"][p];
            //            }
            //            else
            //            {
            //                break;
            //            }
            //        }
            //        if (string.IsNullOrEmpty(matched)) { temptokens.Add(match.Value); }
            //        return matched;
            //    });
            unmatchedTokens = temptokens;
            return command;
        }
    }
}