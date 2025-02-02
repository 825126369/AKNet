using AKNet.Common;
using AKNet.Extentions.Protobuf;
using TestProtocol;

namespace TestNetServer
{
    public class NetHandler
    {
        NetServerMain mNetServer = null;
        const int NetCommand_COMMAND_TESTCHAT = 1000;
        int nSumReceiveCount = 0;

        public const bool InTest = true;
        public void Init()
        {
            mNetServer = new NetServerMain(NetType.Udp4LinuxTcp);
            mNetServer.addNetListenFunc(NetCommand_COMMAND_TESTCHAT, ReceiveMessage);
            mNetServer.InitNet(6000);
        }

        public void Update(double fElapsedTime)
        {
            mNetServer.Update(fElapsedTime);
        }

        private void ReceiveMessage(ClientPeerBase peer, NetPackage mPackage)
        {
            TESTChatMessage mdata = Protocol3Utility.getData<TESTChatMessage>(mPackage);
            //Console.WriteLine("ReceiveMessage: " + mdata.TalkMsg);
            //peer.SendNetData(NetCommand_COMMAND_TESTCHAT, mdata);
            //IMessagePool<TESTChatMessage>.recycle(mdata);
            nSumReceiveCount++;
            Console.WriteLine("nSumReceiveCount: " + nSumReceiveCount);
        }
    }
}

