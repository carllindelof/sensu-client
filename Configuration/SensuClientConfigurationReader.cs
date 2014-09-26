using System;
using System.IO;
using System.Linq;
using NLog;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Threading;
using Timer = System.Timers.Timer;

namespace sensu_client.Configuration
{
    public class SensuClientConfigurationReader : ISensuClientConfigurationReader
    {
        private ISensuClientConfig _sensuClientConfig;
        private static readonly Logger Log = LogManager.GetCurrentClassLogger();
        static Logger _log;
         
        private const string Configfilename = "config.json";
        private const string Configdirname = "conf.d";
        private static string _configfile = "";
        private static string _configdir = "";
        private ReaderWriterLockSlim rwlock;
        private FileSystemWatcher watcher;


        public SensuClientConfigurationReader()
        {
            _log = LogManager.GetCurrentClassLogger();
        
            var  currentDir = Environment.CurrentDirectory;

            _configfile = Path.Combine(currentDir, Configfilename);
            _configdir = Path.Combine(currentDir, Configdirname);
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

        private static void GetConfigurations(string configdir, JObject configsettings)
        {
            foreach (var settings in Directory.EnumerateFiles(configdir).Select(file => JObject.Parse(File.ReadAllText(file))))
            {
                foreach (var thingemebob in settings)
                {
                    configsettings.Add(thingemebob.Key, thingemebob.Value);
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