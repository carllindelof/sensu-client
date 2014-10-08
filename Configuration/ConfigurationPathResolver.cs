using System;
using System.IO;
using System.Reflection;

namespace sensu_client.Configuration
{
    class ConfigurationPathResolver : IConfigurationPathResolver
    {
        private const string Configfilename = "config.json";
        private const string Configdirname = "conf.d";
        public string Configdir()
        {
            var configdir = string.Concat(Path.GetDirectoryName(AppDomain.CurrentDomain.BaseDirectory),
                                          Path.DirectorySeparatorChar, Configdirname);
            return configdir;
        }

        public string ConfigFileName()
        {
            var configfile = string.Concat(Path.GetDirectoryName(AppDomain.CurrentDomain.BaseDirectory),
                                           Path.DirectorySeparatorChar,
                                           Configfilename);
     
            return configfile;
        }
    }
}