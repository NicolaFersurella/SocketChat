using SocketChat.Core;

namespace SocketChat.GUI;

public class UseServer
{
    private static void Fatal(string message)
    {
        Console.Error.WriteLine("Could not connect to ${host}:${port}");
        Environment.Exit(1);
    }

    public static void Main(string[] args)
    {
        int port = 0;
        string host = null;

        if (args.Length == 2)
        {
            host = args[0];
            port = int.Parse(args[1]);
        }
        else
        {
            Console.WriteLine("Usage: SocketChat.GUI <host> <port>");
            Environment.Exit(1);
        }

        var server = new Server(port, host);

        try
        {
            Task.Run(() =>
            {
                try
                {
                    server.Start();
                }
                catch (Exception ex)
                {
                    Console.Error.WriteLine(ex.Message);
                }
            });

            Console.WriteLine("Server started. Waiting for clients...");
            Console.WriteLine("Type a message and press ENTER to send it to all connected clients.");

            while (true)
            {
                var message = Console.ReadLine();
                if (string.IsNullOrWhiteSpace(message))
                {
                    continue;
                }

                if (string.Equals(message, "exit", StringComparison.OrdinalIgnoreCase))
                {
                    break;
                }

                bool sent = server.TrySendMessage(message);
                if (sent)
                {
                    Console.WriteLine("[Server] " + message);
                }
                else
                {
                    Console.WriteLine("No clients connected.");
                }
            }

            server.Close();
        }
        catch (Exception ex)
        {
            Fatal(ex.Message);
        }
    }
}
