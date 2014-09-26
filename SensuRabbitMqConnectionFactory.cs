using System.Linq;
using System.Net.Security;
using NLog;
using RabbitMQ.Client;
using RabbitMQ.Client.Exceptions;
using sensu_client.Configuration;
using System.Security.Cryptography.X509Certificates;

namespace sensu_client
{
    public class SensuRabbitMqConnectionFactory : ISensuRabbitMqConnectionFactory
    {
        private readonly ISensuClientConfigurationReader _sensuClientConfigurationReader;
        private static readonly Logger Log = LogManager.GetCurrentClassLogger();
        private static IConnection _rabbitMqConnection;
        private static readonly object Connectionlock = new object();

        public SensuRabbitMqConnectionFactory(ISensuClientConfigurationReader sensuClientConfigurationReader)
        {
            _sensuClientConfigurationReader = sensuClientConfigurationReader;
        }

        private  ConnectionFactory CreateConnection()
        {
            var connectionFactory = new ConnectionFactory
                {

                    HostName = _sensuClientConfigurationReader.SensuClientConfig.Rabbitmq.Host,
                    Port = _sensuClientConfigurationReader.SensuClientConfig.Rabbitmq.Port,
                    UserName = _sensuClientConfigurationReader.SensuClientConfig.Rabbitmq.User,
                    Password = _sensuClientConfigurationReader.SensuClientConfig.Rabbitmq.Password,
                    VirtualHost = _sensuClientConfigurationReader.SensuClientConfig.Rabbitmq.Vhost,
                    
                };
            
            if (_sensuClientConfigurationReader.SensuClientConfig.Rabbitmq.Ssl != null)
            {
                connectionFactory.Ssl = new SslOption
                    {
                        ServerName = _sensuClientConfigurationReader.SensuClientConfig.Rabbitmq.Host,
                        CertPath = @_sensuClientConfigurationReader.SensuClientConfig.Rabbitmq.Ssl.Private_key_file,
                        CertPassphrase = @_sensuClientConfigurationReader.SensuClientConfig.Rabbitmq.Ssl.Cert_Pass_Phrase,
                        Enabled = true,
                        AcceptablePolicyErrors =
                            SslPolicyErrors.RemoteCertificateNotAvailable |
                            SslPolicyErrors.RemoteCertificateNameMismatch |
                            SslPolicyErrors.RemoteCertificateChainErrors,
                    };
            }
            return connectionFactory;
        }

        public IConnection GetRabbitConnection()
        {
            //One at a time, please
            lock (Connectionlock)
            {
                if (_rabbitMqConnection == null || !_rabbitMqConnection.IsOpen)
                {
                    Log.Debug("No open rMQ connection available. Creating new one.");

                    if (_sensuClientConfigurationReader.SensuClientConfig.Rabbitmq == null)
                    {
                        Log.Error("rabbitmq not configured");
                        return null;
                    }
                    var connectionFactory = CreateConnection();
                    try
                    {
                        _rabbitMqConnection = connectionFactory.CreateConnection();
                    }
                    catch (ConnectFailureException ex)
                    {
                        Log.Error("unable to open rMQ connection", ex);
                        return null;
                    }
                    catch (BrokerUnreachableException ex)
                    {
                        Log.Error("rMQ endpoint unreachable", ex);
                        return null;
                    }
                }
            }
            return _rabbitMqConnection;
        }

    }
}