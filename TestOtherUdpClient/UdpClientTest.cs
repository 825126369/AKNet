using LiteNetLib;
using System.Diagnostics;
using System.Text;

public class UdpClientTest
{
    public const int nClientCount = 10;
    public const int nPackageCount = 30;
    public const int nSumPackageCount = nClientCount * 100;
    int nReceivePackageCount = 0;
    List<NetManager> mClientList = new List<NetManager>();
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
            EventBasedNetListener listener = new EventBasedNetListener();
            NetManager mNetClient = new NetManager(listener);
            mNetClient.Start();
            mNetClient.Connect("127.0.0.1", 9050, "SomeConnectionKey");
            mClientList.Add(mNetClient);
            listener.NetworkReceiveEvent += ReceiveMessage;
        }

        mFinishClientId.Clear();
        mStopWatch.Start();
        nReceivePackageCount = 0;
    }

    double fSumTime = 0;
    uint Id = 0;
    public void Update(double fElapsedTime)
    {
        for (int i = 0; i < nClientCount; i++)
        {
            var mNetClient = mClientList[i];
            mNetClient.PollEvents();
            fSumTime += fElapsedTime;
            if (fSumTime > 0)
            {
                fSumTime = 0;
                for (int j = 0; j < nPackageCount; j++)
                {
                    Id++;
                    if (Id <= nSumPackageCount)
                    {
                        mNetClient.FirstPeer.Send(UTF8Encoding.UTF8.GetBytes(TalkMsg2), DeliveryMethod.ReliableOrdered);
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

    void ReceiveMessage(NetPeer peer, NetPacketReader reader, byte channel, DeliveryMethod deliveryMethod)
    {
        string Data = reader.GetString(1024 * 8);
        Console.WriteLine(Data);

        nReceivePackageCount++;
        if (nReceivePackageCount % 1000 == 0)
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
    }

    void LogToFile(string logFilePath, string Message)
    {
        using (StreamWriter writer = new StreamWriter(logFilePath, true))
        {
            writer.WriteLine(DateTime.Now  + " " + Message);
        }
    }

}

