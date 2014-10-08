using System;
using System.IO;

namespace sensu_client.Configuration
{
    public class ConfigurationPathResolverTest : IConfigurationPathResolver
    {
        private const string Configfilename = "config.json";
        private const string Configdirname = "conf.d";
        public string Configdir()
        {
            return Path.Combine(Environment.CurrentDirectory, Configdirname);
        }

        public string ConfigFileName()
        {
            return Path.Combine(Environment.CurrentDirectory, Configfilename);
        }
    }
}