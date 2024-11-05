using TestCommon;

namespace TestTcpClient
{
    internal class Program
    {
        static TcpClientTest mTest;
        static void Main(string[] args)
        {
            mTest = new TcpClientTest();
            mTest.Init();
            UpdateMgr.Do(Update);
        }

        static void Update(double fElapsed)
        {
            mTest.Update(fElapsed);
        }
    }
}
