using AKNet.Common;
using AKNet.Extentions.Protobuf;
using AKNet.Udp2Tcp.Server;
using TestProtocol;

public class UdpServerTest
{
    Udp2TcpNetServerMain mNetServer = new Udp2TcpNetServerMain();
    const int UdpNetCommand_COMMAND_TESTCHAT = 1000;

    public const bool InTest = true;
    public void Init()
    {
        mNetServer.addNetListenFunc(UdpNetCommand_COMMAND_TESTCHAT, ReceiveMessage);
        mNetServer.InitNet(6000);
    }

    public void Update(double fElapsedTime)
    {
        mNetServer.Update(fElapsedTime);

        //if (mPackageStatisticalTimeOut.orTimeOut(fElapsedTime))
        //{
        //    PackageStatistical.PrintLog();
        //}
    }

    private void ReceiveMessage(ClientPeerBase peer, NetPackage mPackage)
    {
        TESTChatMessage mdata = Protocol3Utility.getData<TESTChatMessage>(mPackage);
        peer.SendNetData(UdpNetCommand_COMMAND_TESTCHAT, mdata);
        IMessagePool<TESTChatMessage>.recycle(mdata);
    }
}

