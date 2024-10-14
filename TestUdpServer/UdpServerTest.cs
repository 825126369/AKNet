using TestProtocol;
using UdpPointtopointProtocols;
using XKNet.Common;
using XKNet.Udp.POINTTOPOINT.Common;
using XKNet.Udp.POINTTOPOINT.Server;

public class UdpServerTest
{
    UdpNetServerMain mNetServer = new UdpNetServerMain();

    public const bool InTest = true;
    public void Init()
    {
        mNetServer.addNetListenFun(UdpNetCommand.COMMAND_TESTCHAT, ReceiveMessage);
        mNetServer.InitNet("0.0.0.0", 10001);
    }

    public void Update(double fElapsedTime)
    {
        mNetServer.Update(fElapsedTime);
    }

    private void ReceiveMessage(ClientPeerBase peer, NetPackage mPackage)
    {
        TESTChatMessage mdata = Protocol3Utility.getData<TESTChatMessage>(mPackage);
        peer.SendNetData(UdpNetCommand.COMMAND_TESTCHAT, mdata);
        IMessagePool<TESTChatMessage>.recycle(mdata);
    }
}

