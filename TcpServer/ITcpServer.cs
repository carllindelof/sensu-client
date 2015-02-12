namespace sensu_client.TcpServer
{
    public interface ITcpServer
    {
        event tcpServerConnectionChanged OnConnect;
        event tcpServerConnectionChanged OnDataAvailable;
        event tcpServerError OnError;
        int Port { get; set; }
        int MaxSendAttempts { get; set; }
        void Open();
        void Close();
        void Send(string data);
    }
}