using TestCommon;

namespace TestOtherUdpServer
{
    internal class Program
    {
        static NetHandler mTest = null;
        static void Main(string[] args)
        {
            mTest = new NetHandler();
            mTest.Do();
            UpdateMgr.Do(Update);
        }

        static void Update(double fElapsed)
        {
            mTest.Update();
        }
    }
}
