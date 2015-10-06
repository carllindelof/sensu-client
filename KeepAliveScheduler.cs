using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using NLog;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using RabbitMQ.Client;
using RabbitMQ.Client.Framing.v0_9_1;
using sensu_client.Configuration;
using sensu_client.Connection;
using sensu_client.Helpers;

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
                try {
                    if (ch == null || !ch.IsOpen)
                    {
                        if (ch != null && !ch.IsOpen)
                            Log.Error("rMQ Q is closed, Getting connection");
                        ch = CreateChannel(ch);
                    }
                    if (ch != null && ch.IsOpen)
                    {
                        PublishKeepAlive(ch);
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
                } catch (Exception e)
                {
                    Log.Warn(e, "Exception on KeepAlive thread");
                    Thread.Sleep(20000);
                }
            }
        }

        private IModel CreateChannel(IModel ch)
        {
            var connection = _connectionFactory.GetRabbitConnection();
            if (connection == null)
            {
                //Do nothing - we'll loop around the while loop again with everything null and retry the connection.
            }
            else
            {
                ch = connection.CreateModel();
            }
            return ch;
        }

        private void PublishKeepAlive(IModel ch)
        {
            var keepAlive = _sensuClientConfigurationReader.Configuration.Config["client"];

            keepAlive["timestamp"] = SensuClientHelper.CreateTimeStamp();
            keepAlive["plugins"] = "";

            List<string> redactlist = null;

            redactlist = SensuClientHelper.GetRedactlist((JObject) keepAlive);

            var payload = SensuClientHelper.RedactSensitiveInformaton(keepAlive, redactlist);

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
    }
}