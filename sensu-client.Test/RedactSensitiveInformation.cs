using System.Collections.Generic;
using NUnit.Framework;
using Newtonsoft.Json.Linq;
using sensu_client.Helpers;
using Shouldly;
namespace sensu_client_test
{
    [TestFixture]
    internal class RedactSensitiveInformation
    {
        [SetUp]
        public void Init()
        {
        }

        [Test]
        public void should_redact_sensitive_information()
        {
            string jsonparams = @"{
                check_cpu_windows: {
                ram: {
                    secret: 'hemliga arne',
                    password: 'mumbojumbo'
                }
              }
            }";
            var check = JObject.Parse(jsonparams);

            var resultcheck = (JObject)SensuClientHelper.RedactSensitiveInformaton(check);

            resultcheck["check_cpu_windows"]["ram"]["secret"].Value<string>().ShouldBe("REDACTED");
            resultcheck["check_cpu_windows"]["ram"]["password"].Value<string>().ShouldBe("REDACTED");

        }

        [Test]
        public void should_not_redact_nonsensitive_information()
        {
            string jsonparams = @"{
                check_cpu_windows: {
                ram: {
                    warning: 'hemliga arne',
                    critical: 'mumbojumbo'
                }
              }
            }";
            var check = JObject.Parse(jsonparams);

            var resultcheck = (JObject)SensuClientHelper.RedactSensitiveInformaton(check);

            resultcheck["check_cpu_windows"]["ram"]["warning"].Value<string>().ShouldBe("hemliga arne");
            resultcheck["check_cpu_windows"]["ram"]["critical"].Value<string>().ShouldBe("mumbojumbo");
        }

        [Test]
        public void should_be_able_to_parse_redact_string_to_list()
        {
            string jsonparams = @"{
                client: { 
                redact: 'password passwd pass api_key secret'
              }
            }";
            var check = JObject.Parse(jsonparams);
            List<string> redactlist = null;

            redactlist = SensuClientHelper.GetRedactlist(check);
            redactlist.ShouldContain("password");
        }

        [Test]
        public void should_return_empty_redactlist_when_client_has_no_redact_key()
        {
            string jsonparams = @"{
                client: { 
              }
            }";
            var check = JObject.Parse(jsonparams);
            List<string> redactlist = null;

            redactlist = SensuClientHelper.GetRedactlist(check);
            Assert.That(redactlist, Is.Null);
            
        }



        [Test]
        public void should_not_brake_when_empty_client_redact_string()
        {
            string jsonparams = @"{
                client: { 
                redact: ''
              }
            }";
            var check = JObject.Parse(jsonparams);
            List<string> redactlist = null;

            redactlist = SensuClientHelper.GetRedactlist(check);
            Assert.That(redactlist, Is.Null);
        }

    }
}