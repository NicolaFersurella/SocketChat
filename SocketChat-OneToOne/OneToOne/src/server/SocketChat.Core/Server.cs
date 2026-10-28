namespace SocketChat.Core;

using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

public class Server
{
    private const string DEFAULT_HOST_NAME = "localhost";
    private const int BUFFER_SIZE = 1024;
    private const int MAX_CONNECTIONS = 10;
    private Socket listener;
    private List<Socket> clientHandlers = new List<Socket>();
    private byte[] buffer;
    public int Port { get; init; }
    public string HostName { get; init; }
    // clientsLock è un oggetto utilizzato per sincronizzare l'accesso alla lista dei client connessi (clientHandlers). 
    // Con il lock evito che più thread possano modificare la lista contemporaneamente, prevenendo condizioni di gara e garantendo la coerenza dei dati.
    private Object clientsLock = new Object();

    public Server(int port, string hostName)
    {
        this.Port = port;
        this.HostName = hostName ?? DEFAULT_HOST_NAME;
        ClearBuffer();
    }
    // Il metodo ClearBuffer() inizializza il buffer utilizzato per ricevere i messaggi dai client.
    private void ClearBuffer()
    {
        this.buffer = new byte[BUFFER_SIZE];
    }
    // Il metodo Start() avvia il server, crea un socket di ascolto e accetta le connessioni dei client. 
    // Per ogni client connesso, viene avviato un task separato per gestire la comunicazione con quel client.
    public void Start()
    {
        var hostIP = Dns.GetHostEntry(HostName).AddressList[0];
        var localEndpoint = new IPEndPoint(hostIP, Port);

        listener = new Socket(hostIP.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
        listener.Bind(localEndpoint);
        listener.Listen(MAX_CONNECTIONS);

        while (true)
        {
            // Blocca finché un client non si connette
            var acceptedClient = listener.Accept();
            lock (clientsLock)
            {
                clientHandlers.Add(acceptedClient);
            }
            Task.Run(() => ListenToClient(acceptedClient));
        }
    }
    // Il metodo ListenToClient(Socket client) gestisce la comunicazione con un singolo client. 
    // Riceve i messaggi inviati dal client e li stampa sulla console. 
    // Se il client si disconnette o si verifica un errore, il client viene rimosso dalla lista dei client connessi.
    private void ListenToClient(Socket client)
    {
        ClearBuffer();

        while (client.Connected)
        {
            try
            {
                int receivedBytes = client.Receive(this.buffer);
                if (receivedBytes == 0)
                {
                    // Il client si è disconnesso
                    break;
                }

                string decodedMessage = Encoding.UTF8.GetString(buffer, 0, receivedBytes);
                Console.WriteLine($"Received from client: {decodedMessage}");
            }
            catch (SocketException)
            {
                // Connessione interrotta o socket chiuso
                break;
            }
        }

        lock (clientsLock)
        {
            clientHandlers.Remove(client);
        }
        client.Close();
    }
    // Il metodo TrySendMessage(string msg) invia un messaggio a tutti i client connessi. 
    // Se l'invio a un client fallisce, il metodo restituisce false, altrimenti true.
    public bool TrySendMessage(string msg)
    {
        lock (clientsLock)
        {
            foreach (var client in clientHandlers)
            {
                if (client.Connected)
                {
                    byte[] raw = Encoding.UTF8.GetBytes(msg);
                    int sent = client.Send(raw);
                    if (sent != raw.Length)
                    {
                        return false;
                    }
                }
            }
        }
        return true;
    }
    // Il metodo StartListening(Action<string> onMessageReceived) avvia un task in background che ascolta i messaggi inviati dai client e chiama la callback onMessageReceived 
    // per ogni messaggio ricevuto.
    public void StartListening(Action<string> onMessageReceived)
    {
        Task.Run(() =>
        {
            ClearBuffer();

            while (true)
            {
                lock (clientsLock)
                {
                    foreach (var client in clientHandlers)
                    {
                        if (client.Connected)
                        {
                            try
                            {
                                int receivedBytes = client.Receive(this.buffer);
                                if (receivedBytes > 0)
                                {
                                    string decodedMessage = Encoding.UTF8.GetString(buffer, 0, receivedBytes);
                                    onMessageReceived(decodedMessage);
                                }
                            }
                            catch (SocketException)
                            {
                                // Connessione interrotta o socket chiuso
                                continue;
                            }
                        }
                    }
                }
            }
        });
    }
    // Il metodo Close() chiude tutte le connessioni con i client e il socket di ascolto del server.
    public void Close()
    {
        try
        {
            lock (clientsLock)
            {
                foreach (var client in clientHandlers)
                {
                    if (client.Connected)
                    {
                        client.Shutdown(SocketShutdown.Both);
                        client.Close();
                    }
                }
                clientHandlers.Clear();
            }
        }
        catch { }

        listener?.Close();
    }
}