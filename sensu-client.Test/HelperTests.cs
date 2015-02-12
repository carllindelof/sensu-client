using NUnit.Framework;
using Shouldly;
using sensu_client.Helpers;

namespace sensu_client_test
{  
    [TestFixture]
    internal class HelperTests
    {
        [Test]
        public void current_version_should_be_1_0()
        {
            CoreAssembly.Version.Major.ToString().ShouldBe("1");
        }

        [Test]
        public void create_queuename_should_return_a_nonempty_string()
        {
            SensuClientHelper.CreateQueueName().ShouldNotBeEmpty();
        }

        [Test]
        public void create_queuename_should_contain_hostname()
        {
            SensuClientHelper.CreateQueueName().ShouldContain(SensuClientHelper.GetFQDN());
        }


        [Test]
        public void create_queuename_should_contain_assembly_version()
        {
            SensuClientHelper.CreateQueueName().ShouldContain(CoreAssembly.Version.ToString());
        }
    }
}
