using System;
using System.ServiceProcess;
using System.Threading;
using NLog;
using Topshelf;
using sensu_client.Configuration;

namespace sensu_client
{
    public class SensuClient : ISensuClient
    {
        private readonly ISensuClientConfigurationReader _sensuClientConfigurationReader;
        private readonly IKeepAliveScheduler _keepAliveScheduler;
        private readonly ISubScriptionsReceiver _subScriptionsReceiver;
        private static readonly Logger Log = LogManager.GetCurrentClassLogger();
        private static readonly object MonitorObject = new object();
        private ISensuClientConfig _configsettings;
        private Thread _keepalivethread;
        private Thread _subscriptionsthread;

        public SensuClient(ISensuClientConfigurationReader sensuClientConfigurationReader, IKeepAliveScheduler keepAliveScheduler, ISubScriptionsReceiver subScriptionsReceiver)
        {
            _sensuClientConfigurationReader = sensuClientConfigurationReader;

            _keepAliveScheduler = keepAliveScheduler;
            _subScriptionsReceiver = subScriptionsReceiver;
            _keepalivethread = new Thread(_keepAliveScheduler.KeepAlive);
            _subscriptionsthread = new Thread(_subScriptionsReceiver.Subscriptions);

            LoadConfiguration();
        }

        public void Start()
        {

            try
            {
                Log.Info("Inside start service sensu-client ");


                Log.Info("Sensu-client configuration loaded!");
                //Start Keepalive thread
                _keepalivethread.Start();
                Log.Info("Sensu-client KeeAliveScheduler started!");
                //Start subscriptions thread.
                _subscriptionsthread.Start();
                Log.Info("Sensu-client Subscription started!");
            }
            catch (Exception exception)
            {
                Log.Error("Fail on starting ", exception);
            }
            
        }

        public void Stop()
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
        protected void OnStart(string[] args)
        {
            Start();
        }

        protected void OnStop()
        {
            Stop();
        }

        public bool Start(HostControl hostControl)
        {
            Start();
            return true;
        }

        public bool Stop(HostControl hostControl)
        {
            Stop();
            return true;
        }
    }
}
