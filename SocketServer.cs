using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using NLog;
using Newtonsoft.Json.Linq;
using sensu_client.Helpers;
using sensu_client.TcpServer;
using sensu_client.UdpReceiver;

namespace sensu_client
{
    public class SocketServer : ISocketServer
    {
        private static int _port = 3030;
        private static ITcpServer _tcpServer;
        private static IUdpReceiver _udpReceiver;
        private static ICheckProcessor _checkProcessor;
        private static readonly Logger Log = LogManager.GetCurrentClassLogger();

        public SocketServer(ITcpServer tcpServer,IUdpReceiver udpReceiver, ICheckProcessor checkProcessor)
        {
            _tcpServer = tcpServer;
            _udpReceiver = udpReceiver;
            _checkProcessor = checkProcessor;
        }

        public  void Open()
        {
            try
            {
                _tcpServer.Port = _port;
                _tcpServer.Open();
                _tcpServer.OnDataAvailable += tcpServer_OnDataAvailable;

                _udpReceiver.Port = _port;
                _udpReceiver.OnDataReceived += UdpReceiverOnOnDataReceived;
                _udpReceiver.Initialize();
            }
            catch (Exception ex)
            {
                Log.Error("Error opening connection",ex);
            }

        }
        public  void Close()
        {
            _tcpServer.Close();
            _udpReceiver.Terminate();
        }
   
        public static  void UdpReceiverOnOnDataReceived(string data, IPEndPoint remoteEndpoint)
        {
            if (data == null) return;

            string reply = ParseCheckResult(data);
            _udpReceiver.Send(Encoding.ASCII.GetBytes(reply), remoteEndpoint);

        }

        public static void tcpServer_OnDataAvailable(TcpServerConnection connection)
        {
            var data = readStream(connection.Socket);

            if (data == null) return;
            
            string reply = ParseCheckResult(data);

            connection.sendData(reply);
        }

        private static string ParseCheckResult(string data)
        {
            if (data == "ping") return "pong";

            JObject check;
            if (!SensuClientHelper.TryParseData(data, out check)) return "Invalid Json!";
            if (!SensuClientHelper.ValidateCheckResult(check)) return "Invalid check format!";
            _checkProcessor.PublishCheckResult(check);

            return "ok";
        }


        protected static string readStream(TcpClient client)
        {
            var returndata = string.Empty;

            var stream = client.GetStream();
            

                if (stream.CanRead)
                {
                    var bytes = new byte[client.ReceiveBufferSize];
                    stream.Read(bytes, 0, (int)client.ReceiveBufferSize);
                    returndata = Encoding.ASCII.GetString(bytes).Trim('\0');
                }
            
            return returndata;
        }

        static byte[] GetBytes(string str)
        {
            byte[] bytes = new byte[str.Length * sizeof(char)];
            System.Buffer.BlockCopy(str.ToCharArray(), 0, bytes, 0, bytes.Length);
            return bytes;
        }
    }
}