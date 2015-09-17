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
        private string _plugins = @"c:\";
        private bool _send_metric_with_check = false;

        [JsonProperty("name")]
        public string Name { get; set; }
        [JsonProperty("address")]
        public string Address { get; set; }
        [JsonProperty("subscriptions")]
        public string[] Subscriptions { get; set; }
        [JsonProperty("safemode")]
        public bool SafeMode { get; set; }
        [JsonProperty("plugins")]
        public string Plugins
        {
            get { return _plugins; }
            set { _plugins = value; }
        }
        [JsonProperty("send_metric_with_check")]
        public bool SendMetricWithCheck
        {
            get { return _send_metric_with_check; }
            set { _send_metric_with_check = value; }
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
        [JsonProperty("name")]
        public string Name { get; set; }
        [JsonProperty("issued")]
        public int Issued { get; set; }
        [JsonProperty("output")]
        public string Output { get; set; }
        [JsonProperty("status")]
        public int Status { get; set; }
        [JsonProperty("notification")]
        public string Notification { get; set; }
        [JsonProperty("handlers")]
        public string[] Handlers { get; set; }
        [JsonProperty("command")]
        public string Command { get; set; }
        [JsonProperty("interval")]
        public int Interval { get; set; }
        [JsonProperty("type")]
        public string Type { get; set; }
        [JsonProperty("subscribers")]
        public string[] Subscribers { get; set; }
        [JsonProperty("history")]
        public string[] History { get; set; }
        [JsonProperty("flapping")]
        public bool Flapping { get; set; }
        [JsonProperty("standalone")]
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
