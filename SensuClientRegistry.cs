using StructureMap.Configuration.DSL;
using StructureMap.Graph;
using sensu_client.Configuration;
using sensu_client.StandAlone;
using sensu_client.TcpServer;
using sensu_client.UdpReceiver;

namespace sensu_client
{
    public class SensuClientRegistry : Registry
    {
        public SensuClientRegistry()
        {
            For<ISensuClientConfig>().Use<SensuClientConfig>().Singleton();
            For<ISensuClientConfigurationReader>().Use<SensuClientConfigurationReader>().Singleton();
            For<ISensuClient>().Use<SensuClient>();
            For<IConfigurationPathResolver>().Use<ConfigurationPathResolver>().Named("ConfigurationPathResolver");
            For<ISubScriptionsReceiver>().Use<SubScriptionsReceiver>().Singleton();
            For<IKeepAliveScheduler>().Use<KeepAliveScheduler>().Singleton();
            For<ICheckProcessor>().Use<CheckProcessor>().Singleton();
            For<ITcpServer>().Use<TcpServer.TcpServer>().Singleton();
            For<IUdpReceiver>().Use<UdpReceiver.UdpReceiver>().Singleton();
            For<ISocketServer>().Use<SocketServer>().Singleton();
            For<IStandAloneCheckScheduler>().Use<StandAloneCheckScheduler>().Singleton();

            // ReSharper disable ConvertToLambdaExpression
            Scan(assembly =>
            {
                assembly.TheCallingAssembly();
                assembly.WithDefaultConventions();
            });
        }

    }
}