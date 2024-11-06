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
    internal class CheckPackageMgr2: CheckPackageMgrInterface
    {
        private class CheckPackageInfo : IPoolItemInterface
        {
            private readonly CheckPackageInfo_TimeOutGenerator mTimeOutGenerator = new CheckPackageInfo_TimeOutGenerator();
            private UdpClientPeerCommonBase mClientPeer;
            private NetUdpFixedSizePackage mPackage = null;
            private CheckPackageMgr2 mMgr;
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

            public void Do(UdpClientPeerCommonBase mClientPeer, CheckPackageMgr2 mMgr, NetUdpFixedSizePackage currentCheckRTOPackage)
            {
                this.mClientPeer = mClientPeer;
                this.mPackage = currentCheckRTOPackage;
                this.mMgr = mMgr;
                ArrangeNextSend();
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
                        DelayedCallFunc();
                    }
                }
            }
        }

        private UdpClientPeerCommonBase mClientPeer;
        private readonly ObjectPool<CheckPackageInfo> mCheckPackagePool = new ObjectPool<CheckPackageInfo>();
        private readonly LinkedList<CheckPackageInfo> mWaitCheckSendQueue = new LinkedList<CheckPackageInfo>();
        private long nLastSureOrderIdTime = 0;
        private int nLastSureOrderId = 0;
        private int nContinueSameSureOrderIdCount = 0;
        private double nLastFrameTime = 0;

        private CheckPackageInfo mCurrentRTOCheckPackage = null;
        public readonly TcpStanardRTOFunc mRTOFuc = new TcpStanardRTOFunc();

        public CheckPackageMgr2(UdpClientPeerCommonBase mClientPeer)
        {
            this.mClientPeer = mClientPeer;
        }

        public void Add(NetUdpFixedSizePackage mPackage)
        {
            CheckPackageInfo mCheckPackageInfo = mCheckPackagePool.Pop();
            mWaitCheckSendQueue.AddLast(mCheckPackageInfo);

            mCheckPackageInfo.Do(this.mClientPeer, this, mPackage);
            StartRtt(mCheckPackageInfo);
        }

        public void Update(double elapsed)
        {
            nLastFrameTime = elapsed;
            double fCoef = Math.Clamp(0.1 / nLastFrameTime, 0, 1.0);
            int nSearchCount = (int)(fCoef * UdpCheckMgr.nDefaultSendPackageCount);
            nSearchCount = Math.Clamp(nSearchCount, 1, UdpCheckMgr.nDefaultSendPackageCount);
            nSearchCount = 1;
            var mNode = mWaitCheckSendQueue.First;
            while (mNode != null && nSearchCount-- > 0)
            {
                mNode.Value.Update(elapsed);
                mNode = mNode.Next;
            }
        }

        public void Reset()
        {
            foreach (var v in mWaitCheckSendQueue)
            {
                mCheckPackagePool.recycle(v);
            }
            mWaitCheckSendQueue.Clear();
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
        
        private void StartRtt(CheckPackageInfo mPackage)
        {
            if (mCurrentRTOCheckPackage == null)
            {
                mCurrentRTOCheckPackage = mPackage;
                mRTOFuc.BeginRtt();
            }
        }

        private void FinishRtt(CheckPackageInfo mPackage)
        {
            if (mCurrentRTOCheckPackage == mPackage)
            {
                mCurrentRTOCheckPackage = null;
                mRTOFuc.FinishRttSuccess();
            }
        }

        public void ReceiveOrderIdRequestPackage(ushort nRequestOrderId)
        {
            if (mClientPeer.GetSocketState() == SOCKET_PEER_STATE.CONNECTED)
            {
                bool bHit = false;
                var mNode = mWaitCheckSendQueue.First;
                int nRemoveCount = 0;
                while (mNode != null)
                {
                    if (mNode.Value.orIsMe(nRequestOrderId))
                    {
                        bHit = true;
                        break;
                    }
                    else
                    {
                        nRemoveCount++;
                    }

                    mNode = mNode.Next;
                }

                if (bHit)
                {
                    while (nRemoveCount-- > 0)
                    {
                        var mCheckPackage = mWaitCheckSendQueue.First.Value;
                        mCheckPackage.Reset();
                        mWaitCheckSendQueue.RemoveFirst();
                    }
                }
                else
                {
                    QuickReSend(nRequestOrderId);
                }
            }
        }

        public void ReceiveOrderIdSurePackage(ushort nSureOrderId)
        {
            var mNode = mWaitCheckSendQueue.First;
            while (mNode != null)
            {
                if (mNode.Value.orIsMe(nSureOrderId))
                {
                    var mRemoveCheckPackage = mNode.Value;
                    FinishRtt(mRemoveCheckPackage);
                    mWaitCheckSendQueue.Remove(mNode);
                    mCheckPackagePool.recycle(mRemoveCheckPackage);

                    break;
                }
                mNode = mNode.Next;
            }
        }
    }

}