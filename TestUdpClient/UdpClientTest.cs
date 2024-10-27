using System.Diagnostics;
using TestProtocol;
using XKNet.Common;
using XKNet.Udp.POINTTOPOINT.Client;
using XKNet.Udp.POINTTOPOINT.Common;

public class UdpClientTest
{
    public int nClientCount = 2;
    public int nPackageCount = 6;
    List<UdpNetClientMain> mClientList = new List<UdpNetClientMain>();

    System.Random mRandom = new System.Random();
    Stopwatch mStopWatch = new Stopwatch();
    public void Init()
    {
        for (int i = 0; i < nClientCount; i++)
        {
            string Name = $"Client{i}";
            string logFileName = $"Client{i}.txt";
            File.Delete(logFileName);

            UdpNetClientMain mNetClient = new UdpNetClientMain();
            mNetClient.SetName(Name);
            mClientList.Add(mNetClient);
            mNetClient.addNetListenFun(UdpNetCommand.COMMAND_TESTCHAT, ReceiveMessage);
            mNetClient.ConnectServer("127.0.0.1", 10001);
        }

        mStopWatch.Start();
    }

    double fSumTime = 0;
    Dictionary<int, int> mIdDic = new Dictionary<int, int>();
    public void Update(double fElapsedTime)
    {
        for (int i = 0; i < nClientCount; i++)
        {
            UdpNetClientMain v = mClientList[i];
            UdpNetClientMain mNetClient = v;
            mNetClient.Update(fElapsedTime);

            if(!mIdDic.ContainsKey(i))
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

                            mNetClient.SendNetData(UdpNetCommand.COMMAND_TESTCHAT, mdata);
                            IMessagePool<TESTChatMessage>.recycle(mdata);
                        }
                    }
                }
            }

        }
    }

    void ReceiveMessage(ClientPeerBase peer, NetPackage mPackage)
    {
        TESTChatMessage mdata = Protocol3Utility.getData<TESTChatMessage>(mPackage);

        string logFileName = $"Client{mdata.NClientId}.txt";
        string msg = "Receive Chat Message: " + mdata.NClientId + " | " + mdata.NSortId + "";
        LogToFile(logFileName, msg);

        if (mdata.NSortId == 1000)
        {
            Console.WriteLine($"{mdata.NClientId} 总共花费时间: {mStopWatch.Elapsed.TotalSeconds}");
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

