using Newtonsoft.Json.Linq;

namespace sensu_client.Configuration
{
    public interface ISensuClientConfigurationReader
    {
        ISensuClientConfig SensuClientConfig { get; }
        JObject ReadConfigSettingsJson();
    }
}