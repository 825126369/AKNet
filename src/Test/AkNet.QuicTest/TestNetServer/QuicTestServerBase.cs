using AKNet.Common;
using AKNet.Extentions.Protobuf;
using TestCommon;
using TestProtocol;

namespace TestNetServer
{
    public abstract class QuicTestServerBase
    {
        QuicServerMainBase mNetServer = null;
        const int NetCommand_COMMAND_TESTCHAT = 1000;
        public const int nSingleClientStreamCount = 3;
        public abstract QuicServerMainBase Create();

        public void Start()
        {
            NetLog.AddConsoleLog();
            Init();
            UpdateMgr.Do(Update);
        }
        
        public void Init()
        {
            mNetServer = Create();
            mNetServer.addNetListenFunc(NetCommand_COMMAND_TESTCHAT, ReceiveChatMessage);
            mNetServer.InitNet(6000);
        }

        public void Update(double fElapsedTime)
        {
            mNetServer.Update(fElapsedTime);
        }

        private void ReceiveChatMessage(QuicClientPeerBase peer, QuicNetPackage mPackage)
        {
            TESTChatMessage mdata = Proto3Tool.GetData<TESTChatMessage>(mPackage);
            peer.SendNetData(1, NetCommand_COMMAND_TESTCHAT, mdata);
            peer.SendNetData(2, NetCommand_COMMAND_TESTCHAT, mdata);
            peer.SendNetData(3, NetCommand_COMMAND_TESTCHAT, mdata);
            peer.SendNetData(4, NetCommand_COMMAND_TESTCHAT, mdata);
            peer.SendNetData(5, NetCommand_COMMAND_TESTCHAT, mdata);
            peer.SendNetData(6, NetCommand_COMMAND_TESTCHAT, mdata);
            IMessagePool<TESTChatMessage>.recycle(mdata);
        }
    }
}

