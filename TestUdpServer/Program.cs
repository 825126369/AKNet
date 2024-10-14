using XKNet.Common;

namespace TestUdpServer
{
    internal class Program
    {
        static UdpServerTest mTest = null;
        static void Main(string[] args)
        {
            mTest = new UdpServerTest();
            mTest.Init();

            try
            {
                UpdateMgr.Do(Update);
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }

            while (true) { };
        }

        static void Update(double fElapsed)
        {
            mTest.Update(fElapsed);
        }
    }
}
