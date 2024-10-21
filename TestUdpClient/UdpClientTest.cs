using System.Diagnostics;
using TestProtocol;
using XKNet.Common;
using XKNet.Udp.POINTTOPOINT.Client;
using XKNet.Udp.POINTTOPOINT.Common;

public class UdpClientTest
{
    public int nClientCount = 1;
    public int nPackageCount = 1;
    List<UdpNetClientMain> mClientList = new List<UdpNetClientMain>();

    System.Random mRandom = new System.Random();
    Stopwatch mStopWatch = new Stopwatch();
    public void Init()
    {
        for (int i = 0; i < nClientCount; i++)
        {
            UdpNetClientMain mNetClient = new UdpNetClientMain();
            mClientList.Add(mNetClient);

            mNetClient.addNetListenFun(UdpNetCommand.COMMAND_TESTCHAT, ReceiveMessage);
            mNetClient.ConnectServer("127.0.0.1", 10001);
        }

        mStopWatch.Start();
    }

    double fSumTime = 0;
    uint Id = 0;
    public void Update(double fElapsedTime)
    {
        for (int i = 0; i < nClientCount; i++)
        {
            UdpNetClientMain v = mClientList[i];
            UdpNetClientMain mNetClient = v;
            mNetClient.Update(fElapsedTime);


            fSumTime += fElapsedTime;
            if (fSumTime > 0)
            {
                fSumTime = 0;
                for (int j = 0; j < nPackageCount; j++)
                {
                    TESTChatMessage mdata = IMessagePool<TESTChatMessage>.Pop();
                    mdata.Id = ++Id;
                    if (mRandom.Next(2, 3) == 1)
                    {
                        mdata.TalkMsg = "Begins..........End";
                    }
                    else
                    {
                        mdata.TalkMsg = "Begin。。。。。。。。。。。。............................................" +
                            "...................................................................................." +
                            "...................................................................." +
                            "sdfsfsf.s.fsfsfds.df.s.fwqerqweprijqwperqwerqowheropwheporpwerjpo qjwepowiopeqwoerpowqejoqwejoqwjeo  " +
                             "sdfsfsf.s.fsfsfds.df.s.fwqerqweprijqwperqwerqowheropwheporpwerjpo qjwepowiopeqwoerpowqejoqwejoqwjeo  " +
                            "sdfsfsf.s.fsfsfds.df.s.fwqerqweprijqwperqwerqowheropwheporpwerjpo qjwepowiopeqwoerpowqejoqwejoqwjeo  " +
                            "sdfsfsf.s.fsfsfds.df.s.fwqerqweprijqwperqwerqowheropwheporpwerjpo qjwepowiopeqwoerpowqejoqwejoqwjeo  " +
                            " qweopqwjeop opqweuq opweuo  eqwup   quweopiquowequoewuqowe" +

                            "床前明月光，疑是地上霜。\r\n\r\n举头望明月，低头思故乡。" +
                            "床前明月光，疑是地上霜。\r\n\r\n举头望明月，低头思故乡。" +
                            ".........................................End";
                    }

                    if (Id <= 5000)
                    {
                        mNetClient.SendNetData(UdpNetCommand.COMMAND_TESTCHAT, mdata);
                        IMessagePool<TESTChatMessage>.recycle(mdata);
                    }
                }
            }
        }
    }

    void ReceiveMessage(ClientPeerBase peer, NetPackage mPackage)
    {
        TESTChatMessage mdata = Protocol3Utility.getData<TESTChatMessage>(mPackage);
        //Console.WriteLine("Receive Chat Message: " + mdata.Id);

        if (mdata.Id == 5000)
        {
            Console.WriteLine($"总共花费时间 {mStopWatch.Elapsed.TotalSeconds}");
        }

        IMessagePool<TESTChatMessage>.recycle(mdata);
    }
}

