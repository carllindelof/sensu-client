using System;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using NLog;

namespace sensu_client.Command
{

    public static class CommandProviders
    {
        public static string  PowerShell = "powershell";
        public static string Ruby = "ruby";
        public static string Cmd = "cmd";

    }
   
    public struct CommandResult 
{
        public string Output { get; set; }
        public int Status { get; set; }
        public string Duration { get; set; }
}

    public static class CommandFactory
    {

        public static Command Create(CommandConfiguration commandConfiguration, string command)
        {
            var command_lower = command.ToLower();
            if (command_lower.Contains(".ps1")) return new PowerShellCommand(commandConfiguration, command);
            if (command_lower.Contains(".rb")) return new RubyCommand(commandConfiguration, command);

            return new ShellCommand(commandConfiguration, command);
        }
    }

    public abstract class Command
    {
        private static readonly Logger Log = LogManager.GetCurrentClassLogger();
        protected readonly CommandConfiguration _commandConfiguration;
        protected readonly string _unparsedCommand;
        private string _arguments;

        protected Command(CommandConfiguration commandConfiguration, string unparsedCommand)
        {
            _commandConfiguration = commandConfiguration;
            _unparsedCommand = unparsedCommand;
        }

        public abstract string FileName { get; protected internal set; }

        public virtual string Arguments
        {
            get
            {
                if (!String.IsNullOrEmpty(_arguments)) return _arguments;

                _arguments = ParseArguments();
                return _arguments;
            }
            protected internal set { _arguments = value; }
        }

        protected abstract string ParseArguments();

        public virtual CommandResult Execute()
        {
            var result = new CommandResult();
            var processstartinfo = new ProcessStartInfo()
                {
                    FileName = FileName,
                    Arguments = Arguments,
                    WindowStyle = ProcessWindowStyle.Hidden,
                    WorkingDirectory = _commandConfiguration.Plugins,
                    UseShellExecute = false,
                    RedirectStandardError = true,
                    RedirectStandardInput = true,
                    RedirectStandardOutput = true
                };
            var process = new Process {StartInfo = processstartinfo};
            var stopwatch = new Stopwatch();
            try
            {
                stopwatch.Start();
                process.Start();
                var output = process.StandardOutput.ReadToEnd();
                var errors = process.StandardError.ReadToEnd();
                var status = process.ExitCode;
                if (_commandConfiguration.TimeOut.HasValue)
                {
                    if (!process.WaitForExit(1000*_commandConfiguration.TimeOut.Value))
                    {
                        process.Kill();
                    }
                }
                else
                {
                    process.WaitForExit();
                    process.Close();
                }

                result.Output = String.Format("{0}{1}", output,errors);
                result.Status = status;
                if (!string.IsNullOrEmpty(errors)) Log.Error("Error when executing command: {0} \n resulted in: {1} \n", Arguments,errors);
            }
            catch (Win32Exception ex)
            {
                result.Output = String.Format("Unexpected error: {0}", ex.Message);
                result.Status = 2;
            }
            stopwatch.Stop();
            result.Duration = String.Format("{0:f3}", ((float) stopwatch.ElapsedMilliseconds)/1000);
            return result;

        }
    }

    public class PowerShellCommand : Command
        {
            private string _fileName;
            private string _arguments;
            const string PowershellOptions = "-NoProfile -NonInteractive -NoLogo -ExecutionPolicy Bypass ";
            
            public PowerShellCommand(CommandConfiguration commandConfiguration, string unparsedCommand): base(commandConfiguration,unparsedCommand)
            {
            }

            public override string FileName
            {
                get
                {
                    if (!String.IsNullOrEmpty(_fileName)) return _fileName;

                    _fileName = GetPowerShellExePath();
                    return _fileName;
                }
                protected internal set { _fileName = value; }
            }

        private static string GetPowerShellExePath()
        {
            var systemRoot = Environment.ExpandEnvironmentVariables("%systemroot%").ToLower();
            if (File.Exists(string.Format("{0}\\sysnative\\WindowsPowershell\\v1.0\\powershell.exe", systemRoot)))
            {
                return string.Format("{0}\\sysnative\\WindowsPowershell\\v1.0\\powershell.exe", systemRoot);
            }
            if (File.Exists(string.Format("{0}\\system32\\WindowsPowershell\\v1.0\\powershell.exe", systemRoot)))
            {
                return string.Format("{0}\\system32\\WindowsPowershell\\v1.0\\powershell.exe", systemRoot);
            }
            return "powershell.exe";
        }

        public override string Arguments
            {
                get { 
                    if (!String.IsNullOrEmpty(_arguments)) return _arguments;

                        _arguments = ParseArguments();
                return _arguments;
                }
                protected internal set { _arguments = value; }
            }

         protected override string ParseArguments()
        {
            int lastSlash = _unparsedCommand.LastIndexOf('/');
            var powershellargument = (lastSlash > -1) ? _unparsedCommand.Substring(lastSlash + 1) : _unparsedCommand;
            return String.Format("{0} -FILE {1}\\{2}", PowershellOptions, _commandConfiguration.Plugins, powershellargument);
        }
     }

        public class RubyCommand : Command
        {
            //string envRubyPath = Environment.GetEnvironmentVariable("RUBYPATH");
            private string _fileName;
            private string _arguments;

            public RubyCommand(CommandConfiguration commandConfiguration, string unparsedCommand)
                : base(commandConfiguration, unparsedCommand)
            {
            }

   
            public override string FileName
            {
                get
                {
                    if (!String.IsNullOrEmpty(_fileName)) return _fileName;

                    _fileName = RubyExePath();
                    return _fileName;
                }
                protected internal set { _fileName = value; }
            }

            private static string RubyExePath()
            {
                var defaultSensuClientPath = @"c:\opt\sensu\embedded\bin";
                var rubyPath = Path.Combine(defaultSensuClientPath, "ruby.exe");
                if (File.Exists(rubyPath))
                {
                    return rubyPath;
                }

                return "ruby.exe";
            }

            public override string Arguments
            {
                get { 
                    if (!String.IsNullOrEmpty(_arguments)) return _arguments;

                        _arguments = ParseArguments();
                         return _arguments;
                }
                protected internal set { _arguments = value; }
            }


            protected override string ParseArguments()
            {
                int lastSlash = _unparsedCommand.LastIndexOf('/');
                var rubyArgument = (lastSlash > -1) ? _unparsedCommand.Substring(lastSlash + 1) : _unparsedCommand;
                return String.Format("{0}\\{1}", _commandConfiguration.Plugins, rubyArgument);
            }
        }
        public class ShellCommand : Command
        {
            private string _fileName = String.Format("{0}\\cmd.exe",Environment.SystemDirectory);
           
            public ShellCommand(CommandConfiguration commandConfiguration, string unparsedCommand): base(commandConfiguration, unparsedCommand)
            {
            }
     
            public override string FileName
            {
                get { return _fileName; }
                protected internal set { _fileName = value; }
            }

            protected override string ParseArguments()
            {
                return String.Format("'{0}'", _unparsedCommand);
            }
        }

}