using AKNet.Common;
using TestCommon;

namespace TestNetServer
{
    internal class Program
    {
        static NetHandler mTest = null;
        static void Main(string[] args)
        {
            NetLog.AddConsoleLog();
            mTest = new NetHandler();
            mTest.Init();
            UpdateMgr.Do(Update);
        }

        static void Update(double fElapsed)
        {
            mTest.Update(fElapsed);
        }
    }
}
