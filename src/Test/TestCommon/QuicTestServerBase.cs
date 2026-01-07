using AKNet.Common;
using AKNet.Extentions.Protobuf;
using TestCommon;
using TestProtocol;

namespace TestNetServer
{
    public abstract class QuicTestServerBase
    {
        NetServerMainBase mNetServer = null;
        const int NetCommand_COMMAND_TESTCHAT = 1000;

        public abstract NetServerMainBase Create();

        public void Start()
        {
            NetLog.AddConsoleLog();
            Init();
            UpdateMgr.Do(Update);
        }
        
        public void Init()
        {
            mNetServer = Create();
            mNetServer.addNetListenFunc(NetCommand_COMMAND_TESTCHAT, ReceiveMessage);
            mNetServer.InitNet(6000);
        }

        public void Update(double fElapsedTime)
        {
            mNetServer.Update(fElapsedTime);
        }

        private void ReceiveMessage(ClientPeerBase peer, NetPackage mPackage)
        {
            TESTChatMessage mdata = Proto3Tool.GetData<TESTChatMessage>(mPackage);
            peer.SendNetData(NetCommand_COMMAND_TESTCHAT, mdata);
            IMessagePool<TESTChatMessage>.recycle(mdata);
        }
    }
}

