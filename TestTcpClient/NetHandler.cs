using TcpProtocol;
using XKNet.Common;
using XKNet.Tcp.Client;
using XKNet.Tcp.Common;

namespace TestTcpClient
{
    public class NetHandler
    {
        TcpNetClientMain mNetClient = null;
        public void Init()
        {
            mNetClient = new TcpNetClientMain();
            mNetClient.addNetListenFun(TcpNetCommand.COMMAND_TESTCHAT, receive_csChat);
            mNetClient.ConnectServer("0.0.0.0", 1002);
        }

        public void Update(double fElapsedTime)
        {
            mNetClient.Update(fElapsedTime);
        }

        private static void receive_csChat(ClientPeerBase clientPeer, NetPackage package)
        {
            TESTChatMessage mSendMsg = Protocol3Utility.getData<TESTChatMessage>(package);
            clientPeer.SendNetData(TcpNetCommand.COMMAND_TESTCHAT, mSendMsg);
            IMessagePool<TESTChatMessage>.recycle(mSendMsg);
        }
    }
}