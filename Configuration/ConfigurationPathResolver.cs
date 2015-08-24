using NLog;
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
            var DefaultDirname = Path.Combine(@"c:\etc", "sensu", Configdirname);
            if (Directory.Exists(DefaultDirname))
                return DefaultDirname;

            Logger log = LogManager.GetCurrentClassLogger();
            log.Debug("Directory " + DefaultDirname + " not found. Using default one");

            return Path.Combine(Path.GetDirectoryName(AppDomain.CurrentDomain.BaseDirectory), Configdirname);
        }

        public string ConfigFileName()
        {
            var DefaultFilename = Path.Combine(@"c:\etc", "sensu", Configfilename );
            if (File.Exists(DefaultFilename))
                return DefaultFilename;
            Logger log = LogManager.GetCurrentClassLogger();
            log.Debug("File " + DefaultFilename + " not found. Using default one");

            return Path.Combine(Path.GetDirectoryName(AppDomain.CurrentDomain.BaseDirectory), Configfilename);
        }
    }
}