using RabbitMQ.Client;

namespace sensu_client.Connection
{
    public interface ISensuRabbitMqConnectionFactory
    {
        IConnection GetRabbitConnection();
    }
}