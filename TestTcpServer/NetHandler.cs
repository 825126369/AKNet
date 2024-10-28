using TcpProtocol;
using TestProtocol;
using XKNet.Common;
using XKNet.Tcp.Common;
using XKNet.Tcp.Server;

namespace TestTcpServer
{
    public class NetHandler
    {
        TcpNetServerMain mNetServer = null;
        const int TcpNetCommand_COMMAND_TESTCHAT = 1000;
        public void Init()
        {
            mNetServer = new TcpNetServerMain();
            mNetServer.addNetListenFun(TcpNetCommand_COMMAND_TESTCHAT, receive_csChat);
            mNetServer.InitNet("0.0.0.0", 1002);
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