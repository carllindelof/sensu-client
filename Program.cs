using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.ServiceProcess;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using NLog;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using RabbitMQ.Client.Exceptions;
using RabbitMQ.Client.Framing.v0_9_1;
using StructureMap;
using sensu_client.Configuration;
using Container = StructureMap.Container;

namespace sensu_client
{
    class Program
    {
        static Logger _log;
        static void Main()
        {
            _log = LogManager.GetCurrentClassLogger();
#if DEBUG
            LogManager.GlobalThreshold = LogLevel.Trace;
#endif
            AppDomain.CurrentDomain.UnhandledException += CurrentDomainUnhandledException;

            var container = new Container(new SensuClientRegistry());

            var sensuClient = container.GetInstance<ISensuClient>();

            if (Environment.UserInteractive)
            {
                sensuClient.Start();
                Console.CancelKeyPress += delegate
                {
                    _log.Info("Cancel Key Pressed. Shutting Down.");
                };
            }
            else
            {
                ServiceBase.Run(container.GetInstance<ServiceBase>());
            }

        }
        static void CurrentDomainUnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            _log.Error("Global Exception handler called with exectpion: {0}", e.ExceptionObject);
        }

    }
}
