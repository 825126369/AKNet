using TestCommon;

namespace githubExample
{
    internal class Program
    {
        static NetServerHandler mServer;
        static NetClientHandler mClient;
        static void Main(string[] args)
        {
            mServer = new NetServerHandler();
            mServer.Init();
            mClient = new NetClientHandler();
            mClient.Init();
            UpdateMgr.Do(Update);
        }

        static void Update(double fElapsed)
        {
            if (fElapsed >= 0.3)
            {
                Console.WriteLine("TestUdpClient 帧 时间 太长: " + fElapsed);
            }

            mServer.Update(fElapsed);
            mClient.Update(fElapsed);
        }
    }

}
