using System;
using System.IO;
using NLog;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace sensu_client.Configuration
{
    public class SensuClientConfigConverter : JsonConverter
    {
        private static readonly Logger Log = LogManager.GetCurrentClassLogger();

        private readonly string _configDir;
        private readonly string _configfile;

        public SensuClientConfigConverter(string configfile, string configDir)
        {
            _configDir = configDir;
            _configfile = configfile;
        }

        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {

        }

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            var json = JObject.Load(reader);

            var config = new SensuClientConfig();
            serializer.Populate(json.CreateReader(), config);

            if (!Directory.Exists(_configDir)) return config;

            foreach (var configFile in Directory.GetFiles(_configDir))
            {
                using (var envReader = new StreamReader(configFile))
                {
                    using (var envJsonReader = new JsonTextReader(envReader))
                    {
                        serializer.Populate(envJsonReader, config);
                    }
                }
            }

            return config;
        }



        public override bool CanConvert(Type objectType)
        {
            return objectType == typeof(SensuClientConfig);
        }
    }
}