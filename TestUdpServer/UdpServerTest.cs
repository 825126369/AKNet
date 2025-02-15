using AKNet.Common;
using AKNet.Extentions.Protobuf;
using AKNet.Udp4LinuxTcp.Server;
using TestProtocol;

public class UdpServerTest
{
    Udp4LinuxTcpNetServerMain mNetServer = new Udp4LinuxTcpNetServerMain();
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

    int nSumReceiveCount = 0;
    private void ReceiveMessage(ClientPeerBase peer, NetPackage mPackage)
    {
        TESTChatMessage mdata = Protocol3Utility.getData<TESTChatMessage>(mPackage);
        nSumReceiveCount++;
        Console.WriteLine("nSumReceiveCount: " + nSumReceiveCount);

        peer.SendNetData(UdpNetCommand_COMMAND_TESTCHAT, mdata);
        IMessagePool<TESTChatMessage>.recycle(mdata);
    }
}

