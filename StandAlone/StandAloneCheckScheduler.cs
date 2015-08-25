using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using NLog;
using Newtonsoft.Json.Linq;
using RabbitMQ.Client;
using sensu_client.Configuration;
using sensu_client.Helpers;
using Timer = System.Timers.Timer;

namespace sensu_client.StandAlone
{
    public class StandAloneCheckScheduler : IStandAloneCheckScheduler
    {
        private readonly static ConcurrentDictionary<string,SchedueledItem> SheduledItems = new ConcurrentDictionary<string,SchedueledItem>();
        private readonly ISensuClientConfigurationReader _sensuClientConfigurationReader;
        private readonly ICheckProcessor _processor;
        private static readonly Logger Log = LogManager.GetCurrentClassLogger();
        private readonly ManualResetEvent _stopEvent = new ManualResetEvent(false);
        private Timer _localCheckSchedulerTimer;

        public StandAloneCheckScheduler(ISensuClientConfigurationReader sensuClientConfigurationReader, ICheckProcessor processor)
        {
            _sensuClientConfigurationReader = sensuClientConfigurationReader;
            _processor = processor;
            
            InitializeSheduledTimer();

            InitializeSheduledItems();
        }

        private void InitializeSheduledItems()
        {
            var obj = (JObject)_sensuClientConfigurationReader.Configuration.Config["checks"];

            var standAloneChecks = SensuClientHelper.GetStandAloneChecks(obj);

            if (!standAloneChecks.Any())
            {
                return;
            }

            foreach (JObject item in standAloneChecks)
            {
                if (item == null || item["interval"] == null) continue;

                var defaultInterval = item["interval"].Value<int>();
                SheduledItems.TryAdd(item["name"].Value<string>(), new SchedueledItem(() => _processor.ProcessCheck(item), item["name"].Value<string>(), defaultInterval));
                Log.Debug("Adding scheduled item with interval property:  {0} | {1}", defaultInterval, item["name"]);
            }
        }

        public void Stop()
        {
            _localCheckSchedulerTimer.Stop();
            _stopEvent.Set();
        }

        public void Start()
        {
            Log.Debug("Starting StandAlone scheduler timer");
            _localCheckSchedulerTimer.Enabled = true;
            _localCheckSchedulerTimer.Start();
                
        }

        private void InitializeSheduledTimer()
        {
// ReSharper disable UseObjectOrCollectionInitializer
            _localCheckSchedulerTimer = new Timer();
// ReSharper restore UseObjectOrCollectionInitializer

            //Set to every half second since the smallest intervall on a check is 1 seccond.
            _localCheckSchedulerTimer.Interval = 500;
            _localCheckSchedulerTimer.Elapsed += LocalCheckSchedulerTimer_Elapsed;
            
        }

        void LocalCheckSchedulerTimer_Elapsed(object sender, System.Timers.ElapsedEventArgs e)
        {
            if (!SheduledItems.Any()) return;
            
            var itemsToProcess = SheduledItems.Where(x => x.Value.NextRunTime < DateTime.Now).ToList();
            foreach (var schedueleItem in itemsToProcess.ToList())
            {
                SchedueledItem workItem = null;
                if (SheduledItems.TryRemove(schedueleItem.Key, out workItem))
                {
                    var processCheck = ProcessCheck(workItem);
                    processCheck.ContinueWith(ReAddWorkItemAfterCompletion);
                }
            }
        }

        public static Task<SchedueledItem> ProcessCheck(SchedueledItem workItem)
        {
            return Task.Factory.StartNew(() =>
            {
                workItem.Execute();
                return workItem;
            });
        }

        private static void ReAddWorkItemAfterCompletion(Task<SchedueledItem> executedTask)
        {
            var scheduleItem = executedTask.Result;
            Console.WriteLine("ReAdding: " + scheduleItem.Key);
            SheduledItems.TryAdd(scheduleItem.Key,scheduleItem);
        }
    }
}