﻿using AKNet.Common;
using AKNet.Extentions.Protobuf;
using System.Diagnostics;
using TestCommon;
using TestProtocol;

namespace TestNetClient
{
    public class NetHandler
    {
        public const int nClientCount = 100;
        public const int nSingleSendPackageCount = 100;
        public const int nSingleCleintSendMaxPackageCount = nSingleSendPackageCount * 100;
        public const double fFrameInternalTime = 0;
        public const int nSumSendPackageCount = nClientCount * nSingleCleintSendMaxPackageCount;
        int nReceivePackageCount = 0;
        int nSendPackageCount = 0;
        List<NetClientMain> mClientList = new List<NetClientMain>();
        Stopwatch mStopWatch = new Stopwatch();
        readonly uint[] mClientSendIdArray = new uint[nClientCount];
        readonly int[] mClientSendPackageCount = new int[nClientCount];
        const int COMMAND_TESTCHAT = 1000;
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
                mNetClient.addNetListenFunc(COMMAND_TESTCHAT, ReceiveMessage);
                mNetClient.ConnectServer("127.0.0.1", 6000);

                mClientSendIdArray[i] = 0;
                mClientSendPackageCount[i] = 0;
            }
            
            mStopWatch.Start();
            nReceivePackageCount = 0;
            nSendPackageCount = 0;
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

                                TESTChatMessage mdata = IMessagePool<TESTChatMessage>.Pop();
                                mdata.NSortId = ++mClientSendIdArray[i];
                                mdata.NClientId = (uint)i;
                                if (RandomTool.Random(1, 2) == 1)
                                {
                                    mdata.TalkMsg = TalkMsg1;
                                }
                                else
                                {
                                    mdata.TalkMsg = TalkMsg2;
                                }

                                mNetClient.SendNetData(COMMAND_TESTCHAT, mdata);
                                IMessagePool<TESTChatMessage>.recycle(mdata);

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

        void ReceiveMessage(ClientPeerBase peer, NetPackage mPackage)
        {
            TESTChatMessage mdata = Proto3Tool.GetData<TESTChatMessage>(mPackage);

            nReceivePackageCount++;
            if (nReceivePackageCount % 1000 == 0)
            {
                string msg = $"接受包数量: {nReceivePackageCount} 总共花费时间: {mStopWatch.Elapsed.TotalSeconds},平均1秒发送：{nReceivePackageCount / mStopWatch.Elapsed.TotalSeconds}";
                NetLog.Log(msg);
            }

            if (nReceivePackageCount == nSumSendPackageCount)
            {
                string msg = $"全部 接收完成!!!!!!";
                NetLog.Log(msg);
            }

            IMessagePool<TESTChatMessage>.recycle(mdata);
        }
    }
}

