# AKNet
这是一个包括 TCP，UDP，Protobuf 的C#游戏网络库。

这是一个致力于实现 TCP 和 UDP 无缝切换的游戏网络库。

这是一个致力于实现高性能 TCP 以及 高性能可靠有序的 UDP 游戏网络库。

这是一个面向 .Net Standard 2.1 的游戏网络库, 兼容Unity游戏引擎和.Net 5.0以上版本。

如果你另外对 WebSocket/Http 游戏网络库有兴趣 或者 有项目需求, 可联系我 1426186059@qq.com

支持作者，就打赏点小钱吧！ 
<img src="https://github.com/825126369/AKNet/blob/main/Image/shoukuan.jpg" alt="支持作者收款码" width="50%" />


``` Main Example:
using System.Diagnostics;

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
            UpdateMgr.Do(Update, 60);
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

    public static class UpdateMgr
    {
        private static readonly Stopwatch mStopWatch = Stopwatch.StartNew();
        private static double fElapsed = 0;

        public static double deltaTime
        {
            get { return fElapsed; }
        }

        public static double realtimeSinceStartup
        {
            get { return mStopWatch.ElapsedMilliseconds / 1000.0; }
        }

        public static void Do(Action<double> updateFunc, int nTargetFPS = 30)
        {
            int nFrameTime = (int)Math.Ceiling(1000.0 / nTargetFPS);

            long fBeginTime = mStopWatch.ElapsedMilliseconds;
            long fFinishTime = mStopWatch.ElapsedMilliseconds;
            fElapsed = 0.0;
            while (true)
            {
                fBeginTime = mStopWatch.ElapsedMilliseconds;
                updateFunc(fElapsed);

                int fElapsed2 = (int)(mStopWatch.ElapsedMilliseconds - fBeginTime);
                int nSleepTime = Math.Max(0, nFrameTime - fElapsed2);
                Thread.Sleep(nSleepTime);
                fFinishTime = mStopWatch.ElapsedMilliseconds;
                fElapsed = (fFinishTime - fBeginTime) / 1000.0;
            }
        }
    }
}
```

``` Server Example:
using AKNet.Common;
using Google.Protobuf;
using TestProtocol;

namespace githubExample
{
    public class NetServerHandler
    {
        NetServerMain mNetServer = null;
        const int COMMAND_TESTCHAT = 1000;
        public void Init()
        {
            mNetServer = new NetServerMain(NetType.UDP);
            mNetServer.addNetListenFunc(COMMAND_TESTCHAT, receive_csChat);
            mNetServer.InitNet(6000);
        }

        public void Update(double fElapsedTime)
        {
            mNetServer.Update(fElapsedTime);
        }

        private static void receive_csChat(ClientPeerBase clientPeer, NetPackage package)
        {
            TESTChatMessage mReceiveMsg = Protocol3Utility.getData<TESTChatMessage>(package);
            Console.WriteLine(mReceiveMsg.TalkMsg);

            SendMsg(clientPeer);
            IMessagePool<TESTChatMessage>.recycle(mReceiveMsg);
        }

        private static void SendMsg(ClientPeerBase peer)
        {
            TESTChatMessage mdata = IMessagePool<TESTChatMessage>.Pop();
            mdata.TalkMsg = "Hello, AkNet Client";
            peer.SendNetData(COMMAND_TESTCHAT, mdata.ToByteArray());
            IMessagePool<TESTChatMessage>.recycle(mdata);

        }
    }
}
```

``` Client Example:
using AKNet.Common;
using TestProtocol;

namespace githubExample
{
    public class NetClientHandler
    {
        NetClientMain mNetClient = null;
        const int COMMAND_TESTCHAT = 1000;

        public void Init()
        {
            mNetClient = new NetClientMain(NetType.UDP);
            mNetClient.addListenClientPeerStateFunc(OnSocketStateChanged);
            mNetClient.addNetListenFunc(COMMAND_TESTCHAT, ReceiveMessage);
            mNetClient.ConnectServer("127.0.0.1", 6000);
        }

        private void OnSocketStateChanged(ClientPeerBase peer)
        {
            if (peer.GetSocketState() == SOCKET_PEER_STATE.CONNECTED)
            {
                SendMsg(mNetClient);
            }
        }

        public void Update(double fElapsedTime)
        {
            mNetClient.Update(fElapsedTime);
        }

        void ReceiveMessage(ClientPeerBase peer, NetPackage mPackage)
        {
            TESTChatMessage mdata = TESTChatMessage.Parser.ParseFrom(mPackage.GetData());
            Console.WriteLine(mdata.TalkMsg);
            IMessagePool<TESTChatMessage>.recycle(mdata);
        }

        private void SendMsg(ClientPeerBase peer)
        {
            TESTChatMessage mdata = new TESTChatMessage();
            mdata.TalkMsg = "Hello, AkNet Server";
            mNetClient.SendNetData(COMMAND_TESTCHAT, mdata);
        }
    }
}

```

## License

This repository is licensed with the [MIT](LICENSE) license.
