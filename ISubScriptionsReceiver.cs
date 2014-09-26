namespace sensu_client
{
    public interface ISubScriptionsReceiver
    {
        void Subscriptions();
        void Stop();
    }
}