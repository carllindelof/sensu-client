using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;

namespace sensu_client.UdpReceiver
{
    public delegate void UdpClientDataReceived(string data, IPEndPoint remoteEndpoint);
    public interface IUdpReceiver
    {
        event UdpClientDataReceived OnDataReceived;
        int Port { get; set; }
        string Address { get; set; }
        void Initialize();
        void Terminate();
        int Send(Byte[] dgram, IPEndPoint endPoint);
    }


    public class UdpReceiver : IUdpReceiver
    {
        private IPEndPoint _remoteEndPoint;
        private UdpClient _udpClient;
        private Thread _worker;

        public event UdpClientDataReceived OnDataReceived = null;
        public UdpReceiver()
        {
            Address = String.Empty;
            Port = 3030;
        }

        public int Port { get; set; }
        public string Address { get; set; }

        public void Initialize()
        {
            if ((_worker != null) && _worker.IsAlive)
                return;
            
            // Init connexion here, before starting the thread, to know the status now
            _remoteEndPoint = new IPEndPoint(IPAddress.Any, Port);
            _udpClient = new UdpClient(Port);
            
            // We need a working thread
            _worker = new Thread(Start);
            _worker.IsBackground = true;
            _worker.Start();
        }

        public void Terminate()
        {
            if (_udpClient != null)
            {
                _udpClient.Close();
                _udpClient = null;

                _remoteEndPoint = null;
            }

            if ((_worker != null) && _worker.IsAlive)
                _worker.Abort();
            _worker = null;
        }

        public int Send(byte[] dgram, IPEndPoint endPoint)
        {
           return _udpClient.Send(dgram, dgram.Length, endPoint);
        }

        private void Start()
        {
           
            while ((_udpClient != null) && (_remoteEndPoint != null))
            {

                try
                {

                    byte[] buffer = _udpClient.Receive(ref _remoteEndPoint);
                    string data = Encoding.ASCII.GetString(buffer);
                    if (OnDataReceived != null)
                    {
                        OnDataReceived(data, _remoteEndPoint);
                    }
                }
                catch (Exception ex)
                {
                    return;
                }
            }
        }

    }
}