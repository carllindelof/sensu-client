using System;
using System.Text;
using System.Threading;
using NLog;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using sensu_client.Configuration;
using sensu_client.Connection;
using sensu_client.Helpers;

namespace sensu_client
{
    public class SubScriptionsReceiver : ISubScriptionsReceiver
    {
        private static readonly Logger Log = LogManager.GetCurrentClassLogger();
        private static readonly object MonitorObject = new object();
        private const int KeepAliveTimeout = 20000;
        private readonly ManualResetEvent _stopEvent = new ManualResetEvent(false);

        private readonly ISensuClientConfigurationReader _sensuClientConfigurationReader;
        private readonly ISensuRabbitMqConnectionFactory _sensuRabbitMqConnectionFactory;
        private readonly ICheckProcessor _processor;
        private static readonly JsonSerializerSettings SerializerSettings = new JsonSerializerSettings { Formatting = Formatting.None };


        public SubScriptionsReceiver(ISensuClientConfigurationReader sensuClientConfigurationReader,ISensuRabbitMqConnectionFactory sensuRabbitMqConnectionFactory, ICheckProcessor processor)
        {
            _sensuClientConfigurationReader = sensuClientConfigurationReader;
            _sensuRabbitMqConnectionFactory = sensuRabbitMqConnectionFactory;
            _processor = processor;
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
                lock (MonitorObject)
                {
                    if (ch != null && ch.IsOpen && consumer.IsRunning)
                    {
                        GetPayloadFromOpenChannel(consumer);
                    }
                    else
                    {
                        ch = CreateChannelAndConsumer(ch, ref consumer);
                    }
                    if (!Running)
                    {
                        Log.Warn("Quitloop set, exiting main loop");
                        Monitor.Wait(MonitorObject, KeepAliveTimeout);
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
                var queueName =SensuClientHelper.CreateQueueName() ;
                foreach (var subscription in _sensuClientConfigurationReader.SensuClientConfig.Client.Subscriptions)
                {
                    Log.Debug("Binding queue {0} to exchange {1}", queueName, subscription);
                    try
                    {
                        ch.ExchangeDeclare(subscription, "fanout");

                        var q = ch.QueueDeclare(queueName, false, false, true, null);
           
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
                    ch.BasicConsume(queueName, true, consumer);
                }
                catch (Exception)
                {
                    Log.Warn("Could not consume queue: {0}", queueName);
                }
            }
            return ch;
        }

 

        private void GetPayloadFromOpenChannel(QueueingBasicConsumer consumer)
        {
            BasicDeliverEventArgs msg;
            try {
                consumer.Queue.Dequeue(100, out msg);
            } catch (Exception e)
            {
                Log.Warn(e);
                _sensuRabbitMqConnectionFactory.CloseRabbitConnection();
                msg = null; // do not process this pass
            }
            if (msg != null)
            {
                var payload = Encoding.UTF8.GetString(((BasicDeliverEventArgs)msg).Body);
                try
                {
                    var check = JObject.Parse(payload);
                    Log.Debug("Received check request: {0}",JsonConvert.SerializeObject(check, SerializerSettings));
                    _processor.ProcessCheck(check);
                }
                catch (JsonReaderException)
                {
                    Log.Error("Malformed Check: {0}", payload);
                }
            }
        }

    }
}