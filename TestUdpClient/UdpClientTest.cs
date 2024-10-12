using UdpPointtopointProtocols;
using XKNet.Common;
using XKNet.Udp.POINTTOPOINT.Client;
using XKNet.Udp.POINTTOPOINT.Common;

public class UdpClientTest
{
    public int nClientCount = 1;
    public int nPackageCount = 1;
    List<UdpNetClientMain> mClientList = new List<UdpNetClientMain>();

    System.Random mRandom = new System.Random();
    public void Init()
    {
        for (int i = 0; i < nClientCount; i++)
        {
            UdpNetClientMain mNetClient = new UdpNetClientMain();
            mClientList.Add(mNetClient);

            mNetClient.addNetListenFun(UdpNetCommand.COMMAND_TESTCHAT, ReceiveMessage);
            mNetClient.ConnectServer("127.0.0.1", 10001);
        }
    }

    public void Update(double fElapsedTime)
    {
        for (int i = 0; i < nClientCount; i++)
        {
            UdpNetClientMain v = mClientList[i];
            UdpNetClientMain mNetClient = v;
            mNetClient.Update(fElapsedTime);

            for (int j = 0; j < nPackageCount; j++)
            {
                TESTChatMessage mdata = IMessagePool<TESTChatMessage>.Pop();
                mdata.Id = (uint)(i + 1);
                if (mRandom.Next(1, 3) == 1)
                {
                    mdata.TalkMsg = "Begins..........End";
                }
                else
                {
                    mdata.TalkMsg = "Begin。。。。。。。。。。。。............................................" +
                        "...................................................................................." +
                        "...................................................................." +
                        "sdfsfsf.s.fsfsfds.df.s.fwqerqweprijqwperqwerqowheropwheporpwerjpo qjwepowiopeqwoerpowqejoqwejoqwjeo  " +
                        " qweopqwjeop opqweuq opweuo  eqwup   quweopiquowequoewuqowe" +
                        ".........................................End";
                }
                mNetClient.SendNetData(UdpNetCommand.COMMAND_TESTCHAT, mdata);
                IMessagePool<TESTChatMessage>.recycle(mdata);
            }
        }
    }

    void ReceiveMessage(ClientPeerBase peer, NetPackage mPackage)
    {
        TESTChatMessage mdata = Protocol3Utility.getData<TESTChatMessage>(mPackage);
        IMessagePool<TESTChatMessage>.recycle(mdata);
    }
}

