using System;
using NLog;
using Topshelf;
using Container = StructureMap.Container;

namespace sensu_client
{
    class Program
    {
        public const string ServiceName = "Sensu-client";
        static Logger _log;
        static void Main()
        {
            _log = LogManager.GetCurrentClassLogger();
#if DEBUG
            LogManager.GlobalThreshold = LogLevel.Trace;
#endif
            AppDomain.CurrentDomain.UnhandledException += CurrentDomainUnhandledException;

            try
            {
                var host = HostFactory.New(x =>
                {
                    x.Service<ISensuClient>(s =>
                    {
                        s.ConstructUsing(name =>
                        {
                            var container = new Container(new SensuClientRegistry());
                            return container.GetInstance<ISensuClient>();
                        });
                        s.WhenStarted(sc => sc.Start());
                        s.WhenStopped(sc => sc.Stop());
                    });
                    x.SetStartTimeout(TimeSpan.FromSeconds(40));
                    x.SetStopTimeout(TimeSpan.FromSeconds(40));
                    x.RunAsLocalSystem();
                    x.SetDisplayName(ServiceName);
                    x.SetDescription("Sensu client for running checks and metrics for Sensu");
                    x.SetServiceName(ServiceName);
                });

                host.Run();

            }
            catch (Exception exception)
            {
                _log.Error("Error in startup sensu-client",exception);
                
            }
            
        }
        static void CurrentDomainUnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            _log.Error("Global Exception handler called with exectpion: {0}", e.ExceptionObject);
        }

    }
}
