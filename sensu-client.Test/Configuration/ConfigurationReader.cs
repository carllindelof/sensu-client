using NUnit.Framework;
using Newtonsoft.Json.Linq;
using Shouldly;
using StructureMap;
using sensu_client;
using sensu_client.Configuration;

namespace sensu_client_test.Configuration
{
    [TestFixture]
    public class ConfigurationReader
    {
        private Container _container;


        [SetUp]
        public void Init()
        {
            _container = new Container(new SensuClientRegistry());
            _container.Configure(cfg => cfg.For<IConfigurationPathResolver>().Use<ConfigurationPathResolverTest>().Named("ConfigurationPathResolverTest"));

        }

        [Test]
        public void can_read_client_config()
        {
            var configreader = _container.GetInstance<ISensuClientConfigurationReader>();

            var client = configreader.SensuClientConfig.Client;

            client.Name.ShouldBe("demo-sensu-client");

        }

        [Test]
        public void can_resolve_default_plugins_folder()
        {

            var configreader = _container.GetInstance<ISensuClientConfigurationReader>();

            var client = configreader.SensuClientConfig.Client;

            client.Plugins.ShouldBe(@"c:\opt\sensu\plugins");

        }


        [Test]
        public void can_merge_existing_payload_check_with_local_check()
        {
            var configreader = _container.GetInstance<ISensuClientConfigurationReader>();
            var check = JObject.Parse(@"{'name':'check_cpu_windows','issued':1412768253,'command':'check_cpu.ps1 -warn 7 -critical 15'}");

            var mergedcheck = configreader.MergeCheckWithLocalCheck(check);
            
            mergedcheck["interval"].ShouldBe(30);
            mergedcheck["issued"].ShouldBe(1412768253);

        }


        [Test]
        public void should_return_payload_check_when_no_local_check_found()
        {
            var configreader = _container.GetInstance<ISensuClientConfigurationReader>();
            var check = JObject.Parse(@"{'name':'check_nolocal_windows','issued':1412768253,'command':'check_cpu.ps1 -warn 7 -critical 15'}");

            var mergedcheck = configreader.MergeCheckWithLocalCheck(check);

            mergedcheck["name"].ShouldBe("check_nolocal_windows");
            

        }

    }
}
