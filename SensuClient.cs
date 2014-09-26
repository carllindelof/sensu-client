using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.ServiceProcess;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using NLog;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using RabbitMQ.Client.Framing.v0_9_1;
using sensu_client.Configuration;

namespace sensu_client
{
    public class SensuClient : ServiceBase, ISensuClient
    {
        private readonly ISensuClientConfigurationReader _sensuClientConfigurationReader;
        private readonly IKeepAliveScheduler _keepAliveScheduler;
        private readonly ISubScriptionsReceiver _subScriptionsReceiver;
        private static readonly Logger Log = LogManager.GetCurrentClassLogger();
        private static readonly object MonitorObject = new object();
        private ISensuClientConfig _configsettings;

        public SensuClient(ISensuClientConfigurationReader sensuClientConfigurationReader, IKeepAliveScheduler keepAliveScheduler, ISubScriptionsReceiver subScriptionsReceiver)
        {
            _sensuClientConfigurationReader = sensuClientConfigurationReader;
            _keepAliveScheduler = keepAliveScheduler;
            _subScriptionsReceiver = subScriptionsReceiver;
        }

        public void Start()
        {
            LoadConfiguration();

            //Start Keepalive thread
            var keepalivethread = new Thread(_keepAliveScheduler.KeepAlive);
            keepalivethread.Start();

            //Start subscriptions thread.
            var subscriptionsthread = new Thread(_subScriptionsReceiver.Subscriptions);
            subscriptionsthread.Start();
        }

        public void LoadConfiguration()
        {
            _configsettings = _sensuClientConfigurationReader.SensuClientConfig;
        }

        public static void Halt()
        {
            Log.Info("Told to stop. Obeying.");
            Environment.Exit(1);
        }
        protected override void OnStart(string[] args)
        {
            Start();
            base.OnStart(args);
        }

        protected override void OnStop()
        {
            Log.Info("Service OnStop called: Shutting Down");
            Log.Info("Attempting to obtain lock on monitor");
            lock (MonitorObject)
            {
                Log.Info("lock obtained");
                _keepAliveScheduler.Stop();
                _subScriptionsReceiver.Stop();
                Monitor.Pulse(MonitorObject);
            }
            base.OnStop();
        }
    }
}
