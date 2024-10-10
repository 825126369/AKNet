using XKNet.Common;

namespace TestTcpServer
{
    internal class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("Hello, World!");
            NetHandler mNet = new NetHandler();
            mNet.Init();

            UpdateMgr.Do(Update);
        }

        static void Update(double fElapsed)
        {

        }
    }
}
