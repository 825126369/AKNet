using AKNet.Common;
using AKNet.Tcp.Server;
using TestProtocol;

namespace TestTcpServer
{
    public class NetHandler
    {
        TcpNetServerMain mNetServer = null;
        const int TcpNetCommand_COMMAND_TESTCHAT = 1000;
        public void Init()
        {
            mNetServer = new TcpNetServerMain();
            mNetServer.addNetListenFunc(TcpNetCommand_COMMAND_TESTCHAT, receive_csChat);
            mNetServer.InitNet(6000);
        }

        public void Update(double fElapsedTime)
        {
            mNetServer.Update(fElapsedTime);
        }

        private static void receive_csChat(ClientPeerBase clientPeer, NetPackage package)
        {
            TESTChatMessage mSendMsg = Protocol3Utility.getData<TESTChatMessage>(package);
            clientPeer.SendNetData(TcpNetCommand_COMMAND_TESTCHAT, mSendMsg);
            IMessagePool<TESTChatMessage>.recycle(mSendMsg);
        }
    }
}