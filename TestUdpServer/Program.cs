using AKNet.Common;

namespace TestUdpServer
{
    internal class Program
    {
        static UdpServerTest mTest = null;
        static void Main(string[] args)
        {
            mTest = new UdpServerTest();
            mTest.Init();

            AKNetUpdateMgr.Do(Update);
        }

        static void Update(double fElapsed)
        {
            mTest.Update(fElapsed);
        }
    }
}
