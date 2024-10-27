//#define Server

using System;
using System.Collections.Generic;
using System.Diagnostics;
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
        internal class CheckPackageInfo_TimeOutGenerator
        {
            double fTime = 0;
            double fInternalTime = 0;
            uint mToken = 0;
            public CheckPackageInfo_TimeOutGenerator(double fInternalTime = 1.0)
            {
                SetToken(0);
                SetInternalTime(fInternalTime);
            }

            public void SetToken(uint mToken)
            {
                this.mToken = mToken;
            }

            public bool CheckTokenOk(uint mToken)
            {
                return this.mToken == mToken;
            }

            public void SetInternalTime(double fInternalTime)
            {
                this.fInternalTime = fInternalTime;
                this.Reset();
            }

            public void Reset()
            {
                this.fTime = 0.0;
            }

            public bool orTimeOut(double fElapsed)
            {
                this.fTime += fElapsed;
                if (this.fTime > fInternalTime)
                {
                    this.Reset();
                    return true;
                }

                return false;
            }
        }

        internal class CheckPackageInfo
        {
            private readonly Action<NetUdpFixedSizePackage> SendNetPackageFunc = null;
            private readonly Stopwatch mStopwatch = new Stopwatch();
            private readonly Queue<long> mAckTimeList = new Queue<long>();
            private readonly CheckPackageInfo_TimeOutGenerator mTimeOutGenerator = new CheckPackageInfo_TimeOutGenerator();
            private uint nTimeOutToken = 1;
            //重发数量
            private int nReSendCount = 0;
            private bool bInPlaying = false;
            private NetUdpFixedSizePackage mPackage = null;
            private ClientPeer mClientPeer;

            public CheckPackageInfo(ClientPeer mClientPeer, Action<NetUdpFixedSizePackage> SendNetPackageFunc)
            {
                this.mClientPeer = mClientPeer;
                this.SendNetPackageFunc = SendNetPackageFunc;
            }

            public void Reset()
            {
                this.mPackage = null;
                this.nReSendCount = 0;
                this.mStopwatch.Reset();
                this.bInPlaying = false;
                this.nTimeOutToken++;
                this.mTimeOutGenerator.Reset();
            }

            public NetUdpFixedSizePackage GetPackage()
            {
                return mPackage;
            }

            public void DoFinish()
            {
                long nSpendTime = mStopwatch.ElapsedMilliseconds;
                mAckTimeList.Enqueue(nSpendTime / nReSendCount);
                this.Reset();
            }

            public bool orFinish()
            {
                return !bInPlaying;
            }

            public void Do(NetUdpFixedSizePackage mOtherPackage)
            {
                this.mPackage = mOtherPackage;
                this.nReSendCount = 0;
                this.mStopwatch.Start();
                this.bInPlaying = true;
                this.nTimeOutToken++;
                
                mTimeOutGenerator.SetToken(this.nTimeOutToken);
                DelayedCallFunc();
            }

            private long GetMinTime()
            {
                if (mAckTimeList.Count > 0)
                {
                    while (mAckTimeList.Count > 3)
                    {
                        mAckTimeList.Dequeue();
                    }

                    long nMinTime = long.MaxValue;
                    foreach (var v in mAckTimeList)
                    {
                        if (v < nMinTime)
                        {
                            nMinTime = v;
                        }
                    }
                    return nMinTime;
                }
                return 100;
            }

            private long GetAverageTime()
            {
                if (mAckTimeList.Count > 0)
                {
                    while (mAckTimeList.Count > 3)
                    {
                        mAckTimeList.Dequeue();
                    }

                    long nAverageTime = 0;
                    foreach (var v in mAckTimeList)
                    {
                        nAverageTime += v;
                    }
                    return nAverageTime / mAckTimeList.Count + 1;
                }
                return 100;
            }

            private void ArrangeNextSend()
            {
                nReSendCount++;
                long nTimeOutTime = GetAverageTime() * nReSendCount;
#if DEBUG
                if (nTimeOutTime >= Config.fReceiveHeartBeatTimeOut * 1000)
                {
                    NetLog.Log("重发时间：" + nTimeOutTime + " | " + nReSendCount);
                }
#endif
                mTimeOutGenerator.SetInternalTime(nTimeOutTime * 1000);
            }

            private void DelayedCallFunc()
            {
                if (bInPlaying && mClientPeer.GetSocketState() == SOCKET_PEER_STATE.CONNECTED)
                {
                    SendNetPackageFunc(mPackage);
                    ArrangeNextSend();
                }
            }

            public void Update(double elapsed)
            {
                if (bInPlaying)
                {
                    if (mTimeOutGenerator.orTimeOut(elapsed))
                    {
                        if (mTimeOutGenerator.CheckTokenOk(this.nTimeOutToken))
                        {
                            DelayedCallFunc();
                        }
                    }
                }
            }
        }

        private ushort nLastReceiveOrderId;
        private ushort nCurrentWaitSendOrderId;

        private readonly CheckPackageInfo mCheckPackageInfo = null;
        private readonly Queue<NetUdpFixedSizePackage> mWaitCheckSendQueue = null;
        private NetCombinePackage mCombinePackage = null;

        private ClientPeer mClientPeer = null;
        public UdpCheckMgr(ClientPeer mClientPeer)
        {
            this.mClientPeer = mClientPeer;

            mCheckPackageInfo = new CheckPackageInfo(mClientPeer, SendNetPackageFunc);
            mWaitCheckSendQueue = new Queue<NetUdpFixedSizePackage>();
            nCurrentWaitSendOrderId = Config.nUdpMinOrderId;
            nLastReceiveOrderId = 0;
        }

        private void AddSendPackageOrderId()
        {
            nCurrentWaitSendOrderId = OrderIdHelper.AddOrderId(nCurrentWaitSendOrderId);
        }

        private void SendPackageCheckResult(ushort nSureOrderId)
        {
            mClientPeer.SendInnerNetData(UdpNetCommand.COMMAND_PACKAGECHECK, nSureOrderId);
        }

        private void SendNetPackageFunc(NetUdpFixedSizePackage mPackage)
        {
            this.mClientPeer.SendNetPackage(mPackage);
        }

        private void ReceiveCheckPackage(ushort nSureOrderId)
        {
            if (!mCheckPackageInfo.orFinish())
            {
                NetUdpFixedSizePackage mCheckPackage = mCheckPackageInfo.GetPackage();
                if (mCheckPackage.nOrderId == nSureOrderId)
                {
                    mCheckPackageInfo.DoFinish();
                    NetUdpFixedSizePackage mPackage2 = mWaitCheckSendQueue.Dequeue();
                    NetLog.Assert(mPackage2 == mCheckPackage);
                    ObjectPoolManager.Instance.mUdpFixedSizePackagePool.recycle(mPackage2);

                    NetUdpFixedSizePackage mSendPackage = null;
                    if (mWaitCheckSendQueue.TryPeek(out mSendPackage))
                    {
                        mCheckPackageInfo.Do(mSendPackage);
                    }
                }
            }
        }

        public void SendLogicPackage(UInt16 id, ReadOnlySpan<byte> buffer)
        {
            if (this.mClientPeer.GetSocketState() != SOCKET_PEER_STATE.CONNECTED) return;

            NetLog.Assert(buffer.Length <= Config.nMsgPackageBufferMaxLength, "超出允许的最大包尺寸：" + Config.nMsgPackageBufferMaxLength);
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

            if (mCheckPackageInfo.orFinish())
            {
                mCheckPackageInfo.Do(mPackage);
            }
        }

        public void ReceiveNetPackage(NetUdpFixedSizePackage mReceivePackage)
        {
            if (mReceivePackage.nPackageId == UdpNetCommand.COMMAND_PACKAGECHECK)
            {
                ushort nSureOrderId = mReceivePackage.nOrderId;
                ReceiveCheckPackage(nSureOrderId);
            }
            if (mReceivePackage.nPackageId == UdpNetCommand.COMMAND_HEARTBEAT)
            {
                this.mClientPeer.mUDPLikeTCPMgr.ReceiveHeartBeat();
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
                mReceivePackage.remoteEndPoint = null;
                ObjectPoolManager.Instance.mUdpFixedSizePackagePool.recycle(mReceivePackage);
            }
            else
            {
                if (mClientPeer.GetSocketState() == SOCKET_PEER_STATE.CONNECTED)
                {
                    SendPackageCheckResult(mReceivePackage.nOrderId);
                    CheckReceivePackageLoss(mReceivePackage);
                }
                else
                {
                    mReceivePackage.remoteEndPoint = null;
                    ObjectPoolManager.Instance.mUdpFixedSizePackagePool.recycle(mReceivePackage);
                }
            }
        }

        private void CheckReceivePackageLoss(NetUdpFixedSizePackage mPackage)
        {
            bool bIsMyWaitPackage = true;

            if (nLastReceiveOrderId > 0)
            {
                ushort nCurrentWaitReceiveOrderId = OrderIdHelper.AddOrderId(nLastReceiveOrderId);
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
                ObjectPoolManager.Instance.mUdpFixedSizePackagePool.recycle(mPackage);
            }
            else if (mPackage.nGroupCount == 1)
            {
                mClientPeer.mMsgReceiveMgr.AddLogicHandleQueue(mPackage);
            }
            else if (mPackage.nGroupCount == 0)
            {
                if (mCombinePackage != null)
                {
                    if (mCombinePackage.Add(mPackage))
                    {
                        if (mCombinePackage.CheckCombineFinish())
                        {
                            mClientPeer.mMsgReceiveMgr.AddLogicHandleQueue(mCombinePackage);
                            mCombinePackage = null;
                        }
                    }
                    else
                    {
                        //残包
                        nLastReceiveOrderId = 0;
                        ObjectPoolManager.Instance.mCombinePackagePool.recycle(mCombinePackage);
                        mCombinePackage = null;
                    }
                }
                else
                {
                    //残包
                    nLastReceiveOrderId = 0;
                }
                ObjectPoolManager.Instance.mUdpFixedSizePackagePool.recycle(mPackage);
            }
            else
            {
                NetLog.Assert(false);
            }
        }

        public void Update(double elapsed)
        {
            mCheckPackageInfo.Update(elapsed);
        }

        public void Reset()
        {
            if (mCombinePackage != null)
            {
                ObjectPoolManager.Instance.mCombinePackagePool.recycle(mCombinePackage);
                mCombinePackage = null;
            }
            
            while (mWaitCheckSendQueue.Count > 0)
            {
                NetUdpFixedSizePackage mRemovePackage = mWaitCheckSendQueue.Dequeue();
                ObjectPoolManager.Instance.mUdpFixedSizePackagePool.recycle(mRemovePackage);
            }

            mCheckPackageInfo.Reset();

            nLastReceiveOrderId = 0;
            nCurrentWaitSendOrderId = Config.nUdpMinOrderId;
        }

        public void Release()
        {

        }
    }
}