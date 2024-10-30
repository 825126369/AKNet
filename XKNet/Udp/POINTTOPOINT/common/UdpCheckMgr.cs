/************************************Copyright*****************************************
*        ProjectName:XKNet
*        Web:https://github.com/825126369/XKNet
*        Description:XKNet 网络库, 兼容 C#8.0 和 .Net Standard 2.1
*        Author:阿珂
*        CreateTime:2024/10/30 12:14:19
*        Copyright:MIT软件许可证
************************************Copyright*****************************************/
using Google.Protobuf.Collections;
using System;
using System.Collections.Generic;
using XKNet.Common;

namespace XKNet.Udp.POINTTOPOINT.Common
{
    internal class UdpCheckMgr
    {
        internal class CheckPackageInfo_TimeOutGenerator
        {
            double fTime = 0;
            double fInternalTime = 0;
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
                if (this.fTime >= fInternalTime)
                {
                    this.Reset();
                    return true;
                }

                return false;
            }
        }

        internal class CheckPackageInfo
        {
            private readonly CheckPackageInfo_TimeOutGenerator mTimeOutGenerator = new CheckPackageInfo_TimeOutGenerator();
            private bool bInPlaying = false;
            private UdpClientPeerCommonBase mClientPeer;
            private UdpCheckMgr mUdpCheckMgr;
            private readonly TcpStanardRTOFunc mRTOFuc = new TcpStanardRTOFunc();
            private NetUdpFixedSizePackage currentCheckRTOPackage = null;
            private double nLastFrameTime = 0;

            private long nLastSureOrderIdTime = 0;
            private int nLastSureOrderId = 0;
            private int nContinueSameSureOrderIdCount = 0;

            public CheckPackageInfo(UdpClientPeerCommonBase mClientPeer, UdpCheckMgr mUdpCheckMgr)
            {
                this.mClientPeer = mClientPeer;
                this.mUdpCheckMgr = mUdpCheckMgr;
            }

            public void Reset()
            {
                this.bInPlaying = false;
                this.mTimeOutGenerator.Reset();
                this.currentCheckRTOPackage = null;
            }

            public void ReceiveCheckPackage(ushort nSureOrderId)
            {
                if (mClientPeer.GetSocketState() == SOCKET_PEER_STATE.CONNECTED)
                {
                    bool bHaveOrderId = false;
                    int nSearchCount = nDefaultSendPackageCount;
                    var mQueueIter = mUdpCheckMgr.mWaitCheckSendQueue.GetEnumerator();
                    int nRemoveCount = 0;
                    while (mQueueIter.MoveNext() && nSearchCount-- > 0)
                    {
                        nRemoveCount++;
                        if (mQueueIter.Current.nOrderId == nSureOrderId)
                        {
                            bHaveOrderId = true;
                            break;
                        }
                    }

                    if (bHaveOrderId)
                    {
                        while (nRemoveCount-- > 0)
                        {
                            var mCheckPackage = mUdpCheckMgr.mWaitCheckSendQueue.Dequeue();
                            FinishRtt(mCheckPackage);
                            mClientPeer.GetObjectPoolManager().NetUdpFixedSizePackage_Recycle(mCheckPackage);
                        }
                    }
                    else
                    {
                        QuickReSend(nSureOrderId);
                    }
                }
            }

            private void StartRtt(NetUdpFixedSizePackage mCheckPackage)
            {
                if (currentCheckRTOPackage == null)
                {
                    currentCheckRTOPackage = mCheckPackage;
                    mRTOFuc.BeginRtt();
                }
            }

            private void FinishRtt(NetUdpFixedSizePackage mCheckPackage)
            {
                if (mCheckPackage == currentCheckRTOPackage)
                {
                    mRTOFuc.FinishRttSuccess();
                    currentCheckRTOPackage = null;
                }
            }

            public void Do()
            {
                if (!this.bInPlaying)
                {
                    DelayedCallFunc();
                }
            }

            private void ArrangeNextSend()
            {
                long nTimeOutTime = mRTOFuc.GetRTOTime();
                double fTimeOutTime = nTimeOutTime / 1000.0;
#if DEBUG
                if (fTimeOutTime >= Config.fReceiveHeartBeatTimeOut)
                {
                    NetLog.Log("重发时间：" + fTimeOutTime);
                }
#endif
                mTimeOutGenerator.SetInternalTime(fTimeOutTime);
            }

            //快速重传
            private void QuickReSend(ushort nSureOrderId)
            {
                if (nSureOrderId != nLastSureOrderId)
                {
                    nContinueSameSureOrderIdCount = 0;
                    nLastSureOrderId = nSureOrderId;
                    nLastSureOrderIdTime = UdpStaticCommon.GetNowTime();
                }

                nContinueSameSureOrderIdCount++;
                if (nContinueSameSureOrderIdCount > 3)
                {
                    //if (UdpStaticCommon.GetNowTime() - nLastSureOrderIdTime < mRTOFuc.GetRTOTime())
                    {
                        nContinueSameSureOrderIdCount = 0;
                        if (mUdpCheckMgr.mWaitCheckSendQueue.TryPeek(out NetUdpFixedSizePackage mCheckPackage))
                        {
                            if (mCheckPackage.nOrderId == OrderIdHelper.AddOrderId(nSureOrderId))
                            {
                                StartRtt(mCheckPackage);
                                var mSendPackage = mClientPeer.GetObjectPoolManager().NetUdpFixedSizePackage_Pop();
                                mSendPackage.CopyFrom(mCheckPackage);
                                mClientPeer.SendNetPackage(mSendPackage);
                            }
                        }
                    }
                }
            }

            private void SendPackageFunc()
            {
                double fCoef = Math.Clamp(0.3 / nLastFrameTime, 0, 1.0);
                int nSendCount = (int)(fCoef * nDefaultSendPackageCount);

                var mQueueIter = mUdpCheckMgr.mWaitCheckSendQueue.GetEnumerator();
                while (mQueueIter.MoveNext() && nSendCount-- > 0)
                {
                    var mCheckPackage = mQueueIter.Current;
                    StartRtt(mCheckPackage);

                    var mSendPackage = mClientPeer.GetObjectPoolManager().NetUdpFixedSizePackage_Pop();
                    mSendPackage.CopyFrom(mCheckPackage);
                    mClientPeer.SendNetPackage(mSendPackage);
                }
            }

            private void DelayedCallFunc()
            {
                if (mClientPeer.GetSocketState() == SOCKET_PEER_STATE.CONNECTED)
                {
                    this.bInPlaying = mUdpCheckMgr.mWaitCheckSendQueue.Count > 0;
                    if (this.bInPlaying)
                    {
                        SendPackageFunc();
                        ArrangeNextSend();
                    }
                }
                else
                {
                    bInPlaying = false;
                }
            }

            public void Update(double elapsed)
            {
                nLastFrameTime = elapsed;
                if (bInPlaying && mClientPeer.GetSocketState() == SOCKET_PEER_STATE.CONNECTED)
                {
                    if (mTimeOutGenerator.orTimeOut(elapsed))
                    {
                        DelayedCallFunc();
                    }
                }
            }
        }

        public const int nDefaultSendPackageCount = 20;
        public const int nDefaultCacheReceivePackageCount = 50;
        private ushort nCurrentWaitSendOrderId;
        private ushort nCurrentWaitReceiveOrderId;
        private ushort nTellMyWaitReceiveOrderId;

        private readonly CheckPackageInfo mCheckPackageInfo = null;
        private readonly Queue<NetUdpFixedSizePackage> mWaitCheckSendQueue = null;
        private NetCombinePackage mCombinePackage = null;

        private UdpClientPeerCommonBase mClientPeer = null;
        public UdpCheckMgr(UdpClientPeerCommonBase mClientPeer)
        {
            this.mClientPeer = mClientPeer;

            mCheckPackageInfo = new CheckPackageInfo(mClientPeer, this);
            mWaitCheckSendQueue = new Queue<NetUdpFixedSizePackage>();
            nCurrentWaitSendOrderId = Config.nUdpMinOrderId;
            nCurrentWaitReceiveOrderId = Config.nUdpMinOrderId;
        }

        private void AddSendPackageOrderId()
        {
            nCurrentWaitSendOrderId = OrderIdHelper.AddOrderId(nCurrentWaitSendOrderId);
        }

        private void AddReceivePackageOrderId()
        {
            nTellMyWaitReceiveOrderId = nCurrentWaitReceiveOrderId;
            nCurrentWaitReceiveOrderId = OrderIdHelper.AddOrderId(nCurrentWaitReceiveOrderId);
        }

        public void SetSureOrderId(NetUdpFixedSizePackage mPackage)
        {
            mPackage.nSureOrderId = nTellMyWaitReceiveOrderId;
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

                    var mPackage = mClientPeer.GetObjectPoolManager().NetUdpFixedSizePackage_Pop();
                    mPackage.nOrderId = (UInt16)nCurrentWaitSendOrderId;
                    mPackage.nGroupCount = groupCount;
                    mPackage.nPackageId = id;
                    mPackage.Length = Config.nUdpPackageFixedHeadSize;
                    mPackage.CopyFromMsgStream(buffer, nBeginIndex, readBytes);

                    groupCount = 0;
                    nBeginIndex += readBytes;
                    AddSendPackageOrderId();
                    AddSendCheck(mPackage);
                }
            }
            else
            {
                var mPackage = mClientPeer.GetObjectPoolManager().NetUdpFixedSizePackage_Pop();
                mPackage.nOrderId = (UInt16)nCurrentWaitSendOrderId;
                mPackage.nGroupCount = 1;
                mPackage.nPackageId = id;
                mPackage.Length = Config.nUdpPackageFixedHeadSize;
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

            mCheckPackageInfo.Do();
        }

        public void ReceiveNetPackage(NetUdpFixedSizePackage mReceivePackage)
        {
            this.mClientPeer.ReceiveHeartBeat();
            if (mReceivePackage.nSureOrderId > 0)
            {
                mCheckPackageInfo.ReceiveCheckPackage(mReceivePackage.nSureOrderId);
            }

            if (mReceivePackage.nPackageId == UdpNetCommand.COMMAND_PACKAGECHECK)
            {

            }
            else if (mReceivePackage.nPackageId == UdpNetCommand.COMMAND_HEARTBEAT)
            {

            }
            else if (mReceivePackage.nPackageId == UdpNetCommand.COMMAND_CONNECT)
            {
                this.mClientPeer.ReceiveConnect();
            }
            else if (mReceivePackage.nPackageId == UdpNetCommand.COMMAND_DISCONNECT)
            {
                this.mClientPeer.ReceiveDisConnect();
            }

            if (UdpNetCommand.orInnerCommand(mReceivePackage.nPackageId))
            {
                mClientPeer.GetObjectPoolManager().NetUdpFixedSizePackage_Recycle(mReceivePackage);
            }
            else
            {
                if (mClientPeer.GetSocketState() == SOCKET_PEER_STATE.CONNECTED)
                {
                    CheckReceivePackageLoss(mReceivePackage);
                }
                else
                {
                    mClientPeer.GetObjectPoolManager().NetUdpFixedSizePackage_Recycle(mReceivePackage);
                }
            }
        }

        readonly List<NetUdpFixedSizePackage> mCacheReceivePackageList = new List<NetUdpFixedSizePackage>(nDefaultCacheReceivePackageCount);
        private void CheckReceivePackageLoss(NetUdpFixedSizePackage mPackage)
        {
            if (mPackage.nOrderId == nCurrentWaitReceiveOrderId)
            {
                AddReceivePackageOrderId();
                CheckCombinePackage(mPackage);

                mPackage = mCacheReceivePackageList.Find((x) => x.nOrderId == nCurrentWaitReceiveOrderId);
                while (mPackage != null)
                {
                    mCacheReceivePackageList.Remove(mPackage);
                    AddReceivePackageOrderId();
                    CheckCombinePackage(mPackage);
                    mPackage = mCacheReceivePackageList.Find((x) => x.nOrderId == nCurrentWaitReceiveOrderId);
                }

                for (int i = mCacheReceivePackageList.Count - 1; i >= 0; i--)
                {
                    var mTempPackage = mCacheReceivePackageList[i];
                    if (mTempPackage.nOrderId <= nCurrentWaitReceiveOrderId)
                    {
                        mClientPeer.GetObjectPoolManager().NetUdpFixedSizePackage_Recycle(mTempPackage);
                        mCacheReceivePackageList.RemoveAt(i);
                    }
                }
            }
            else
            {
                //NetLog.Log("CheckReceivePackageLoss: " + mPackage.nOrderId + " | " + nTellMyWaitReceiveOrderId + " | " + nCurrentWaitReceiveOrderId);
                if (OrderIdHelper.orInOrderIdFront(nCurrentWaitSendOrderId, mPackage.nOrderId, nDefaultCacheReceivePackageCount) &&
                    mCacheReceivePackageList.Find(x => x.nOrderId == mPackage.nOrderId) == null &&
                    mCacheReceivePackageList.Count < mCacheReceivePackageList.Capacity)
                {
                    mCacheReceivePackageList.Add(mPackage);
                }
                else
                {
                    mClientPeer.GetObjectPoolManager().NetUdpFixedSizePackage_Recycle(mPackage);
                }
            }
            nTellMyWaitReceiveOrderId = OrderIdHelper.MinusOrderId(nCurrentWaitReceiveOrderId);
        }

        private void CheckCombinePackage(NetUdpFixedSizePackage mPackage)
        {
            if (mPackage.nGroupCount > 1)
            {
                mCombinePackage = mClientPeer.GetObjectPoolManager().NetCombinePackage_Pop();
                mCombinePackage.Init(mPackage);
                mClientPeer.GetObjectPoolManager().NetUdpFixedSizePackage_Recycle(mPackage);
            }
            else if (mPackage.nGroupCount == 1)
            {
                mClientPeer.AddLogicHandleQueue(mPackage);
            }
            else if (mPackage.nGroupCount == 0)
            {
                if (mCombinePackage != null)
                {
                    if (mCombinePackage.Add(mPackage))
                    {
                        if (mCombinePackage.CheckCombineFinish())
                        {
                            mClientPeer.AddLogicHandleQueue(mCombinePackage);
                            mCombinePackage = null;
                        }
                    }
                    else
                    {
                        //残包
                        NetLog.Assert(false, "残包");
                    }
                }
                else
                {
                    //残包
                    NetLog.Assert(false, "残包");
                }
                mClientPeer.GetObjectPoolManager().NetUdpFixedSizePackage_Recycle(mPackage);
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

        public void OnUpdateEnd(double elapsed)
        {
            if (nTellMyWaitReceiveOrderId > 0)
            {
                mClientPeer.SendInnerNetData(UdpNetCommand.COMMAND_PACKAGECHECK);
                nTellMyWaitReceiveOrderId = 0;
            }
        }

        public void Reset()
        {
            if (mCombinePackage != null)
            {
                mClientPeer.GetObjectPoolManager().NetCombinePackage_Recycle(mCombinePackage);
                mCombinePackage = null;
            }

            while (mWaitCheckSendQueue.Count > 0)
            {
                NetUdpFixedSizePackage mRemovePackage = mWaitCheckSendQueue.Dequeue();
                mClientPeer.GetObjectPoolManager().NetUdpFixedSizePackage_Recycle(mRemovePackage);
            }

            while (mCacheReceivePackageList.Count > 0)
            {
                int nRemoveIndex = mCacheReceivePackageList.Count - 1;
                NetUdpFixedSizePackage mRemovePackage = mCacheReceivePackageList[nRemoveIndex];
                mCacheReceivePackageList.RemoveAt(nRemoveIndex);
                mClientPeer.GetObjectPoolManager().NetUdpFixedSizePackage_Recycle(mRemovePackage);
            }

            mCheckPackageInfo.Reset();
            nCurrentWaitReceiveOrderId = Config.nUdpMinOrderId;
            nCurrentWaitSendOrderId = Config.nUdpMinOrderId;
            nTellMyWaitReceiveOrderId = 0;
        }

        public void Release()
        {

        }
    }
}