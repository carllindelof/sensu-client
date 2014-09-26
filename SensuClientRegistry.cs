using System.ServiceProcess;
using StructureMap.Configuration.DSL;
using StructureMap.Graph;
using sensu_client.Command;
using sensu_client.Configuration;

namespace sensu_client
{
    public class SensuClientRegistry : Registry
    {
        public SensuClientRegistry()
        {
            For<ISensuClientConfig>().Use<SensuClientConfig>().Singleton();
            For<ISensuClientConfigurationReader>().Use<SensuClientConfigurationReader>().Singleton();
            For<ServiceBase>().Use<SensuClient>();
            // ReSharper disable ConvertToLambdaExpression
            Scan(assembly =>
            {
                assembly.TheCallingAssembly();
                assembly.WithDefaultConventions();
            });
        }

    }
}