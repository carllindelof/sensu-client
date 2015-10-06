using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using NLog;

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
        void Send(Byte[] dgram, IPEndPoint endPoint);
    }


    public class UdpReceiver : IUdpReceiver
    {
        private IPEndPoint _remoteEndPoint;
        private UdpClient _udpListener;
        
        private Thread _worker;
        
        private static readonly Logger Log = LogManager.GetCurrentClassLogger();

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
           
            // We need a working thread
            _worker = new Thread(Start) {IsBackground = true};
            _worker.Start();
        }

        public void Terminate()
        {
            if (_udpListener != null)
            {
                _udpListener.Close();
                _udpListener = null;

                _remoteEndPoint = null;
            }
       
            if ((_worker != null) && _worker.IsAlive)
                _worker.Abort();
            _worker = null;
        }

        public void Send(byte[] dgram, IPEndPoint endPoint)
        {
             _udpListener.BeginSend(dgram, dgram.Length, endPoint, SendCallback, _udpListener);
        }

        private void Start()
        {
            
              // Init connection here, before starting the thread, to know the status now
            _remoteEndPoint = new IPEndPoint(IPAddress.Any, 0);
            
            Log.Info("Remote endpoint created {0}", _remoteEndPoint.Address.ToString());

            _udpListener = new UdpClient(Port);

            Log.Debug("UdpListener {0}", _udpListener.Client.LocalEndPoint.ToString()); 
            
            Log.Debug("Before listening to server");
            
            StartListening();

        }
        public static void SendCallback(IAsyncResult ar)
        {
            var u = (UdpClient)ar.AsyncState;
            u.EndSend(ar);
        }
        private void StartListening()
        {
            while ((_udpListener != null) && (_remoteEndPoint != null))
            {
                try
                {
                    if (_udpListener.Available > 0) // Only read if we have some data 
                    {
                        _udpListener.BeginReceive(CallBackDataReceived, _udpListener);
                    }
                }
                catch (Exception ex)
                {
                    Log.Error(ex, "Error when receiving data from udp");
                }
                //Add Sleep to reduce CPU usage;
                Thread.Sleep(1);
            }
        }

        private void CallBackDataReceived(IAsyncResult res)
        {
            var client = (UdpClient)res.AsyncState;
            var received = client.EndReceive(res, ref _remoteEndPoint);
            var data = Encoding.ASCII.GetString(received);
            
            if (OnDataReceived != null)
            {
                OnDataReceived(data, _remoteEndPoint);
            }
        }
    }


}