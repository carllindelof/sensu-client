using System;
using System.Reflection;
using System.ServiceProcess;
using System.Threading;
using NLog;
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
                var container = new Container(new SensuClientRegistry());
                var sensuClient = container.GetInstance<ISensuClient>() as ServiceBase;
                
                ServiceBase[] servicesToRun;
                servicesToRun = new ServiceBase[] 
                {
                    sensuClient
                 };

                if (Environment.UserInteractive)
                {
                    RunInteractive(servicesToRun);
                }
                else
                {
                    ServiceBase.Run(servicesToRun);
                }
            }
            catch (Exception exception)
            {
                _log.Error(exception, "Error in startup sensu-client.");
            }
            
        }

        //private static void StartHost(IContainer container)
        //{
        //    var host = HostFactory.New(x =>
        //        {
        //            x.Service<ISensuClient>(s =>
        //                {
        //                    s.ConstructUsing(name => container.GetInstance<ISensuClient>());
        //                    s.WhenStarted(sc => sc.Start());
        //                    s.WhenStopped(sc => sc.Stop());
        //                });
        //            x.SetStartTimeout(TimeSpan.FromSeconds(40));
        //            x.SetStopTimeout(TimeSpan.FromSeconds(40));
        //            x.RunAsLocalSystem();
        //            x.SetDisplayName(ServiceName);
        //            x.SetDescription("Sensu client for running checks and metrics for Sensu");
        //            x.SetServiceName(ServiceName);
        //        });

        //    host.Run();
        //}

        static void CurrentDomainUnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            _log.Error("Global Exception handler called with exectpion: {0}", e.ExceptionObject);
        }
        static void RunInteractive(ServiceBase[] servicesToRun)
        {
            Console.WriteLine("Services running in interactive mode.");
            Console.WriteLine();

            MethodInfo onStartMethod = typeof(ServiceBase).GetMethod("OnStart",
                BindingFlags.Instance | BindingFlags.NonPublic);
            foreach (ServiceBase service in servicesToRun)
            {
                Console.Write("Starting {0}...", service.ServiceName);
                onStartMethod.Invoke(service, new object[] { new string[] { } });
                Console.Write("Started");
            }

            Console.WriteLine();
            Console.WriteLine();
            Console.WriteLine(
                "Press any key to stop the services and end the process...");
            Console.ReadKey();
            Console.WriteLine();

            MethodInfo onStopMethod = typeof(ServiceBase).GetMethod("OnStop",
                BindingFlags.Instance | BindingFlags.NonPublic);
            foreach (ServiceBase service in servicesToRun)
            {
                Console.Write("Stopping {0}...", service.ServiceName);
                onStopMethod.Invoke(service, null);
                Console.WriteLine("Stopped");
            }

            Console.WriteLine("All services stopped.");
            // Keep the console alive for a second to allow the user to see the message.
            Thread.Sleep(1000);
        }
    }
}
