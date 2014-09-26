namespace sensu_client
{
    public interface IKeepAliveScheduler
    {
        void KeepAlive();
        void Stop();
    }
}