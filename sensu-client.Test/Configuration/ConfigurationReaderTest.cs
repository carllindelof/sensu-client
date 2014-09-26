using NUnit.Framework;
using Shouldly;
using StructureMap;
using sensu_client;
using sensu_client.Configuration;

namespace sensu_client_test.Configuration
{
    [TestFixture]
    public class ConfigurationReaderTest
    {

        [Test]
        public void can_read_client_config()
        {
             var container = new Container(new SensuClientRegistry());

            var configreader = container.GetInstance<ISensuClientConfigurationReader>();

            var client = configreader.SensuClientConfig.Client;

            client.Name.ShouldBe("demo-sensu-client");

        }

        [Test]
        public void can_resolve_default_plugins_folder()
        {
            var container = new Container(new SensuClientRegistry());

            var configreader = container.GetInstance<ISensuClientConfigurationReader>();

            var client = configreader.SensuClientConfig.Client;

            client.Plugins.ShouldBe(@"c:\opt\sensu\plugins");

        }


    }
}
