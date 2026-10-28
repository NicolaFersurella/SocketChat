namespace SocketChat.Core;

using System.Net;
using System.Net.Sockets;
using System.Text;

public class Client
{
    private const string DEFAULT_HOST_NAME = "localhost";
    private const int MAX_RECONNECTION_ATTEMPTS = 10;
    private readonly int BUFFER_SIZE = 1024;
    private Socket communicationChannel;
    public int Port { get; init; }
    public string HostName { get; init; }
    public bool IsConnected => communicationChannel is not null && communicationChannel.Connected;
    private byte[] buffer;
    
    public Client(int port, string hostName)
    {
        this.Port = port;
        this.HostName = hostName ?? DEFAULT_HOST_NAME;
        ClearBuffer();
    }

    private void ClearBuffer()
    {
        this.buffer = new byte[BUFFER_SIZE];
    }

    public void Connect()
    {
        var hostIP = Dns
            .GetHostEntry(HostName)
            .AddressList
            .First();
        var remoteEndpoint = new IPEndPoint(hostIP, Port);
        
        this.communicationChannel = new Socket(
            hostIP.AddressFamily, 
            SocketType.Stream, 
            ProtocolType.Tcp
        );

        for (int attempts = 0; attempts < MAX_RECONNECTION_ATTEMPTS && !IsConnected; attempts++)
        {
            try
            {
                communicationChannel.Connect(remoteEndpoint);
            } catch (Exception)
            {
                Thread.Sleep(500);
            }
        }
    }

    public bool TrySendMessage(string msg)
    {
        if (!IsConnected)
        {
            return false;
        }

        byte[] raw = Encoding.UTF8.GetBytes(msg);
        int sent = communicationChannel.Send(raw);
        return sent == raw.Length;
    }

    public void StartListening(Action<string> onMessageReceived)
    {
        Task.Run(() =>
        {
            ClearBuffer();

            while (IsConnected)
            {
                int receivedBytes = this.communicationChannel.Receive(this.buffer);
                if (receivedBytes == 0)
                {
                    // Server just disconnected
                    return;
                }
                string decodedMessage = Encoding.UTF8.GetString(buffer, 0, receivedBytes);
                onMessageReceived(decodedMessage);
            }
        });
    }

    public void Close()
    {
        communicationChannel.Shutdown(SocketShutdown.Both);
        communicationChannel.Close();
    }
}
