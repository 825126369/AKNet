//#define Server

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using UdpPointtopointProtocols;
using XKNet.Common;
using XKNet.Udp.POINTTOPOINT.Common;

#if Server
namespace XKNet.Udp.POINTTOPOINT.Server
#else
namespace XKNet.Udp.POINTTOPOINT.Client
#endif
{
    // 用 并发 Queue 实现 
    internal class UdpCheckMgr
    {
        internal class CheckPackageInfo
        {
            private Action<NetUdpFixedSizePackage> SendNetPackageFunc = null;
            private NetUdpFixedSizePackage mPackage = null;
            private int nReSendCount = 0;
            private Stopwatch mStopwatch = new Stopwatch();
            private readonly Queue<long> mAckTimeList = new Queue<long>();

            public CheckPackageInfo()
            {
                
            }

            public void Reset()
            {
                this.mPackage = null;
                this.SendNetPackageFunc = null;
                this.nReSendCount = 0;
                this.mStopwatch.Reset();
            }

            public NetUdpFixedSizePackage GetPackage()
            {
                return mPackage;
            }

            private void Init(Action<NetUdpFixedSizePackage> SendNetPackageFunc, NetUdpFixedSizePackage mPackage)
            {
                this.mPackage = mPackage;
                this.SendNetPackageFunc = SendNetPackageFunc;
                this.nReSendCount = 0;
            }

            public void DoFinish(int nWaitSureOrderId = -1)
            {
                if (nWaitSureOrderId >= 0)
                {
                    NetLog.Assert(mPackage.nOrderId == nWaitSureOrderId, $"CheckPackageInfo DoFinsih Error !!!: {mPackage.nOrderId} | {nWaitSureOrderId}");
                }

                long nSpendTime = mStopwatch.ElapsedMilliseconds;
                NetLog.Log($"{nSpendTime}, {mPackage.nOrderId}");
                mAckTimeList.Enqueue(nSpendTime);

                this.Reset();
            }

            public bool orFinish()
            {
                return mPackage == null;
            }

            public void Do(Action<NetUdpFixedSizePackage> SendNetPackageFunc, NetUdpFixedSizePackage mPackage)
            {
                Init(SendNetPackageFunc, mPackage);

                mStopwatch.Start();
                SendNetPackageFunc(mPackage);
                ArrangeNextSend();
            }

            private long GetAverageTime()
            {
                if (mAckTimeList.Count > 0)
                {
                    long nAverageTime = 0;
                    foreach (var v in mAckTimeList)
                    {
                        nAverageTime += v;
                    }
                    nAverageTime = nAverageTime / mAckTimeList.Count;

                    while (mAckTimeList.Count > 5)
                    {
                        mAckTimeList.Dequeue();
                    }
                }

                return 10;
            }

            private void ArrangeNextSend()
            {
                nReSendCount++;

                int nTimeOutTime = 20 * nReSendCount;
                DelayedCall(nTimeOutTime);
            }

            private void DelayedCall(int millisecondsTimeout)
            {
                Task.Run(() =>
                {
                    Thread.Sleep(millisecondsTimeout);
                    if (mPackage != null && SendNetPackageFunc != null)
                    {
                        SendNetPackageFunc(mPackage);
                        ArrangeNextSend();
                    }
                });
            }
        }

        private const double fReSendTimeOut = 1.0;
        private ushort nLastReceiveOrderId;
        private ushort nCurrentWaitSendOrderId;

        private CheckPackageInfo mCheckPackageInfo = null;
        private ConcurrentQueue<NetUdpFixedSizePackage> mWaitCheckSendQueue = null;
        private ConcurrentQueue<NetCombinePackage> mCombinePackageQueue = null;

        private ClientPeer mClientPeer = null;

        private object lock_nCurrentWaitSendOrderId_Obj = new object();

        public UdpCheckMgr(ClientPeer mClientPeer)
        {
            this.mClientPeer = mClientPeer;

            mCheckPackageInfo = new CheckPackageInfo();
            mWaitCheckSendQueue = new ConcurrentQueue<NetUdpFixedSizePackage>();
            mCombinePackageQueue = new ConcurrentQueue<NetCombinePackage>();

            nCurrentWaitSendOrderId = Config.nUdpMinOrderId;
            nLastReceiveOrderId = 0;
        }

        private ushort AddOrderId(ushort nOrderId)
        {
            nOrderId++;
            if (nOrderId > Config.nUdpMaxOrderId)
            {
                nOrderId = 1;
            }
            return nOrderId;
        }

        private void AddSendPackageOrderId()
        {
            lock (lock_nCurrentWaitSendOrderId_Obj)
            {
                nCurrentWaitSendOrderId = AddOrderId(nCurrentWaitSendOrderId);
            }
        }

        private void SendPackageCheckResult(uint nSureOrderId, uint nLossOrderId = 0)
        {
            PackageCheckResult mResult = IMessagePool<PackageCheckResult>.Pop();
            mResult.NSureOrderId = nSureOrderId;
            mResult.NLossOrderId = nLossOrderId;
            NetUdpFixedSizePackage mPackage = mClientPeer.GetUdpSystemPackage(UdpNetCommand.COMMAND_PACKAGECHECK, mResult);
            mClientPeer.SendNetPackage(mPackage);
            IMessagePool<PackageCheckResult>.recycle(mResult);
        }

        private void ReceiveCheckPackage(NetPackage mPackage)
        {
            NetLog.Assert(mPackage.nPackageId == UdpNetCommand.COMMAND_PACKAGECHECK);

            PackageCheckResult mPackageCheckResult = Protocol3Utility.getData<PackageCheckResult>(mPackage);
            ushort nSureOrderId = (ushort)mPackageCheckResult.NSureOrderId;
            ushort nLossOrderId = (ushort)mPackageCheckResult.NLossOrderId;
            IMessagePool<PackageCheckResult>.recycle(mPackageCheckResult);

            NetUdpFixedSizePackage mPeekPackage = null;
            if (mWaitCheckSendQueue.TryPeek(out mPeekPackage))
            {
                if (nSureOrderId != mPeekPackage.nOrderId)
                {
                    return;
                }

                NetUdpFixedSizePackage mRemovePackage = null;
                if (mWaitCheckSendQueue.TryDequeue(out mRemovePackage))
                {
                    NetLog.Assert(mCheckPackageInfo.GetPackage() == mRemovePackage);
                    NetLog.Assert(mPeekPackage == mRemovePackage);
                    mCheckPackageInfo.DoFinish(mRemovePackage.nOrderId);

                    ObjectPoolManager.Instance.mUdpFixedSizePackagePool.recycle(mRemovePackage);
                }

                mPeekPackage = null;
                if (mWaitCheckSendQueue.TryPeek(out mPeekPackage))
                {
                    mCheckPackageInfo.Do(SendNetPackageFunc, mPeekPackage);
                }
            }
        }

        public void SendLogicPackage(UInt16 id, Span<byte> buffer)
        {
            NetLog.Assert(buffer.Length <= Config.nUdpCombinePackageFixedSize);
            NetLog.Assert(UdpNetCommand.orNeedCheck(id));
            if (!buffer.IsEmpty)
            {
                int readBytes = 0;
                int nBeginIndex = 0;

                UInt16 groupCount = 0;
                if (buffer.Length % Config.nUdpPackageFixedBodySize == 0)
                {
                    groupCount = (UInt16)(buffer.Length / Config.nUdpPackageFixedBodySize);
                }
                else
                {
                    groupCount = (UInt16)(buffer.Length / Config.nUdpPackageFixedBodySize + 1);
                }

                while (nBeginIndex < buffer.Length)
                {
                    if (nBeginIndex + Config.nUdpPackageFixedBodySize > buffer.Length)
                    {
                        readBytes = buffer.Length - nBeginIndex;
                    }
                    else
                    {
                        readBytes = Config.nUdpPackageFixedBodySize;
                    }

                    var mPackage = ObjectPoolManager.Instance.mUdpFixedSizePackagePool.Pop();
                    mPackage.nOrderId = (UInt16)nCurrentWaitSendOrderId;
                    mPackage.nGroupCount = groupCount;
                    mPackage.nPackageId = id;
                    mPackage.Length = readBytes + Config.nUdpPackageFixedHeadSize;

                    for (int i = 0; i < readBytes; i++)
                    {
                        mPackage.buffer[Config.nUdpPackageFixedHeadSize + i] = buffer[nBeginIndex + i];
                    }

                    groupCount = 0;
                    nBeginIndex += readBytes;

                    NetPackageEncryption.Encryption(mPackage);
                    AddSendPackageOrderId();
                    AddSendCheck(mPackage);
                }
            }
            else
            {
                var mPackage = ObjectPoolManager.Instance.mUdpFixedSizePackagePool.Pop();
                mPackage.nOrderId = (UInt16)nCurrentWaitSendOrderId;
                mPackage.nGroupCount = 1;
                mPackage.nPackageId = id;
                mPackage.Length = Config.nUdpPackageFixedHeadSize;

                NetPackageEncryption.Encryption(mPackage);
                AddSendPackageOrderId();
                AddSendCheck(mPackage);
            }
        }

        private void AddSendCheck(NetUdpFixedSizePackage mPackage)
        {
            NetLog.Assert(mPackage.nOrderId > 0);
            mWaitCheckSendQueue.Enqueue(mPackage);

            if (mWaitCheckSendQueue.Count > Config.nUdpMaxOrderId / 2)
            {
                NetLog.LogError("待发送的包太多了： " + mWaitCheckSendQueue.Count);
            }

            if (mCheckPackageInfo.orFinish())
            {
                mCheckPackageInfo.Do(SendNetPackageFunc, mPackage);
            }
        }

        public void ReceivePackage(NetUdpFixedSizePackage mReceivePackage)
        {
            this.mClientPeer.mUDPLikeTCPMgr.ReceiveHeartBeat();

            if (mReceivePackage.nPackageId == UdpNetCommand.COMMAND_PACKAGECHECK)
            {
                ReceiveCheckPackage(mReceivePackage);
            }
            else if (mReceivePackage.nPackageId == UdpNetCommand.COMMAND_CONNECT)
            {
                this.mClientPeer.mUDPLikeTCPMgr.ReceiveConnect();
            }
            else if (mReceivePackage.nPackageId == UdpNetCommand.COMMAND_DISCONNECT)
            {
                this.mClientPeer.mUDPLikeTCPMgr.ReceiveDisConnect();
            }

            if (UdpNetCommand.orNeedCheck(mReceivePackage.nPackageId))
            {
                CheckReceivePackageLoss(mReceivePackage);
            }
            else
            {
                ObjectPoolManager.Instance.mUdpFixedSizePackagePool.recycle(mReceivePackage);
            }
        }

        private void CheckReceivePackageLoss(NetUdpFixedSizePackage mPackage)
        {
            SendPackageCheckResult(mPackage.nOrderId);
            if (nLastReceiveOrderId > 0)
            {
                ushort nCurrentWaitReceiveOrderId = AddOrderId(nLastReceiveOrderId);
                if (mPackage.nOrderId != nCurrentWaitReceiveOrderId)
                {
                    //NetLog.Log("Server 等包: " + mPackage.nPackageId + " | " + mPackage.nOrderId + " | " + mPackage.nGroupCount + " | " + nCurrentWaitReceiveOrderId);
                    return;
                }
            }

            nLastReceiveOrderId = mPackage.nOrderId;
            CheckCombinePackage(mPackage);
        }

        private void CheckCombinePackage(NetUdpFixedSizePackage mPackage)
        {
            lock (mCombinePackageQueue)
            {
                if (mPackage.nGroupCount > 1)
                {
                    NetCombinePackage cc = ObjectPoolManager.Instance.mCombinePackagePool.Pop();
                    cc.Init(mPackage);
                    mCombinePackageQueue.Enqueue(cc);
                }
                else if (mPackage.nGroupCount == 1)
                {
                    mClientPeer.mMsgReceiveMgr.AddLogicHandleQueue(mPackage);
                }
                else if (mPackage.nGroupCount == 0)
                {
                    NetCombinePackage currentGroup = null;
                    if (mCombinePackageQueue.TryPeek(out currentGroup))
                    {
                        currentGroup.Add(mPackage);
                        ObjectPoolManager.Instance.mUdpFixedSizePackagePool.recycle(mPackage);
                        if (currentGroup.CheckCombineFinish())
                        {
                            if (mCombinePackageQueue.TryDequeue(out currentGroup))
                            {
                                mClientPeer.mMsgReceiveMgr.AddLogicHandleQueue(currentGroup);
                            }
                        }
                    }
                    else
                    {
                        //残包 直接舍弃
                        ObjectPoolManager.Instance.mUdpFixedSizePackagePool.recycle(mPackage);
                    }
                }
                else
                {
                    NetLog.Assert(false);
                }
            }
        }

        private void SendNetPackageFunc(NetUdpFixedSizePackage mPackage)
        {
            this.mClientPeer.SendNetPackage(mPackage);
        }

        public void Reset()
        {
            NetUdpFixedSizePackage mRemovePackage = null;
            while (mWaitCheckSendQueue.TryDequeue(out mRemovePackage))
            {
                ObjectPoolManager.Instance.mUdpFixedSizePackagePool.recycle(mRemovePackage);
            }

            NetCombinePackage mRemovePackage2 = null;
            while (mCombinePackageQueue.TryDequeue(out mRemovePackage2))
            {
                ObjectPoolManager.Instance.mCombinePackagePool.recycle(mRemovePackage2);
            }

            mCheckPackageInfo.Reset();

            nCurrentWaitSendOrderId = Config.nUdpMinOrderId;
            nLastReceiveOrderId = 0;
        }

        public void Release()
        {
            Reset();
        }
    }
}