using System;
using System.IO;
using System.Linq;
using NLog;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Threading;
using sensu_client.Helpers;
using Timer = System.Timers.Timer;

namespace sensu_client.Configuration
{
    public class SensuClientConfigurationReader : ISensuClientConfigurationReader
    {
        private readonly IConfigurationPathResolver _configurationPathResolver;
        private ISensuClientConfig _sensuClientConfig;
        private IConfiguration _configuration;
        private static readonly Logger Log = LogManager.GetCurrentClassLogger();
        static Logger _log;
         
       
        private static string _configfile = "";
        private static string _configdir = "";
        private ReaderWriterLockSlim rwlock;
        private FileSystemWatcher watcher;


        public SensuClientConfigurationReader(IConfigurationPathResolver configurationPathResolver)
        {
            _configurationPathResolver = configurationPathResolver;
            _log = LogManager.GetCurrentClassLogger();
            _configfile = configurationPathResolver.ConfigFileName();
            _configdir = configurationPathResolver.Configdir();
            _log.Info("Configuration file: {0}", _configfile);
            _log.Info("Configuration directory: {0}",_configdir);
            rwlock = new ReaderWriterLockSlim();
            InitFileSystemWatcher();
            
        }

        public ISensuClientConfig SensuClientConfig
        {
            get
            {
                if (_sensuClientConfig != null) return _sensuClientConfig;
                ReadConfigLocked();
                return _sensuClientConfig;
            }
        }

        public IConfiguration Configuration
        {
            get
            {
                if (_configuration != null) return _configuration;
                ReadConfigLocked();
                return _configuration;
            }
        }

        private ISensuClientConfig LoadConfig()
        {
            using (var reader = new StreamReader(_configfile))
            {
                using (var jsonReader = new JsonTextReader(reader))
                {
                    var serializer = new JsonSerializer();
                    serializer.Converters.Add(new SensuClientConfigConverter(_configfile, _configdir));
                    var config = serializer.Deserialize<SensuClientConfig>(jsonReader);

                    return config;
                }
            }
        }
         private IConfiguration LoadConfiguration()
         {
             var configuration = new Configuration();
             configuration.Config = ReadConfigSettingsJson();
             return configuration;
         }
        public JObject ReadConfigSettingsJson()
        {
            JObject configsettings;
            try
            {
                configsettings = JObject.Parse(File.ReadAllText(_configfile));
                //Grab configs from dir.
                if (Directory.Exists(_configdir))
                {
                    GetConfigurations(_configdir,configsettings);
                }
                else
                {
                    Log.Warn("Config dir not found");
                }
            }
            catch (FileNotFoundException ex)
            {
                Log.Error(string.Format("Config file not found: {0}", _configfile), ex);
                configsettings = new JObject();
            }
            return configsettings;
        }

        public JObject MergeCheckWithLocalCheck(JObject check)
        {
            var checks = (JObject)Configuration.Config["checks"];

            if (checks == null) return check;

            var localcheck = SensuClientHelper.GetCheckByName(check, checks);
            if (localcheck == null) return check;

            localcheck.Merge(check, new JsonMergeSettings
            {
                // union array values together to avoid duplicates
                MergeArrayHandling = MergeArrayHandling.Merge
            });

            return localcheck;
        }

        private static void GetConfigurations(string configdir, JObject configSettings)
        {
            foreach (var configFile in Directory.GetFiles(configdir))
            {
                using (var envReader = new StreamReader(configFile))
                {
                    using (var envJsonReader = new JsonTextReader(envReader))
                    {
                        var current = (JObject) JToken.ReadFrom(envJsonReader);
                        configSettings.Merge(current, new JsonMergeSettings 
                                            { MergeArrayHandling = MergeArrayHandling.Merge });
                    
                    }
                }
            }
        }

        private void InitFileSystemWatcher()
        {
            watcher = new FileSystemWatcher {Filter = "*.*"};
            watcher.Changed += Watcher_FileModified;
            watcher.Created += Watcher_FileModified;
            watcher.Deleted += Watcher_FileModified;

            watcher.Error += Watcher_Error;
            watcher.Path = _configdir;
            watcher.IncludeSubdirectories = true;
            watcher.EnableRaisingEvents = true;
        }

        private void Watcher_Error(object sender, ErrorEventArgs e)
        {
            // Watcher crashed. Re-init.
            InitFileSystemWatcher();
        }

        private void Watcher_FileModified(object sender, FileSystemEventArgs e)
        {
            _log.Debug("Hepp nu ändrades: ", e.Name);
            ReadConfigLocked();
        }

        private void ReadConfigLocked()
        {
            try
            {
                rwlock.EnterWriteLock();
                _sensuClientConfig = LoadConfig();
                _configuration = LoadConfiguration();
            }
            finally
            {
                rwlock.ExitWriteLock();
            }
        }


        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                if (rwlock != null)
                {
                    rwlock.Dispose();
                    rwlock = null;
                }
                if (watcher != null)
                {
                    watcher.EnableRaisingEvents = false;
                    watcher.Dispose();
                    watcher = null;
                }
            }
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }


    }
}