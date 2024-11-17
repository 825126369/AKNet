# AKNet
这是一个包括 TCP，UDP，Protobuf 的C#游戏网络库。

此库致力于实现高性能 TCP 以及 高性能可靠有序的 UDP 游戏网络库。

如果你另外对 WebSocket/Http 游戏网络库有兴趣 或者 有项目需求, 可联系我 1426186059@qq.com

支持作者，就打赏点小钱吧！ 
<img src="https://github.com/825126369/AKNet/blob/main/Image/shoukuan.jpg" alt="支持作者收款码" width="50%" />

``` Server Example:
using AKNet.Common;
using AKNet.Tcp.Server;
using Google.Protobuf;
using TestProtocol;

namespace githubExample
{
    public class NetServerHandler
    {
        TcpNetServerMain mNetServer = null;
        const int COMMAND_TESTCHAT = 1000;
        public void Init()
        {
            mNetServer = new TcpNetServerMain();
            mNetServer.addNetListenFun(COMMAND_TESTCHAT, receive_csChat);
            mNetServer.InitNet(6000);
        }

        public void Update(double fElapsedTime)
        {
            mNetServer.Update(fElapsedTime);
        }

        private static void receive_csChat(ClientPeerBase clientPeer, NetPackage package)
        {
            TESTChatMessage mSendMsg = Protocol3Utility.getData<TESTChatMessage>(package);
            SendMsg(clientPeer);
            IMessagePool<TESTChatMessage>.recycle(mSendMsg);
        }

        private static void SendMsg(ClientPeerBase peer)
        {
            TESTChatMessage mdata = IMessagePool<TESTChatMessage>.Pop();
            mdata.NClientId = 1;
            mdata.NSortId = 2;
            mdata.TalkMsg = "Hello, AkNet Server";
            peer.SendNetData(COMMAND_TESTCHAT, mdata.ToByteArray());
            IMessagePool<TESTChatMessage>.recycle(mdata);

        }
    }
}
```

``` Client Example:
using AKNet.Common;
using AKNet.Tcp.Client;
using Google.Protobuf;
using TestProtocol;

namespace githubExample
{
    public class TcpClientTest
    {
        TcpNetClientMain mNetClient = new TcpNetClientMain();
        const int COMMAND_TESTCHAT = 1000;

        public void Init()
        {
            mNetClient.addListenClientPeerStateFunc(OnSocketStateChanged);
            mNetClient.addNetListenFun(COMMAND_TESTCHAT, ReceiveMessage);
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
            TESTChatMessage mdata = TESTChatMessage.Parser.ParseFrom(mPackage.GetProtoBuff());
            Console.WriteLine(mdata.TalkMsg);
            IMessagePool<TESTChatMessage>.recycle(mdata);
        }

        private void SendMsg(ClientPeerBase peer)
        {
            TESTChatMessage mdata = new TESTChatMessage();
            mdata.NClientId = 1;
            mdata.NSortId = 2;
            mdata.TalkMsg = "Hello, AkNet Client";
            mNetClient.SendNetData(COMMAND_TESTCHAT, mdata);
        }
    }
}
```

## License

This repository is licensed with the [MIT](LICENSE) license.
