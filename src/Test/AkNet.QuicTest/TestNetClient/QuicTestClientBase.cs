using AKNet.Common;
using AKNet.Extentions.Protobuf;
using System.Diagnostics;
using TestCommon;
using TestProtocol;

namespace TestNetClient
{
    public abstract class QuicTestClientBase
    {
        public void Start()
        {
            NetLog.AddConsoleLog();
            Init();
            UpdateMgr.Do(Update);
        }

        public abstract QuicClientMainBase Create();
        public abstract void OnTestFinish();

        public const int nClientCount = 100;
        public const int nSingleSendPackageCount = 10000;
        public const int nSingleCleintSendMaxPackageCount = nSingleSendPackageCount * 100;
        public const double fFrameInternalTime = 0;
        public const int nSumSendPackageCount = nClientCount * nSingleCleintSendMaxPackageCount;
        public const int nSingleClientStreamCount = 2;
        int nReceivePackageCount = 0;
        int nSendPackageCount = 0;
        List<QuicClientMainBase> mClientList = new List<QuicClientMainBase>();
        Stopwatch mStopWatch = new Stopwatch();
        readonly uint[] mClientSendIdArray = new uint[nClientCount];
        readonly int[] mClientSendPackageCount = new int[nClientCount];
        readonly int[] mClientReceivePackageCount = new int[nClientCount];

        const int COMMAND_TESTCHAT = 1000;

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
            for (int i = 0; i < nClientCount; i++)
            {
                QuicClientMainBase mNetClient = Create();
                mClientList.Add(mNetClient);
                mNetClient.addNetListenFunc(COMMAND_TESTCHAT, ReceiveChatMessage);
                mNetClient.ConnectServer("127.0.0.1", 6000);
                mNetClient.SetName("C" + i);
                mNetClient.SetID((uint)i);

                mClientSendIdArray[i] = 0;
                mClientSendPackageCount[i] = 0;
                mClientReceivePackageCount[i] = 0;
            }
            
            mStopWatch.Start();
            nReceivePackageCount = 0;
            nSendPackageCount = 0;
            nLastReceiveTime = mStopWatch.ElapsedMilliseconds;
        }

        double fSumTime = 0;
        public void Update(double fElapsedTime)
        {
            for (int i = 0; i < nClientCount; i++)
            {
                var v = mClientList[i];
                var mNetClient = v;
                mNetClient.Update(fElapsedTime);
            }

            fSumTime += fElapsedTime;
            if (fSumTime > fFrameInternalTime)
            {
                fSumTime = 0;
                for (int j = 0; j < nSingleSendPackageCount; j++)
                {
                    for (int i = 0; i < nClientCount; i++)
                    {
                        var mNetClient = mClientList[i];
                        if (mNetClient.GetSocketState() == SOCKET_PEER_STATE.CONNECTED)
                        {
                            if (mClientSendPackageCount[i] < nSingleCleintSendMaxPackageCount)
                            {
                                for (byte k = 0; k < nSingleClientStreamCount; k++)
                                {
                                    TESTChatMessage mdata = IMessagePool<TESTChatMessage>.Pop();
                                    mdata.NSortId = ++mClientSendIdArray[i];
                                    mdata.NClientId = (uint)i;
                                    if (RandomTool.Random(2, 2) == 1)
                                    {
                                        mdata.TalkMsg = TalkMsg1;
                                    }
                                    else
                                    {
                                        mdata.TalkMsg = TalkMsg2;
                                    }

                                    mNetClient.SendNetData(k, COMMAND_TESTCHAT, mdata);
                                    IMessagePool<TESTChatMessage>.recycle(mdata);
                                }

                                mClientSendPackageCount[i]++;
                                if (mClientSendPackageCount[i] >= nSingleCleintSendMaxPackageCount)
                                {
                                    string msg = $"客户端{i} 全部发送完成";
                                    NetLog.Log(msg);
                                }

                                nSendPackageCount++;
                                if (nSendPackageCount >= nSumSendPackageCount)
                                {
                                    string msg = $"所有客户端 全部发送完成";
                                    NetLog.Log(msg);
                                }

                                if (mClientSendPackageCount[i] >= nSingleCleintSendMaxPackageCount ||
                                   nSendPackageCount >= nSumSendPackageCount)
                                {
                                    break;
                                }

                            }
                        }
                    }
                }
            }
        }

        long nLastReceiveTime = 0;
        bool bCallOnTestFinish = false;
        void ReceiveChatMessage(QuicClientPeerBase peer, QuicNetPackage mPackage)
        {
            TESTChatMessage mdata = Proto3Tool.GetData<TESTChatMessage>(mPackage);

            nReceivePackageCount++;
            mClientReceivePackageCount[peer.GetID()]++;

            if (nReceivePackageCount % 10000 == 0)
            {
                string msg = $"接受包数量: {nReceivePackageCount} 总共花费时间: {mStopWatch.Elapsed.TotalSeconds},平均1秒接收：{nReceivePackageCount / mStopWatch.Elapsed.TotalSeconds}";
                NetLog.Log(msg);
            }

            if (mClientReceivePackageCount[peer.GetID()] == nSingleCleintSendMaxPackageCount * nSingleClientStreamCount)
            {
                NetLog.Log($"客户端{peer.GetName()} 全部 接收 完成");
            }

            if (nReceivePackageCount == nSumSendPackageCount * nSingleClientStreamCount)
            {
                string msg = $"全部 接收完成!!!!!!";
                NetLog.Log(msg);
                OnTestFinish();
                bCallOnTestFinish = true;
            }

            IMessagePool<TESTChatMessage>.recycle(mdata);
            nLastReceiveTime = mStopWatch.ElapsedMilliseconds;
        }
    }
}

