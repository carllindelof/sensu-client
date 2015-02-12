using NUnit.Framework;
using Newtonsoft.Json.Linq;
using Shouldly;
using sensu_client.Helpers;

namespace sensu_client_test
{
    [TestFixture]
    internal class ValidateChecks
    {
        [SetUp]
        public void Init()
        {
        }

        [Test]
        public void should_validate_true_when_valid_values()
        {
            string json = @"{
                    name: 'mymetrics',
                    output: 'name  value',
                    status: 0,
                    type: 'metric',
                    handlers: 'irc'
            }";
            var check = JObject.Parse(json);

            var resultcheck = SensuClientHelper.ValidateCheckResult(check);
            
            resultcheck.ShouldBe(true);
        }

        [Test]
        public void should_validate_false_when_status_is_not_an_integer()
        {
            string json = @"{
                    name: 'mymetrics',
                    output: 'name  value',
                    status: 'a',
                    type: 'metric',
                    handlers: 'irc'
            }";
            var check = JObject.Parse(json);

            var resultcheck = SensuClientHelper.ValidateCheckResult(check);

            resultcheck.ShouldBe(false);
        }

        [Test]
        public void should_validate_false_when_output_is_not_a_string()
        {
            string json = @"{
                    name: 'mymetrics',
                    output: 3,
                    status: 0,
                    type: 'metric',
                    handlers: 'irc'
            }";
            var check = JObject.Parse(json);

            var resultcheck = SensuClientHelper.ValidateCheckResult(check);

            resultcheck.ShouldBe(false);
        }

        [Test]
        public void should_validate_false_when_name_contains_special_characters()
        {
            string json = @"{
                    name: 'mymet$rics',
                    output: 3,
                    status: 0,
                    type: 'metric',
                    handlers: 'irc'
            }";
            var check = JObject.Parse(json);

            var resultcheck = SensuClientHelper.ValidateCheckResult(check);

            resultcheck.ShouldBe(false);
        }
    }
}