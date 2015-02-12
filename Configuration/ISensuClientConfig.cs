using Newtonsoft.Json.Linq;

namespace sensu_client.Configuration
{

    public interface IConfiguration
    {
        JObject Config { get; set; }
    }

    public interface ISensuClientConfig
    {
        Client Client { get; set; }
        Rabbitmq Rabbitmq { get; set; }
        Checks Checks { get; set; } 
    }
}