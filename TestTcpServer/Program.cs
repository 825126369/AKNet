using AKNet.Common;

namespace TestTcpServer
{
    internal class Program
    {
        static NetHandler mNet = null;
        static void Main(string[] args)
        {
            Console.WriteLine("Hello, World!");
            mNet = new NetHandler();
            mNet.Init();

            AKNetUpdateMgr.Do(Update);
        }

        static void Update(double fElapsed)
        {
            mNet.Update(fElapsed);
        }
    }
}
