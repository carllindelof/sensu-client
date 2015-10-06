using System;
using NLog;

namespace sensu_client.StandAlone
{
    public class SchedueledItem
    {
        private readonly string _key;
        private readonly Action _action;
        public readonly int _interval;
        public DateTime NextRunTime;
        private static readonly Logger Log = LogManager.GetCurrentClassLogger();
        public string Key { get { return _key; } }

        public SchedueledItem(Action action, string key, int interval)
        {
            _key = key;
            _action = action;
            _interval = interval;
            SetNextRunTime();
        }

        public void Execute()
        {
            try
            {
                _action();
            }
            catch (Exception e)
            {
                Log.Warn(e, "The task {0} failed", _key);
            }
            SetNextRunTime();
        }

        private void SetNextRunTime()
        {
            NextRunTime = DateTime.Now.AddSeconds(_interval);
        }

    }
}