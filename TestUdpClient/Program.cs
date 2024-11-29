using TestCommon;

namespace TestUdpClient
{
    internal class Program
    {
        static UdpClientTest mTest = null;
        static void Main(string[] args)
        {
            mTest = new UdpClientTest();
            mTest.Init();
            UpdateMgr.Do2(Update);
        }

        static void Update(double fElapsed)
        {
            if (fElapsed >= 0.3)
            {
                Console.WriteLine("TestUdpClient 帧 时间 太长: " + fElapsed);
            }

            mTest.Update(fElapsed);
        }
    }
}
