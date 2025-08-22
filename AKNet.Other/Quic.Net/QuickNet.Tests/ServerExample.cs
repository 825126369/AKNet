using System.Text;

namespace AKNet.QuicNet.Common
{
    class ServerExample
    {
        /// <summary>
        /// Fired when Client is connected
        /// </summary>
        /// <param name="connection">The new connection</param>
        static void ClientConnected(QuicConnection connection)
        {
            Console.WriteLine("Client Connected");
            connection.OnStreamOpened += StreamOpened;
        }
            
        static void StreamOpened(QuicStream stream)
        {
            Console.WriteLine("Stream Opened");
            stream.OnStreamDataReceived += StreamDataReceived;
        }

        static void StreamDataReceived(QuicStream stream, byte[] data)
        {
            Console.WriteLine("Stream Data Received: ");
            string decoded = Encoding.UTF8.GetString(data);
            Console.WriteLine(decoded);

            stream.Send(Encoding.UTF8.GetBytes("Ping back from server."));
        }

        static void Example()
        {
            QuicListener listener = new QuicListener(11000);
            listener.OnClientConnected += ClientConnected;

            listener.Start();

            Console.ReadKey();
        }

        static void Main_Delete(string[] args)
        {
            Example();
            return;
        }
    }
}
