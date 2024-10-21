using XKNet.Common;

namespace TestTcpClient
{
    internal class Program
    {
        static List<NetHandler> mClientList = null;
        static void Main(string[] args)
        {
            Console.WriteLine("Hello, World!");

            mClientList = new List<NetHandler>();
            for (int i = 0; i < 1; i++)
            {
                NetHandler mNetHandler = new NetHandler();
                mNetHandler.Init();
                mClientList.Add(mNetHandler);
            }

            UpdateMgr.Do(Update);
        }

        static void Update(double fElapsed)
        {
            foreach (var v in mClientList)
            {
                v.Update(fElapsed);
            }
        }
    }
}
