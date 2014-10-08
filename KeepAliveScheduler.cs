using System;
using System.Text;
using System.Threading;
using NLog;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using RabbitMQ.Client;
using RabbitMQ.Client.Framing.v0_9_1;
using sensu_client.Configuration;
using sensu_client.Connection;

namespace sensu_client
{
    public class KeepAliveScheduler : IKeepAliveScheduler
    {
        private readonly ISensuRabbitMqConnectionFactory _connectionFactory;
        private readonly ISensuClientConfigurationReader _sensuClientConfigurationReader;
        private const int KeepAliveTimeout = 20000;
        private static readonly Logger Log = LogManager.GetCurrentClassLogger();
        private static readonly object MonitorObject = new object();
        private readonly ManualResetEvent _stopEvent = new ManualResetEvent(false);

        public KeepAliveScheduler(ISensuRabbitMqConnectionFactory connectionFactory, ISensuClientConfigurationReader sensuClientConfigurationReader)
        {
            _connectionFactory = connectionFactory;
            _sensuClientConfigurationReader = sensuClientConfigurationReader;
        }


        private bool Running
        {
            get { return !_stopEvent.WaitOne(0); }
        }


        public void Stop()
        {
            _stopEvent.Set();
        }

        public void KeepAlive()
        {
            IModel ch = null;
            Log.Debug("Starting keepalive scheduler thread");
            while (Running)
            {
                if (ch == null || !ch.IsOpen)
                {
                    Log.Error("rMQ Q is closed, Getting connection");
                    var connection = _connectionFactory.GetRabbitConnection();
                    if (connection == null)
                    {
                        //Do nothing - we'll loop around the while loop again with everything null and retry the connection.
                    }
                    else
                    {
                        ch = connection.CreateModel();
                    }
                }
                if (ch != null && ch.IsOpen)
                {
                    var settings = new JsonSerializerSettings {ContractResolver = new LowercaseContractResolver()};
                    var clientJson = JsonConvert.SerializeObject(_sensuClientConfigurationReader.SensuClientConfig.Client, Formatting.Indented, settings);
                    
                    //Valid channel. Good to publish.
                    var payload = JObject.Parse(clientJson);
                    payload["timestamp"] = CreateTimeStamp();
                    payload["plugins"] = "";
                    Log.Debug("Publishing keepalive");
                    var properties = new BasicProperties
                        {
                            ContentType = "application/octet-stream",
                            Priority = 0,
                            DeliveryMode = 1
                        };
                    try
                    {
                        ch.BasicPublish("", "keepalives", properties, Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(payload)));
                    }
                    catch (Exception)
                    {
                        Log.Error("Lost MQ connection when trying to publish keepalives!");
                    }
                    
                }
                else
                {
                    Log.Error("Valiant attempts to get a valid rMQ connection were in vain. Skipping this keepalive loop.");
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
        private static long CreateTimeStamp()
        {
            return Convert.ToInt64(Math.Round((DateTime.UtcNow - new DateTime(1970, 1, 1, 0, 0, 0)).TotalSeconds, MidpointRounding.AwayFromZero));
        }

    }
}