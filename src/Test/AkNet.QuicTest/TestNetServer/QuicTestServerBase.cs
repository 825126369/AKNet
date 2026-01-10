using AKNet.Common;
using AKNet.Extentions.Protobuf;
using System.Diagnostics;
using TestCommon;
using TestProtocol;

namespace TestNetServer
{
    public abstract class QuicTestServerBase
    {
        QuicServerMainBase mNetServer = null;
        const int NetCommand_COMMAND_TESTCHAT = 1000;
        public const int nSingleClientStreamCount = 1;
        public abstract QuicServerMainBase Create();
        int nReceivePackageCount;

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
            nReceivePackageCount++;
            for (byte i = 1; i <= nSingleClientStreamCount; i++)
            {
                peer.SendNetData(i, NetCommand_COMMAND_TESTCHAT, mdata);
            }
            IMessagePool<TESTChatMessage>.recycle(mdata);

            if (nReceivePackageCount % 10000 == 0)
            {
                NetLog.Log($"接受包数量: {nReceivePackageCount}");
            }
        }
    }
}

