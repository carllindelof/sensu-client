using Newtonsoft.Json.Linq;

namespace sensu_client.Configuration
{
    public interface ISensuClientConfigurationReader
    {
        ISensuClientConfig SensuClientConfig { get; }
        IConfiguration Configuration { get; }
        JObject ReadConfigSettingsJson();
        JObject MergeCheckWithLocalCheck(JObject check);  

    }
}