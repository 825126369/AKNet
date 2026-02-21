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
            ConfigInstance mConfig = new ConfigInstance();
            mConfig.bAutoReConnect = false;
            mConfig.MaxPlayerCount = 10;
            mNetServer = new NetServerMain(NetType.Udp3Tcp, mConfig);
            mNetServer.addNetListenFunc(COMMAND_TESTCHAT, receive_csChat);
            mNetServer.InitNet(6000);
        }

        public void Update(double fElapsedTime)
        {
            mNetServer.Update(fElapsedTime);
        }

        private static void receive_csChat(ClientPeerBase clientPeer, NetPackage package)
        {
            TESTChatMessage mReceiveMsg = TESTChatMessage.Parser.ParseFrom(package.GetData());
            Console.WriteLine(mReceiveMsg.TalkMsg);
            SendMsg(clientPeer);
        }

        private static void SendMsg(ClientPeerBase peer)
        {
            TESTChatMessage mdata = new TESTChatMessage();
            mdata.TalkMsg = "Hello, AkNet Client";
            peer.SendNetData(COMMAND_TESTCHAT, mdata.ToByteArray());
        }
    }
}