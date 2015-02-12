using System;
using System.ServiceProcess;
using System.Threading;
using NLog;
using sensu_client.Configuration;
using sensu_client.StandAlone;

namespace sensu_client
{
    public class SensuClient : ServiceBase, ISensuClient
    {
        private readonly ISensuClientConfigurationReader _sensuClientConfigurationReader;
        private static IKeepAliveScheduler _keepAliveScheduler;
        private static ISubScriptionsReceiver _subScriptionsReceiver;
        private static ISocketServer _socketServer;
        private static IStandAloneCheckScheduler _standAloneCheckScheduler;

        private static readonly Logger Log = LogManager.GetCurrentClassLogger();
        private static readonly object MonitorObject = new object();
        private ISensuClientConfig _configsettings;
        private static Thread _keepalivethread;
        private static Thread _subscriptionsthread;
       

        public SensuClient(
            ISensuClientConfigurationReader sensuClientConfigurationReader, 
            IKeepAliveScheduler keepAliveScheduler, 
            ISubScriptionsReceiver subScriptionsReceiver,
            ISocketServer socketServer,
            IStandAloneCheckScheduler standAloneCheckScheduler
            )
        {
            Log.Debug("sensu-client constructor");

            try
            {
                _sensuClientConfigurationReader = sensuClientConfigurationReader;
            }
            catch (Exception ex)
            {

                Log.Error("Error getting configuration reader:", ex);
            }
            
            
            Log.Debug("sensu-client configuration read!");

            _keepAliveScheduler = keepAliveScheduler;
            Log.Debug("sensu-client keepalive i");

            _subScriptionsReceiver = subScriptionsReceiver;
            _socketServer = socketServer;
            _standAloneCheckScheduler = standAloneCheckScheduler;
            Log.Debug("sensu-client subscription");

            _socketServer = socketServer;
            Log.Debug("sensu-client socket server");

            _standAloneCheckScheduler = standAloneCheckScheduler;
            _keepalivethread = new Thread(_keepAliveScheduler.KeepAlive);
            _subscriptionsthread = new Thread(_subScriptionsReceiver.Subscriptions);
            Log.Debug("Threads started");

            LoadConfiguration();
            
            Log.Debug("Configuration loaded");

        }

        public static void Start()
        {
            try
            {
                Log.Info("Inside start service sensu-client ");

                _keepalivethread.Start();
                Log.Info("Sensu-client KeeAliveScheduler started!");
                
                _subscriptionsthread.Start();
                Log.Info("Sensu-client Subscription started!");

                _socketServer.Open();
                Log.Info("Socket server opened");

                _standAloneCheckScheduler.Start();
               Log.Info("StandAlone checks started");
            }
            catch (Exception exception)
            {
                Log.Error("Fail on starting ", exception);
            }
        }

        public new static void Stop()
        {
            Log.Info("Service OnStop called: Shutting Down");
            Log.Info("Attempting to obtain lock on monitor");
            lock (MonitorObject)
            {
                Log.Info("lock obtained");
                _keepAliveScheduler.Stop();
                _subScriptionsReceiver.Stop();
                _socketServer.Close();
                Monitor.Pulse(MonitorObject);
            }
        }

        public void LoadConfiguration()
        {
            try
            {
                _configsettings = _sensuClientConfigurationReader.SensuClientConfig;
            }
            catch (Exception ex)
            {
                
                Log.Error("Error loading configuration:",ex);
            }
            
        }

        public static void Halt()
        {
            Log.Info("Told to stop. Obeying.");
            Environment.Exit(1);
        }
        protected override void OnStart(string[] args)
        {
            Start();
        }

   
        protected override void OnStop()
        {
            Stop();
        }
    }
}
