using UdpPointtopointProtocols;
using XKNet.Udp.Server;

public class UdpServerTest
{
    Udps mNetServer = new TcpNetServerMain();

    public const bool InTest = true;
    private void Init()
    {
        mNetServer.GetPackageManager().addNetListenFun(UdpNetCommand.COMMAND_TESTCHAT, ReceiveMessage);
        mNetServer.InitNet("0.0.0.0", 10001);
    }

    private void Update()
    {
        mNetServer.Update(Time.deltaTime);
    }

    void OnDestroy()
    {
        mNetServer.Release();
    }

    private void ReceiveMessage(ClientPeer peer, NetPackage mPackage)
    {
        //Debug.Log("Server 收到数据: " + mPackage.nOrderId + " | " + mPackage.Length + " | " + mPackage.buffer.Length);
        TESTChatMessage mdata = Protocol3Utility.getData<TESTChatMessage>(mPackage);
        peer.SendNetData(UdpNetCommand.COMMAND_TESTCHAT, mdata);
        ProtobufHelper.IMessagePool<TESTChatMessage>.recycle(mdata);
    }
}

