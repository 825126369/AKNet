using TestCommon;

namespace TestUdpClient
{
    internal class Program
    {
        static NetHandler mTest = null;
        static void Main(string[] args)
        {
            mTest = new NetHandler();
            mTest.Init();
            UpdateMgr.Do(Update);
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
