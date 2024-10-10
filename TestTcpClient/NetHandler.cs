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
            mNetClient.ConnectServer("127.0.0.1", 1002);
        }

        public void Update(double fElapsedTime)
        {
            mNetClient.Update(fElapsedTime);
            SendChatInfo();
        }

        private void SendChatInfo()
        {
            TESTChatMessage mData = new TESTChatMessage();
            mData.Id = 0;
            mData.TalkMsg = "sdfsfsfdsfsfsfsdfsfsdf";
            mNetClient.SendNetData(TcpNetCommand.COMMAND_TESTCHAT, mData);
        }

        private static void receive_csChat(ClientPeerBase clientPeer, NetPackage package)
        {
            TESTChatMessage mSendMsg = Protocol3Utility.getData<TESTChatMessage>(package);
            IMessagePool<TESTChatMessage>.recycle(mSendMsg);
        }
    }
}