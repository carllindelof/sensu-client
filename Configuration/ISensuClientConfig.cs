namespace sensu_client.Configuration
{
    public interface ISensuClientConfig
    {
        Client Client { get; set; }
        Rabbitmq Rabbitmq { get; set; }
        Checks Checks { get; set; } 
    }
}