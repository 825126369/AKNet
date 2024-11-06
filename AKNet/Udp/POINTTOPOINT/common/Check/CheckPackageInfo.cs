/************************************Copyright*****************************************
*        ProjectName:AKNet
*        Web:https://github.com/825126369/AKNet
*        Description:这是一个面向 .Net Standard 2.1 的游戏网络库
*        Author:阿珂
*        CreateTime:2024/11/4 20:04:54
*        Copyright:MIT软件许可证
************************************Copyright*****************************************/
using AKNet.Common;
using System;
using System.Collections.Generic;

namespace AKNet.Udp.POINTTOPOINT.Common
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

    internal class UdpCheckMgr2_CheckPackageInfo
    {
        private readonly CheckPackageInfo_TimeOutGenerator mTimeOutGenerator = new CheckPackageInfo_TimeOutGenerator();
        private bool bInPlaying = false;
        private UdpClientPeerCommonBase mClientPeer;
        private UdpCheckMgr2 mUdpCheckMgr;

        private uint nRttId = 0;
        private readonly TcpStanardRTOFunc mRTOFuc = new TcpStanardRTOFunc();
        private NetUdpFixedSizePackage currentCheckRTOPackage = null;
        private double nLastFrameTime = 0;

        private long nLastSureOrderIdTime = 0;
        private int nLastSureOrderId = 0;
        private int nContinueSameSureOrderIdCount = 0;

        public UdpCheckMgr2_CheckPackageInfo(UdpClientPeerCommonBase mClientPeer, UdpCheckMgr2 mUdpCheckMgr)
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
                int nSearchCount = UdpCheckMgr2.nDefaultSendPackageCount;
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

                    QuickReSend(nSureOrderId);
                }
                else
                {
                    QuickReSend(nSureOrderId);
                }
            }
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
                            var mSendPackage = mClientPeer.GetObjectPoolManager().NetUdpFixedSizePackage_Pop();
                            mSendPackage.CopyFrom(mCheckPackage);
                            mClientPeer.SendNetPackage(mSendPackage);
                        }
                    }
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

        private void SendPackageFunc()
        {
            double fCoef = Math.Clamp(0.3 / nLastFrameTime, 0, 1.0);
            int nSendCount = (int)(fCoef * UdpCheckMgr2.nDefaultSendPackageCount);
            nSendCount = Math.Clamp(nSendCount, 1, UdpCheckMgr2.nDefaultSendPackageCount);

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

    internal class UdpCheckMgr1_CheckPackageInfo :IPoolItemInterface
    {
        private readonly CheckPackageInfo_TimeOutGenerator mTimeOutGenerator = new CheckPackageInfo_TimeOutGenerator();
        private UdpClientPeerCommonBase mClientPeer;
        private NetUdpFixedSizePackage mPackage = null;
        private CheckPackageMgr mMgr;
        public void Reset()
        {
            if (this.mPackage != null)
            {
                mClientPeer.GetObjectPoolManager().NetUdpFixedSizePackage_Recycle(mPackage);
                this.mPackage = null;
            }

            this.mTimeOutGenerator.Reset();
            this.mClientPeer = null;
        }

        public bool orIsMe(ushort nSureOrderId)
        {
            if (nSureOrderId == mPackage.nOrderId)
            {
                return true;
            }
            return false;
        }

        public bool ReceiveCheckPackage(ushort nSureOrderId)
        {
            if (mClientPeer.GetSocketState() == SOCKET_PEER_STATE.CONNECTED)
            {
                if (nSureOrderId == mPackage.nOrderId)
                {
                    Reset();
                    return true;
                }
            }

            return false;
        }

        //快速重传
        public void QuickReSend()
        {
            var mSendPackage = mClientPeer.GetObjectPoolManager().NetUdpFixedSizePackage_Pop();
            mSendPackage.CopyFrom(mPackage);
            mClientPeer.SendNetPackage(mSendPackage);
        }

        public void Do(UdpClientPeerCommonBase mClientPeer, CheckPackageMgr mMgr, NetUdpFixedSizePackage currentCheckRTOPackage)
        {
            this.mClientPeer = mClientPeer;
            this.mPackage = currentCheckRTOPackage;
            this.mMgr = mMgr;
            DelayedCallFunc();
        }

        private void ArrangeNextSend()
        {
            long nTimeOutTime = this.mMgr.mRTOFuc.GetRTOTime();
            double fTimeOutTime = nTimeOutTime / 1000.0;
#if DEBUG
            if (fTimeOutTime >= Config.fReceiveHeartBeatTimeOut)
            {
                NetLog.Log("重发时间：" + fTimeOutTime);
            }
#endif
            mTimeOutGenerator.SetInternalTime(fTimeOutTime);
        }

        private void SendPackageFunc()
        {
            var mSendPackage = mClientPeer.GetObjectPoolManager().NetUdpFixedSizePackage_Pop();
            mSendPackage.CopyFrom(mPackage);
            mClientPeer.SendNetPackage(mSendPackage);
        }

        private void DelayedCallFunc()
        {
            if (mClientPeer.GetSocketState() == SOCKET_PEER_STATE.CONNECTED)
            {
                SendPackageFunc();
                ArrangeNextSend();
            }
        }

        public void Update(double elapsed)
        {
            if (mClientPeer.GetSocketState() == SOCKET_PEER_STATE.CONNECTED)
            {
                if (mTimeOutGenerator.orTimeOut(elapsed))
                {
                    NetLog.Log("mPackage.nOrderId: " + mPackage.nOrderId);
                    DelayedCallFunc();
                }
            }
        }
    }

    internal class CheckPackageMgr
    {
        private UdpClientPeerCommonBase mClientPeer;
        private readonly ObjectPool<UdpCheckMgr1_CheckPackageInfo> mCheckPackagePool = new ObjectPool<UdpCheckMgr1_CheckPackageInfo>();
        private readonly LinkedList<UdpCheckMgr1_CheckPackageInfo> mWaitCheckSendQueue = new LinkedList<UdpCheckMgr1_CheckPackageInfo>();
        private long nLastSureOrderIdTime = 0;
        private int nLastSureOrderId = 0;
        private int nContinueSameSureOrderIdCount = 0;
        private double nLastFrameTime = 0;

        private UdpCheckMgr1_CheckPackageInfo mCurrentRTOCheckPackage = null;
        public readonly TcpStanardRTOFunc mRTOFuc = new TcpStanardRTOFunc();

        public CheckPackageMgr(UdpClientPeerCommonBase mClientPeer)
        {
            this.mClientPeer = mClientPeer;
        }

        public void Add(NetUdpFixedSizePackage mPackage)
        {
            UdpCheckMgr1_CheckPackageInfo mCheckPackageInfo = mCheckPackagePool.Pop();
            mCheckPackageInfo.Do(this.mClientPeer, this, mPackage);
            mWaitCheckSendQueue.AddLast(mCheckPackageInfo);

            NetLog.Log("mWaitCheckSendQueue Count: " + mWaitCheckSendQueue.Count);

            StartRtt(mCheckPackageInfo);
        }

        public void Update(double elapsed)
        {
            int nSearchCount = 1;
            var mNode = mWaitCheckSendQueue.First;
            while (mNode != null && nSearchCount-- > 0)
            {
                mNode.Value.Update(elapsed);
                mNode = mNode.Next;
            }
        }

        public void Reset()
        {
            var mNode = mWaitCheckSendQueue.First;
            while (mNode != null)
            {
                var mRemoveCheckPackage = mNode.Value;
                mWaitCheckSendQueue.Remove(mNode);
                mCheckPackagePool.recycle(mRemoveCheckPackage);
                
                mNode = mNode.Next;
            }
        }

        public void ReceiveCheckPackage(ushort nSureOrderId)
        {
            if (mClientPeer.GetSocketState() == SOCKET_PEER_STATE.CONNECTED)
            {
                var mNode = mWaitCheckSendQueue.First;
                while (mNode != null)
                {
                    if (mNode.Value.ReceiveCheckPackage(nSureOrderId))
                    {
                        while (mNode != null)
                        {
                            var mRemoveCheckPackage = mNode.Value;
                            FinishRtt(mRemoveCheckPackage);
                            mWaitCheckSendQueue.Remove(mNode);
                            mCheckPackagePool.recycle(mRemoveCheckPackage);

                            mNode = mNode.Previous;
                        }
                        break;
                    }
                    mNode = mNode.Next;
                }

                QuickReSend(nSureOrderId);
            }
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
                ushort nWillSendOrderId = OrderIdHelper.AddOrderId(nSureOrderId);
                var mNode = mWaitCheckSendQueue.First;
                while (mNode != null)
                {
                    if (mNode.Value.orIsMe(nWillSendOrderId))
                    {
                        mNode.Value.QuickReSend();
                        break;
                    }
                    mNode = mNode.Next;
                }
            }

        }
        
        private void StartRtt(UdpCheckMgr1_CheckPackageInfo mPackage)
        {
            if (mCurrentRTOCheckPackage == null)
            {
                mCurrentRTOCheckPackage = mPackage;
                mRTOFuc.BeginRtt();
            }
        }

        private void FinishRtt(UdpCheckMgr1_CheckPackageInfo mPackage)
        {
            if (mCurrentRTOCheckPackage == mPackage)
            {
                mCurrentRTOCheckPackage = null;
                mRTOFuc.FinishRttSuccess();
            }
        }
    }

}