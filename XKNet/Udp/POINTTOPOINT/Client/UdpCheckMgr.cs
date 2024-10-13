//#define Server

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
                if (mAckTimeList.Count > 0)
                {
                    while (mAckTimeList.Count > 5)
                    {
                        mAckTimeList.TryDequeue(out _);
                    }

                    long nAverageTime = 0;
                    foreach (var v in mAckTimeList)
                    {
                        nAverageTime += v;
                    }
                    nAverageTime = nAverageTime / mAckTimeList.Count;

                    return (nAverageTime + 1);
                }
                return 100;
            }

            private void ArrangeNextSend()
            {
                nReSendCount++;

                long nTimeOutTime = GetAverageTime() * nReSendCount;
                DelayedCall3(nTimeOutTime);
            }

            private void DelayedCallFunc()
            {
                if (bInPlaying)
                {
                    SendNetPackageFunc(mPackage);
                    ArrangeNextSend();
                }
            }

            //5000: 81,80,80
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

            //5000: 82,81
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
            
            //5000: 82, 82
            private void DelayedCall2(long millisecondsTimeout)
            {
                mSystemThreadingTimer.Change(millisecondsTimeout, millisecondsTimeout);
            }

            private void DelayedCall2Func(object state = null)
            {
                mSystemThreadingTimer.Change(-1, -1);
                DelayedCallFunc();
            }
            
            //5000: 82
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
            
            //5000:84
            private async void DelayedCall4(long millisecondsTimeout)
            {
                mDelayedCall4CancellationTokenSource = new CancellationTokenSource();
                CancellationToken ct = mDelayedCall4CancellationTokenSource.Token;

                try
                {
                    await Task.Run(async () =>
                    {
                        await Task.Delay((int)millisecondsTimeout, ct);
                        if (!ct.IsCancellationRequested)
                        {
                            DelayedCallFunc();
                        }
                    }, ct);
                }
                catch (TaskCanceledException)
                {

                }
                catch (Exception e)
                {
                    NetLog.LogError(e);
                }
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
        private NetCombinePackage mCombinePackage = null;

        private ClientPeer mClientPeer = null;

        private readonly object lock_nCurrentWaitSendOrderId_Obj = new object();
        private readonly object lock_Check_Receive_Logic_Package_Obj = new object();

        public UdpCheckMgr(ClientPeer mClientPeer)
        {
            this.mClientPeer = mClientPeer;

            mCheckPackageInfo = new CheckPackageInfo(SendNetPackageFunc);
            mWaitCheckSendQueue = new ConcurrentQueue<NetUdpFixedSizePackage>();
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
            PackageCheckResult mPackageCheckResult = Protocol3Utility.getData<PackageCheckResult>(mPackage);

            ushort nSureOrderId = (ushort)mPackageCheckResult.NSureOrderId;
            ushort nLossOrderId = (ushort)mPackageCheckResult.NLossOrderId;
            IMessagePool<PackageCheckResult>.recycle(mPackageCheckResult);

            lock (mCheckPackageInfo)
            {
                if (mCheckPackageInfo.GetPackageOrderId() == nSureOrderId)
                {
                    mCheckPackageInfo.DoFinish(nSureOrderId);

                    NetUdpFixedSizePackage mPackage2 = null;
                    NetLog.Assert(mWaitCheckSendQueue.TryDequeue(out mPackage2));
                    NetLog.Assert(mPackage2.nOrderId == nSureOrderId);
                    ObjectPoolManager.Instance.mUdpFixedSizePackagePool.recycle(mPackage2);

                    mPackage2 = null;
                    if (mWaitCheckSendQueue.TryPeek(out mPackage2))
                    {
                        mCheckPackageInfo.Do(mPackage2);
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
                NetLog.Log("待发送的包太多了： " + mWaitCheckSendQueue.Count);
            }

            lock (mCheckPackageInfo)
            {
                if (mCheckPackageInfo.orFinish())
                {
                    mCheckPackageInfo.Do(mPackage);
                }
            }
        }

        public void MultiThreadingReceiveNetPackage(NetUdpFixedSizePackage mReceivePackage)
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

            if (UdpNetCommand.orInnerCommand(mReceivePackage.nPackageId))
            {
                ObjectPoolManager.Instance.mUdpFixedSizePackagePool.recycle(mReceivePackage);
            }
            else
            {
                SendPackageCheckResult(mReceivePackage.nOrderId);
                lock (lock_Check_Receive_Logic_Package_Obj)
                {
                    MultiThreadingCheckReceivePackageLoss(mReceivePackage);
                }
            }
        }

        private void MultiThreadingCheckReceivePackageLoss(NetUdpFixedSizePackage mPackage)
        {
            bool bIsMyWaitPackage = true;

            if (nLastReceiveOrderId > 0)
            {
                ushort nCurrentWaitReceiveOrderId = AddOrderId(nLastReceiveOrderId);
                if (mPackage.nOrderId != nCurrentWaitReceiveOrderId)
                {
                    bIsMyWaitPackage = false;
                }
                else
                {
                    nLastReceiveOrderId = mPackage.nOrderId;
                }
            }
            else
            {
                nLastReceiveOrderId = mPackage.nOrderId;
            }
            
            //上面的Lock语句保证了 包的重复性，以及 更靠后的包 进来
            if (bIsMyWaitPackage)
            {
                CheckCombinePackage(mPackage);
            }
            else
            {
                ObjectPoolManager.Instance.mUdpFixedSizePackagePool.recycle(mPackage);
            }
        }

        private void CheckCombinePackage(NetUdpFixedSizePackage mPackage)
        {
            if (mPackage.nGroupCount > 1)
            {
                mCombinePackage = ObjectPoolManager.Instance.mCombinePackagePool.Pop();
                mCombinePackage.Init(mPackage);
            }
            else if (mPackage.nGroupCount == 1)
            {
                mClientPeer.mMsgReceiveMgr.AddLogicHandleQueue(mPackage);
            }
            else if (mPackage.nGroupCount == 0)
            {
                if (mCombinePackage != null)
                {
                    mCombinePackage.Add(mPackage);
                    if (mCombinePackage.CheckCombineFinish())
                    {
                        mClientPeer.mMsgReceiveMgr.AddLogicHandleQueue(mCombinePackage);
                        mCombinePackage = null;
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

            if (mCombinePackage != null)
            {
                ObjectPoolManager.Instance.mCombinePackagePool.recycle(mCombinePackage);
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