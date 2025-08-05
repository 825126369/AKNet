using AKNet.Common;
using AKNet.Extentions.Protobuf;
using TestProtocol;

namespace TestNetServer
{
    public class NetHandler
    {
        NetServerMain mNetServer = null;
        const int NetCommand_COMMAND_TESTCHAT = 1000;

        public const bool InTest = true;
        private double fDuringTime;
        public void Init()
        {
            mNetServer = new NetServerMain(NetType.UDP);
            mNetServer.addNetListenFunc(NetCommand_COMMAND_TESTCHAT, ReceiveMessage);
            mNetServer.InitNet(6000);
        }

        public void Update(double fElapsedTime)
        {
            mNetServer.Update(fElapsedTime);

            fDuringTime += fElapsedTime;
            if (fDuringTime > 5.0)
            {
                fDuringTime = 0.0;
            }

        }

        private void ReceiveMessage(ClientPeerBase peer, NetPackage mPackage)
        {
            TESTChatMessage mdata = Protocol3Utility.getData<TESTChatMessage>(mPackage);
            //Console.WriteLine("ReceiveMessage: " + mdata.TalkMsg);
            peer.SendNetData(NetCommand_COMMAND_TESTCHAT, mdata);
            IMessagePool<TESTChatMessage>.recycle(mdata);
        }
    }
}

