# AKNet
这是一个包括 TCP，UDP 的C#游戏网络库, 支持Unity游戏引擎以及.Net 10.0更高版本。

这是一个致力于实现 UDP 超越 TCP 的可靠有序高性能算法。

特点1:  UDP和TCP无缝切换，高性能，稳定可靠。 

特点2： 实现了一个C# Udp版的Linux TCP 缩略版，原封不动的保留了Linux TCP 最精华的代码，可以很轻松的与Linux TCP保持代码同步。

特点3： 实现了一个C# Quic版本，原封不动的保留了MSQuic最精华的代码，可以很轻松的与 MSQuic 保持代码同步。（Demo阶段）

# UDP和TCP总结

1: UDP比TCP快？ 错。 UDP 和 TCP 底层走的都是IP层/物理链路层，只拿 数据分割包来说, 两者没有差异，甚至很多硬件提供了对TCP流的更好的支持。

2：UDP丢包率比TCP低？ 错。 同上，两者在 IP层/物理链路层 差异不大，主要源于上层逻辑 数据重传/丢包检测/拥塞控制算法 的差异。

3：UDP比TCP更省带宽？ 错。 一个没有实现 稳定/可靠/兼容性好的 UDP，确实包头长度小于TCP, 但如果UDP要实现 稳定/可靠/兼容性好 的话，某种程度上这个优势就不存在了。

4：QUIC比TCP更现代化？ 错。TCP有众多大神维护，很多QUIC实现的算法，早已经在TCP上实战检验过。 

UDP的优势，主要体现在 灵活性。 上层可靠有序算法，程序员可任意修改，刚出来的很多 安全算法/性能更优的库，可以立刻在UDP 上应用。 这才是 UDP/QUIC 的意义所在。

(QUIC/UDP 夸张说法: 解决了队头阻塞的问题。其实就是1个连接有N个流，这些流互不干扰，自然不存在相互阻塞的问题。但流内部依然会存在队头阻塞。一般开发中，1个流足够了)

# 找份工作
找份工作20K左右: 10多年Unity游戏开发经验  邮箱：1426186059@qq.com, 微信: AAA-2025-666-888

# 客户端/服务器 例子
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
            NetLog.AddConsoleLog();
            mServer = new NetServerHandler();
            mServer.Init();
            mClient = new NetClientHandler();
            mClient.Init();
            UpdateMgr.Do(Update);
        }

        static void Update(double fElapsed)
        {
            mServer.Update(fElapsed);
            mClient.Update(fElapsed);
        }
    }

    public static class UpdateMgr
    {
        private static readonly Stopwatch mStopWatch = Stopwatch.StartNew();
        private static double fElapsed = 0;

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
using AKNet.Extentions.Protobuf;
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
            TESTChatMessage mReceiveMsg = Proto3Tool.GetData<TESTChatMessage>(package);
            Console.WriteLine(mReceiveMsg.TalkMsg);
            SendMsg(clientPeer);
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
using AKNet.Extentions.Protobuf;
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
                SendMsg();
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
        }

        private void SendMsg()
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
