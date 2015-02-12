using Newtonsoft.Json.Linq;

namespace sensu_client.Configuration
{
    public class Configuration : IConfiguration
    {
        public JObject Config { get; set; }
    }
}