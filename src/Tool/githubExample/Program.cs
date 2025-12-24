using AKNet.Common;

namespace githubExample
{
    internal class Program
    {
        static void Main(string[] args)
        {
            NetLog.AddConsoleLog();
            var mServer = new NetServerHandler();
            mServer.Init();
            var mClient = new NetClientHandler();
            mClient.Init();

            while (true)
            {
                mServer.Update(0.001);
                mClient.Update(0.001);
                Thread.Sleep(1);
            }
        }
    }
}
