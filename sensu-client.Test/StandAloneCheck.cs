using System.Diagnostics;
using System.Linq;
using NUnit.Framework;
using Newtonsoft.Json.Linq;
using StructureMap;
using sensu_client;
using sensu_client.Configuration;
using sensu_client.Helpers;
using Shouldly;
namespace sensu_client_test
{
    [TestFixture]
    internal class StandAloneCheck
    {
        private Container _container;
        private ISensuClientConfigurationReader _sensuClientConfigurationReader;


        [SetUp]
        public void Init()
        {
            _container = new Container(new SensuClientRegistry());
            _container.Configure(
                cfg =>
                cfg.For<IConfigurationPathResolver>()
                   .Use<ConfigurationPathResolverTest>()
                   .Named("ConfigurationPathResolverTest"));

            _sensuClientConfigurationReader = _container.GetInstance<ISensuClientConfigurationReader>();
        }

        [Test]
        public void should_merge_localcheck_with_check_defenition()
        {
            var checks = (JObject)_sensuClientConfigurationReader.Configuration.Config["checks"];
            var check = SensuClientHelper.GetStandAloneChecks(checks).First();
            check = _sensuClientConfigurationReader.MergeCheckWithLocalCheck(check);

            check["team"].ToString().ShouldBe("pink");
        }
        [Test]
        public void should_return_standalone_checkdefenitions()
        {
            var obj = (JObject)_sensuClientConfigurationReader.Configuration.Config["checks"];

            var standAloneChecks = SensuClientHelper.GetStandAloneChecks(obj);

            standAloneChecks.Any().ShouldBe(true);
            standAloneChecks.Count().ShouldBe(2);
            //{"name":"check_standalone_cpu_windows","issued":1423609458,"command":"check_standalone_cpu_windows.ps1"}
            standAloneChecks.First()["command"].Value<string>().ShouldNotBeEmpty();
            standAloneChecks.First()["interval"].Value<int>().ShouldBeGreaterThan(0);
            standAloneChecks.First()["name"].Value<string>().ShouldNotBeEmpty();
            standAloneChecks.First()["name"].Value<string>().ShouldBe("check_standalone_cpu_windows");
        }

       
    }
}