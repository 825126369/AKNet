using LoginServer;

namespace TestTcpServer
{
    internal class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("Hello, World!");
            NetHandler mNet = new NetHandler();
            mNet.Init();
        }
    }
}
