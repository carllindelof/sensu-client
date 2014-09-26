using RabbitMQ.Client;

namespace sensu_client
{
    public interface ISensuRabbitMqConnectionFactory
    {
        IConnection GetRabbitConnection();
    }
}