using NUnit.Framework;
using Newtonsoft.Json.Linq;
using Shouldly;
using StructureMap;
using sensu_client;
using sensu_client.Configuration;
using sensu_client.Helpers;

namespace sensu_client_test
{
    [TestFixture]
    internal class CommandSubstition
    {
        private Container _container;
        private ISensuClientConfigurationReader _sensuClientConfigurationReader;
        private Check _check;
        private JObject _jsonCheck;
        private JObject _jsonClient;

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
            _check = _sensuClientConfigurationReader.SensuClientConfig.Checks[0];

            _jsonCheck = JObject.FromObject(_check);
            _jsonClient = JObject.FromObject(_sensuClientConfigurationReader.SensuClientConfig.Client);
        }


        [Test]
        public void can_substitute_command_with_arguments()
        {
            var parseErrors = "";
            _jsonCheck["command"] = "check-ram.rb -w :::params.ram.warning|10::: -c :::params.ram.critical|5:::";
            var jsonparams = @"{
                ram: {
                    warning: 20,
                    critical: 10
                }
            }";

            var clientparams = JObject.Parse(jsonparams);
            _jsonClient["params"] = clientparams;

            var substitueCommandTokens = SensuClientHelper.SubstitueCommandTokens
                                         (_jsonCheck, out parseErrors,_jsonClient);

            substitueCommandTokens.ShouldBe("check-ram.rb -w 20 -c 10");
        }

        [Test]
        public void can_substitute_command_from_client_config()
        {
            var parseErrors = "";

            _jsonCheck["command"] = ":::ramcheck|check-ram.rb:::";
            _jsonClient["ramcheck"] = "other-ram-check.sh";

            var substitueCommandTokens = SensuClientHelper.SubstitueCommandTokens
                                         (_jsonCheck, out parseErrors,_jsonClient);

            substitueCommandTokens.ShouldBe("other-ram-check.sh");
        }

        [Test]
        public void can_substitute_command_with_no_client_params()
        {
            var parseErrors = "";
            _jsonCheck["command"] = "check-ram.rb -w :::params.ram.warning|10::: -c :::params.ram.critical|5:::";

            var substitueCommandTokens = SensuClientHelper.SubstitueCommandTokens
                                        (_jsonCheck, out parseErrors,_jsonClient);
            substitueCommandTokens.ShouldBe("check-ram.rb -w 10 -c 5");
        }

        [Test]
        public void can_substitute_command_with_no_substitutions()
        {
            var parseErrors = "";
            _jsonCheck["command"] = "check-ram.rb -w 25 -c 45";
            string jsonparams = @"{
                ram: {
                    warning: 20,
                    critical: 10
                }
            }";

            var clientparams = JObject.Parse(jsonparams);
            _jsonClient["params"] = clientparams;

            var substitueCommandTokens = SensuClientHelper.SubstitueCommandTokens
                                        (_jsonCheck, out parseErrors,_jsonClient);

            substitueCommandTokens.ShouldBe("check-ram.rb -w 25 -c 45");
        }

        [Test]
        public void can_not_substitute_command_with_no_defaultdivider()
        {
            var parseErrors = "";
            _jsonCheck["command"] = "check-ram.rb -w :::params.ram.warning::: -c :::params.ram.critical:::";

            SensuClientHelper.SubstitueCommandTokens(_jsonCheck, out parseErrors, _jsonClient);

            parseErrors.ShouldBe(SensuClientHelper.ErroTextDefaultDividerMissing);
        }

        [Test]
        public void can_not_substitute_command_with_no_defaultvalue()
        {
            var parseErrors = "";
            _jsonCheck["command"] = "check-ram.rb -w :::params.ram.warning|::: -c :::params.ram.critical|:::";

            SensuClientHelper.SubstitueCommandTokens(_jsonCheck, out parseErrors, _jsonClient);
            
            parseErrors.ShouldBe(SensuClientHelper.ErroTextDefaultValueMissing +
                                 "params.ram.warning , params.ram.critical");
        }
    }
}
