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
        public void Init()
        {
            mNetServer = new TcpNetServerMain();
            mNetServer.addNetListenFun(1, receive_csChat);
            mNetServer.InitNet("0.0.0.0", 1002);
        }

        public void Update(double fElapsedTime)
        {
            mNetServer.Update(fElapsedTime);
        }

        private static void receive_csChat(ClientPeerBase clientPeer, NetPackage package)
        {
            TESTChatMessage mSendMsg = Protocol3Utility.getData<TESTChatMessage>(package);
            clientPeer.SendNetData(1, mSendMsg);
            IMessagePool<TESTChatMessage>.recycle(mSendMsg);
        }
    }
}