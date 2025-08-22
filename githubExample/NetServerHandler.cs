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

            var mInstance = mNetServer.GetInstance() as AKNet.Udp.POINTTOPOINT.Server.UdpNetServerMain;
            mInstance.GetConfig().nECryptoType = ECryptoType.Xor;
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