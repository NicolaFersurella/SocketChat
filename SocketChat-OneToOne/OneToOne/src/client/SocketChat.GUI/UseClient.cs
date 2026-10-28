using SocketChat.Core;

namespace SocketChat.GUI;

public class UseClient
{
    private static void Fatal(string message)
    {
        Console.Error.WriteLine(message);
        Environment.Exit(1);
    }

    public static void Main(string[] args)
    {
        var host = args[0] ?? "localhost";
        var port = int.Parse(args[1] ?? "11000");
        var client = new Client(port, host); 
        client.Connect();

        if (!client.IsConnected)
        {
            Fatal("Could not connect to ${host}:${port}");
        }

        client.StartListening(message => Console.WriteLine("[Server] " + message));

        bool keepGoing = true;
        Console.WriteLine("Type here your messages and press ENTER to send them!");
        while (keepGoing && client.IsConnected)
        {
            var message = Console.ReadLine();
            if (!string.IsNullOrEmpty(message))
            {
                bool sent = client.TrySendMessage(message);
                if (sent)
                {
                    Console.WriteLine("[Client] " + message);
                }
            }
            keepGoing = message != "exit";
        }
    }
}
