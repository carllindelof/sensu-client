using System;

namespace sensu_client.StandAlone
{
    public class SchedueledItem
    {
        private readonly string _key;
        private readonly Action _action;
        public readonly int _interval;
        public DateTime NextRunTime;
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
            _action();
            SetNextRunTime();
        }

        private void SetNextRunTime()
        {
            NextRunTime = DateTime.Now.AddSeconds(_interval);
        }

    }
}