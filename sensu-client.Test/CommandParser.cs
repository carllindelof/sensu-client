using NUnit.Framework;
using Shouldly;
using sensu_client.Command;

namespace sensu_client_test
{
    [TestFixture]
    class CommandParser
    {

        private static CommandConfiguration _configuration;

        [SetUp]
        public void Init()
        {
            _configuration = new CommandConfiguration { Plugins = @"c:\etc\plugins" };
        }
        
        
        [Test]
        public void parser_can_parse_command_with_arguments()
        {
            var command = CommandFactory.Create(_configuration, "script.ps1 -w 50 -c 90");
            command.ShouldBeOfType(typeof(PowerShellCommand));
            command.Arguments.ShouldBe(@"-NoProfile -NonInteractive -NoLogo -ExecutionPolicy Bypass  -FILE c:\etc\plugins\script.ps1 -w 50 -c 90", Case.Insensitive);
        }

        [Test]
        public void parser_can_parse_command_with_only_command()
        {
            var command = CommandFactory.Create(_configuration, "script.ps1");
            command.ShouldBeOfType(typeof(PowerShellCommand));
            command.FileName.ToLower().ShouldContain("powershell.exe");
            command.FileName.ShouldBe(@"C:\Windows\system32\WindowsPowershell\v1.0\powershell.exe",Case.Insensitive);
            command.Arguments.ShouldBe(@"-NoProfile -NonInteractive -NoLogo -ExecutionPolicy Bypass  -FILE c:\etc\plugins\script.ps1", Case.Insensitive);
        }

        [Test]
        public void parser_can_parse_command_with_powershell_hints_and_plugins_path()
        {
            var command = CommandFactory.Create(_configuration, "powershell -file c:/opt/sensu/plugins/perfmon-metrics.ps1");
            command.ShouldBeOfType(typeof(PowerShellCommand));
            command.FileName.ToLower().ShouldContain("powershell.exe");
            command.Arguments.ShouldBe(@"-NoProfile -NonInteractive -NoLogo -ExecutionPolicy Bypass  -FILE c:\etc\plugins\perfmon-metrics.ps1", Case.Insensitive);
            command.FileName.ShouldBe(@"C:\Windows\system32\WindowsPowershell\v1.0\powershell.exe", Case.Insensitive);
        }

        [Test]
        public void parser_can_parse_simple_ruby_command()
        {
            var command = CommandFactory.Create(_configuration, "check_cpu.rb -w 50 -c 90");
            command.ShouldBeOfType(typeof(RubyCommand));
            command.FileName.ToLower().ShouldContain("ruby.exe");
            command.Arguments.ShouldBe(@"c:\etc\plugins\check_cpu.rb -w 50 -c 90", Case.Insensitive);
        }

        [Test]
        public void parser_can_parse_simple_ruby_command_with_path_to_ruby_and_plugins()
        {
            var command = CommandFactory.Create(_configuration, "/opt/sensu/embedded/bin/ruby.exe /opt/sensu/plugins/check-windows-cpu-load.rb");
            command.ShouldBeOfType(typeof(RubyCommand));
            command.FileName.ToLower().ShouldContain("ruby.exe");
            command.Arguments.ShouldBe(@"c:\etc\plugins\check-windows-cpu-load.rb", Case.Insensitive);
        }
    
      
    }
}
