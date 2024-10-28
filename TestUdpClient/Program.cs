using XKNet.Common;

namespace TestUdpClient
{
    internal class Program
    {
        static UdpClientTest mTest = null;
        static void Main(string[] args)
        {
            mTest = new UdpClientTest();
            mTest.Init();
            UpdateMgr.Do(Update, 100);
        }

        static void Update(double fElapsed)
        {
            mTest.Update(fElapsed);
        }
    }
}
