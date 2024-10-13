#define Server

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Timers;
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
            private readonly Action<NetUdpFixedSizePackage> SendNetPackageFunc = null;
            private readonly NetUdpFixedSizePackage mPackage = null;
            private readonly Stopwatch mStopwatch = new Stopwatch();
            private readonly ConcurrentQueue<long> mAckTimeList = new ConcurrentQueue<long>();
            
            //5种定时器方法
            private readonly System.Threading.Timer mSystemThreadingTimer = null;
            private readonly System.Timers.Timer mSystemTimersTimer = null;
            private CancellationTokenSource mDelayedCall4CancellationTokenSource = null;
            private readonly Stopwatch mStopwatch2 = new Stopwatch();

            //重发数量
            private int nReSendCount = 0;
            private bool bInPlaying = false;
            public CheckPackageInfo(Action<NetUdpFixedSizePackage> SendNetPackageFunc)
            {
                this.SendNetPackageFunc = SendNetPackageFunc;
                this.mSystemThreadingTimer = new System.Threading.Timer(DelayedCall2Func);
                this.mSystemTimersTimer = new System.Timers.Timer();
                this.mSystemTimersTimer.Elapsed += DelayedCall3Func;
                this.mPackage = new NetUdpFixedSizePackage();
            }

            public void Reset()
            {
                this.mPackage.nOrderId = 0;
                this.nReSendCount = 0;
                this.mStopwatch.Reset();
                this.CancelTask();
                this.bInPlaying = false;
            }

            public ushort GetPackageOrderId()
            {
                return mPackage.nOrderId;
            }

            private void Init(NetUdpFixedSizePackage mPackage)
            {
                this.mPackage.CopyFrom(mPackage);
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
                return !bInPlaying;
            }

            public void Do(NetUdpFixedSizePackage mOtherPackage)
            {
                Init(mOtherPackage);
                mStopwatch.Start();
                bInPlaying = true;
                DelayedCallFunc();
            }

            private long GetAverageTime()
            {
                //if (mAckTimeList.Count > 0)
                //{
                //    while (mAckTimeList.Count > 5)
                //    {
                //        mAckTimeList.TryDequeue(out _);
                //    }

                //    long nAverageTime = 0;
                //    foreach (var v in mAckTimeList)
                //    {
                //        nAverageTime += v;
                //    }
                //    nAverageTime = nAverageTime / mAckTimeList.Count;

                //    return (nAverageTime + 1) * 2;
                //}
                return 100;
            }

            private void ArrangeNextSend()
            {
                nReSendCount++;

                long nTimeOutTime = GetAverageTime() * nReSendCount;
                DelayedCall2(nTimeOutTime);
            }

            private void DelayedCallFunc()
            {
                if (bInPlaying)
                {
                    NetLog.Log($"DelayedCallFunc: {mStopwatch.ElapsedMilliseconds}, {mPackage.nOrderId}");
                    SendNetPackageFunc(mPackage);
                    ArrangeNextSend();
                }
            }

            private void DelayedCall0(long millisecondsTimeout)
            {
                Task.Run(() =>
                {
                    mStopwatch2.Start();
                    while (!mDelayedCall4CancellationTokenSource.IsCancellationRequested)
                    {
                        if (mStopwatch2.ElapsedMilliseconds >= millisecondsTimeout)
                        {
                            DelayedCallFunc();
                            break;
                        }
                    }
                });
            }

            private void DelayedCall1(long millisecondsTimeout)
            {
                mDelayedCall4CancellationTokenSource = new CancellationTokenSource();
                Task.Run(() =>
                {
                    Thread.Sleep((int)millisecondsTimeout);
                    if (!mDelayedCall4CancellationTokenSource.IsCancellationRequested)
                    {
                        DelayedCallFunc();
                    }
                });
            }
            
            private void DelayedCall2(long millisecondsTimeout)
            {
                mSystemThreadingTimer.Change(millisecondsTimeout, millisecondsTimeout);
            }

            private void DelayedCall2Func(object state = null)
            {
                mSystemThreadingTimer.Change(-1, -1);
                DelayedCallFunc();
            }

            private void DelayedCall3(long millisecondsTimeout)
            {
                mSystemTimersTimer.Interval = millisecondsTimeout;
                mSystemTimersTimer.AutoReset = false;
                mSystemTimersTimer.Start();
            }

            private void DelayedCall3Func(object sender, ElapsedEventArgs e)
            {
                mSystemTimersTimer.Stop();
                DelayedCallFunc();
            }

            private async void DelayedCall4(long millisecondsTimeout)
            {
                mDelayedCall4CancellationTokenSource = new CancellationTokenSource();
                CancellationToken ct = mDelayedCall4CancellationTokenSource.Token;
                await Task.Run(async () =>
                {
                    await Task.Delay((int)millisecondsTimeout, ct);
                    if (!ct.IsCancellationRequested)
                    {
                        DelayedCallFunc();
                    }
                }, ct);
            }

            private void CancelTask()
            {
                if (mDelayedCall4CancellationTokenSource != null)
                {
                    mDelayedCall4CancellationTokenSource.Cancel();
                    mDelayedCall4CancellationTokenSource = null;
                }

                if (mSystemThreadingTimer != null)
                {
                    mSystemThreadingTimer.Change(-1, -1);
                }

                if (mSystemTimersTimer != null)
                {
                    mSystemTimersTimer.Stop();
                }

                if (mStopwatch2 != null)
                {
                    mStopwatch2.Reset();
                }
            }
        }
        
        private ushort nLastReceiveOrderId;
        private ushort nCurrentWaitSendOrderId;

        private readonly CheckPackageInfo mCheckPackageInfo = null;
        private readonly ConcurrentQueue<NetUdpFixedSizePackage> mWaitCheckSendQueue = null;
        private readonly ConcurrentQueue<NetCombinePackage> mCombinePackageQueue = null;

        private ClientPeer mClientPeer = null;

        private object lock_nCurrentWaitSendOrderId_Obj = new object();

        public UdpCheckMgr(ClientPeer mClientPeer)
        {
            this.mClientPeer = mClientPeer;

            mCheckPackageInfo = new CheckPackageInfo(SendNetPackageFunc);
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
            mClientPeer.SendInnerNetData(UdpNetCommand.COMMAND_PACKAGECHECK, mResult);
            IMessagePool<PackageCheckResult>.recycle(mResult);
        }

        private void SendNetPackageFunc(NetUdpFixedSizePackage mPackage)
        {
            this.mClientPeer.SendNetPackage(mPackage);
        }

        private void ReceiveCheckPackage(NetPackage mPackage)
        {
            NetLog.Assert(mPackage.nPackageId == UdpNetCommand.COMMAND_PACKAGECHECK);

            if (mPackage.GetMsgSpin().Length > 1024)
            {
                NetLog.LogError("ReceiveCheckPackage mPackage.GetMsgSpin().Length: " + mPackage.GetMsgSpin().Length);
            }

            PackageCheckResult mPackageCheckResult = Protocol3Utility.getData<PackageCheckResult>(mPackage);
            if (mPackageCheckResult.CalculateSize() > 1024)
            {
                NetLog.LogError("ReceiveCheckPackage CalculateSize: " + mPackageCheckResult.CalculateSize());
            }

            ushort nSureOrderId = (ushort)mPackageCheckResult.NSureOrderId;
            ushort nLossOrderId = (ushort)mPackageCheckResult.NLossOrderId;
            IMessagePool<PackageCheckResult>.recycle(mPackageCheckResult);

            lock (mWaitCheckSendQueue) //不加Lock的话，有可能多 出队 几个包
            {
                NetUdpFixedSizePackage mPeekPackage = null;
                if (mWaitCheckSendQueue.TryPeek(out mPeekPackage) && nSureOrderId == mPeekPackage.nOrderId)
                {
                    NetUdpFixedSizePackage mRemovePackage = null;
                    if (mWaitCheckSendQueue.TryDequeue(out mRemovePackage))
                    {
                        NetLog.Assert(mCheckPackageInfo.GetPackageOrderId() == mRemovePackage.nOrderId);
                        NetLog.Assert(mPeekPackage == mRemovePackage);
                        mCheckPackageInfo.DoFinish(mRemovePackage.nOrderId);
                        ObjectPoolManager.Instance.mUdpFixedSizePackagePool.recycle(mRemovePackage);

                        mPeekPackage = null;
                        if (mWaitCheckSendQueue.TryPeek(out mPeekPackage))
                        {
                            mCheckPackageInfo.Do(mPeekPackage);
                        }
                    }
                }
            }
        }

        public void SendLogicPackage(UInt16 id, ReadOnlySpan<byte> buffer)
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
                    mPackage.Length = Config.nUdpPackageFixedHeadSize;
                    mPackage.CopyFromMsgStream(buffer, nBeginIndex, readBytes);

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
                mCheckPackageInfo.Do(mPackage);
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
                bool bAdd = false;
                NetCombinePackage currentGroup = null;
                if (mCombinePackageQueue.Count > 0)
                {
                    foreach (var v in mCombinePackageQueue)
                    {
                        currentGroup = v;
                        if (currentGroup.Add(mPackage))
                        {
                            bAdd = true;
                            break;
                        }
                    }
                }

                if (bAdd)
                {
                    currentGroup = null;
                    if (mCombinePackageQueue.TryPeek(out currentGroup))
                    {
                        if (currentGroup.CheckCombineFinish())
                        {
                            if (mCombinePackageQueue.TryDequeue(out currentGroup))
                            {
                                mClientPeer.mMsgReceiveMgr.AddLogicHandleQueue(currentGroup);
                            }
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