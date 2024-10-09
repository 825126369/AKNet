using TcpProtocol;
using XKNet.Common;
using XKNet.Tcp.Common;
using XKNet.Tcp.Server;

namespace LoginServer
{
    public class NetHandler
    {
        TcpNetServerMain mNetServer = null;
        public void Init()
        {
            mNetServer = new TcpNetServerMain();
            //mNetServer.mPackageManager.addNetListenFun(.COMMAND_TESTCHAT, receive_csChat);
            //mNetServer.mPackageManager.addNetListenFun(NetProtocolCommand.CS_REQUEST_LOGIN, receive_csRequestLogin);
            UInt16 nPort = 10002;
            mNetServer.InitNet("0.0.0.0", nPort);
        }

        public void Update(double fElapsedTime)
        {
            mNetServer.Update(fElapsedTime);
        }

        private static void receive_csChat(ClientPeerBase clientPeer, NetPackage package)
        {
            TESTChatMessage mSendMsg = Protocol3Utility.getData<TESTChatMessage>(package);
            //clientPeer.SendNetData(NetProtocolCommand.COMMAND_TESTCHAT, mSendMsg);
            IMessagePool<TESTChatMessage>.recycle(mSendMsg);
        }
    }
}