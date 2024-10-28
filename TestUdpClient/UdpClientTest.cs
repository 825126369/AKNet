using System.Diagnostics;
using TestProtocol;
using XKNet.Common;
using XKNet.Udp.POINTTOPOINT.Client;
using XKNet.Udp.POINTTOPOINT.Common;

public class UdpClientTest
{
    public int nClientCount = 100;
    public int nPackageCount = 1;
    List<UdpNetClientMain> mClientList = new List<UdpNetClientMain>();

    System.Random mRandom = new System.Random();
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
            UdpNetClientMain mNetClient = new UdpNetClientMain();
            mNetClient.SetName("" + i);
            mClientList.Add(mNetClient);
            mNetClient.addNetListenFun(UdpNetCommand_COMMAND_TESTCHAT, ReceiveMessage);
            mNetClient.ConnectServer("127.0.0.1", 10001);
        }

        mFinishClientId.Clear();
        mStopWatch.Start();
    }

    double fSumTime = 0;
    Dictionary<int, int> mIdDic = new Dictionary<int, int>();
    public void Update(double fElapsedTime)
    {
        ProfilerTool2.TestStart();
        for (int i = 0; i < nClientCount; i++)
        {
            UdpNetClientMain v = mClientList[i];
            UdpNetClientMain mNetClient = v;
            mNetClient.Update(fElapsedTime);

            if (!mIdDic.ContainsKey(i))
            {
                mIdDic[i] = 1;
            }

            if (mNetClient.GetSocketState() == SOCKET_PEER_STATE.CONNECTED)
            {
                fSumTime += fElapsedTime;
                if (fSumTime > 0)
                {
                    fSumTime = 0;
                    for (int j = 0; j < nPackageCount; j++)
                    {
                        int Id = mIdDic[i]++;
                        if (Id <= 1000)
                        {
                            TESTChatMessage mdata = IMessagePool<TESTChatMessage>.Pop();
                            mdata.NSortId = (uint)Id;
                            mdata.NClientId = (uint)i;
                            if (mRandom.Next(1, 3) == 1)
                            {
                                mdata.TalkMsg = TalkMsg1;
                            }
                            else
                            {
                                mdata.TalkMsg = TalkMsg2;
                            }
                            mNetClient.SendNetData(UdpNetCommand_COMMAND_TESTCHAT, mdata);
                            IMessagePool<TESTChatMessage>.recycle(mdata);

                            if (Id == 1000)
                            {
                                string msg = DateTime.Now + " Send Chat Message: " + i + " | " + Id + "";
                                Console.WriteLine(msg);
                            }
                        }
                    }
                }
            }
        }

        if (fElapsedTime > 0.3)
        {
            Console.WriteLine("fElapsedTime: " + fElapsedTime);
            ProfilerTool2.TestFinishAndLog("AAAAA");
        }
    }

    void ReceiveMessage(ClientPeerBase peer, NetPackage mPackage)
    {
        TESTChatMessage mdata = Protocol3Utility.getData<TESTChatMessage>(mPackage);
        if (mdata.NSortId == 1000)
        {
            mFinishClientId.Add(mdata.NClientId);
            string msg = $"总完成客户端数量：{mFinishClientId.Count}, {mdata.NClientId} 总共花费时间: {mStopWatch.Elapsed.TotalSeconds}";
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

