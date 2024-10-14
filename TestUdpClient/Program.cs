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
            UpdateMgr.Do(Update);

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
