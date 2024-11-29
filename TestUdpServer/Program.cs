using TestCommon;

namespace TestUdpServer
{
    internal class Program
    {
        static UdpServerTest mTest = null;
        static void Main(string[] args)
        {
            mTest = new UdpServerTest();
            mTest.Init();

            UpdateMgr.Do2(Update);
        }

        static void Update(double fElapsed)
        {
            mTest.Update(fElapsed);
        }
    }
}
