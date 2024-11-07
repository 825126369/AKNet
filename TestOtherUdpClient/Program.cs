using TestCommon;

namespace OtherUdpTest
{
    internal class Program
    {
        static UdpClientTest mTest = null;
        static void Main(string[] args)
        {
            mTest = new UdpClientTest();
            mTest.Init();
            UpdateMgr.Do(Update);
        }

        static void Update(double fElapsed)
        {
            mTest.Update(fElapsed);
        }
    }
}
