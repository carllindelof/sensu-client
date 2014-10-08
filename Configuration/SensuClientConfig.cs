using System.Collections.Generic;
using Newtonsoft.Json;

namespace sensu_client.Configuration
{
    public class SensuClientConfig : ISensuClientConfig
    {
        public Client Client { get; set; }
        public Rabbitmq Rabbitmq { get; set; }
        public Checks Checks { get; set; } 
    }

    public class Client
    {
        private string _plugins = @"c:\opt\sensu\plugins";
        public string Name { get; set; }
        public string Address { get; set; }
        public string[] Subscriptions { get; set; }
        public bool SafeMode { get; set; }
        public string Plugins
        {
            get { return _plugins; }
            set { _plugins = value; }
        }
    }

    public class Rabbitmq
    {
        public Ssl Ssl { get; set; }
        public string Host { get; set; }
        public int Port { get; set; }
        public string Vhost { get; set; }
        public string User { get; set; }
        public string Password { get; set; }
    }

    public class Ssl
    {
        public string Private_key_file { get; set; }
        public string Cert_chain_file { get; set; }
        public string Cert_Pass_Phrase { get; set; }
    }


    public class Check : ICheck
    {
        public string Name { get; set; }
        public int Issued { get; set; }
        public string Output { get; set; }
        public int Status { get; set; }
        public string Notification { get; set; }
        public string[] Handlers { get; set; }
        public string Command { get; set; }
        public int Interval { get; set; }
        public string Type { get; set; }
        public string[] Subscribers { get; set; }
        public string[] History { get; set; }
        public bool Flapping { get; set; }
        public bool Standalone { get; set; }
    }

    public interface ICheck
    {
        string Name { get; set; }
        string[] Handlers { get; set; }
        string Command { get; set; }
        int Interval { get; set; }
        string[] Subscribers { get; set; }
        bool Standalone { get; set; }
    }


    [JsonConverter(typeof(ChecksConverter))]
    public class Checks : List<Check>
    {

    }
}
