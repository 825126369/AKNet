using AKNet.Common;
using System.Diagnostics;
using TestCommon;
using TestProtocol;

public class NetHandler
{
    public const int nClientCount = 20;
    public const int nPackageCount = 50;
    public const int nSumPackageCount = nClientCount * 10000;
    int nReceivePackageCount = 0;
    List<NetClientMain> mClientList = new List<NetClientMain>();
    Stopwatch mStopWatch = new Stopwatch();
    readonly List<uint> mFinishClientId = new List<uint>();

    const int UdpNetCommand_COMMAND_TESTCHAT = 1000;
    const string logFileName = $"TestLog.txt";

    const string TalkMsg1 = "Begin..........End";
    const string TalkMsg2 = "Begin。。。。。。。。。。。。............................................" +
                                    "...................................................................................." +
                                    "...................................................................." +
                                    "sdfsfsf.s.fsfsfds.df.s.fwqerqweprijqwperqwerqowheropwheporpwerjpo qjwepowiopeqwoerpowqejoqwejoqwjeo  " +
                                     "sdfsfsf.s.fsfsfds.df.s.fwqerqweprijqwperqwerqowheropwheporpwerjpo qjwepowiopeqwoerpowqejoqwejoqwjeo  " +
                                    "sdfsfsf.s.fsfsfds.df.s.fwqerqweprijqwperqwerqowheropwheporpwerjpo qjwepowiopeqwoerpowqejoqwejoqwjeo  " +
                                    "sdfsfsf.s.fsfsfds.df.s.fwqerqweprijqwperqwerqowheropwheporpwerjpo qjwepowiopeqwoerpowqejoqwejoqwjeo  " +
                                    " qweopqwjeop opqweuq opweuo  eqwup   quweopiquowequoewuqowe" +
                                    " qweopqwjeop opqweuq opweuo  eqwup   quweopiquowequoewuqowe" +
                                    " qweopqwjeop opqweuq opweuo  eqwup   quweopiquowequoewuqowe" +
                                    " qweopqwjeop opqweuq opweuo  eqwup   quweopiquowequoewuqowe" +
                                    " qweopqwjeop opqweuq opweuo  eqwup   quweopiquowequoewuqowe" +
                                    " qweopqwjeop opqweuq opweuo  eqwup   quweopiquowequoewuqowe" +
                                    " qweopqwjeop opqweuq opweuo  eqwup   quweopiquowequoewuqowe" +
                                    " qweopqwjeop opqweuq opweuo  eqwup   quweopiquowequoewuqowe" +
                                    " qweopqwjeop opqweuq opweuo  eqwup   quweopiquowequoewuqowe" +

                                    "床前明月光，疑是地上霜。\r\n\r\n举头望明月，低头思故乡。" +
                                    "床前明月光，疑是地上霜。\r\n\r\n举头望明月，低头思故乡。" +
                                    ".........................................End";

    public void Init()
    {
        File.Delete(logFileName);
        for (int i = 0; i < nClientCount; i++)
        {
            NetClientMain mNetClient = new NetClientMain(NetType.UDP);
            mClientList.Add(mNetClient);
            mNetClient.addNetListenFunc(UdpNetCommand_COMMAND_TESTCHAT, ReceiveMessage);
            mNetClient.ConnectServer("127.0.0.1", 6000);
        }

        mFinishClientId.Clear();
        mStopWatch.Start();
        nReceivePackageCount = 0;
    }

    double fSumTime = 0;
    uint Id = 0;
    public void Update(double fElapsedTime)
    {
        //ProfilerTool2.TestStart();
        for (int i = 0; i < nClientCount; i++)
        {
            var v = mClientList[i];
            var mNetClient = v;
            mNetClient.Update(fElapsedTime);

            if (mNetClient.GetSocketState() == SOCKET_PEER_STATE.CONNECTED)
            {
                fSumTime += fElapsedTime;
                if (fSumTime > 0)
                {
                    fSumTime = 0;
                    for (int j = 0; j < nPackageCount; j++)
                    {
                        Id++;
                        if (Id <= nSumPackageCount)
                        {
                            TESTChatMessage mdata = IMessagePool<TESTChatMessage>.Pop();
                            mdata.NSortId = (uint)Id;
                            mdata.NClientId = (uint)i;
                            if (RandomTool.Random(1, 2) == 1)
                            {
                                mdata.TalkMsg = TalkMsg1;
                            }
                            else
                            {
                                mdata.TalkMsg = TalkMsg2;
                            }
                            mNetClient.SendNetData(UdpNetCommand_COMMAND_TESTCHAT, mdata);
                            IMessagePool<TESTChatMessage>.recycle(mdata);

                            if (Id == nSumPackageCount)
                            {
                                string msg = DateTime.Now + " Send Chat Message: " + Id + "";
                                Console.WriteLine(msg);
                            }
                        }
                    }
                }
            }
        }
    }

    void ReceiveMessage(ClientPeerBase peer, NetPackage mPackage)
    {
        TESTChatMessage mdata = Protocol3Utility.getData<TESTChatMessage>(mPackage);

        nReceivePackageCount++;
        if (nReceivePackageCount % 10000 == 0)
        {
            string msg = $"接受包数量: {nReceivePackageCount} 总共花费时间: {mStopWatch.Elapsed.TotalSeconds},平均1秒发送：{ nReceivePackageCount / mStopWatch.Elapsed.TotalSeconds}";
            Console.WriteLine(msg);
        }

        if (nReceivePackageCount == nSumPackageCount)
        {
            string msg = "全部完成！！！！！！";
            Console.WriteLine(msg);
            LogToFile(logFileName, msg);
        }

        IMessagePool<TESTChatMessage>.recycle(mdata);
    }

    void LogToFile(string logFilePath, string Message)
    {
        using (StreamWriter writer = new StreamWriter(logFilePath, true))
        {
            writer.WriteLine(DateTime.Now  + " " + Message);
        }
    }

}

