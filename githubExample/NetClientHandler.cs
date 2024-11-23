using AKNet.Common;
using AKNet.Tcp.Client;
using TestProtocol;

namespace githubExample
{
    public class NetClientHandler
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
            TESTChatMessage mdata = TESTChatMessage.Parser.ParseFrom(mPackage.GetData());
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

