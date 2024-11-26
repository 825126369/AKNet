/************************************Copyright*****************************************
*        ProjectName:AKNet
*        Web:https://github.com/825126369/AKNet
*        Description:这是一个面向 .Net Standard 2.1 的游戏网络库
*        Author:阿珂
*        CreateTime:2024/11/23 22:12:37
*        Copyright:MIT软件许可证
************************************Copyright*****************************************/
using AKNet.Common;
using System;

namespace AKNet.Udp.POINTTOPOINT.Common
{
    internal class ReSendPackageMgr3 : ReSendPackageMgrInterface
    {
        private UdpClientPeerCommonBase mClientPeer;

        private readonly AkLinkedList<NetUdpFixedSizePackage> mWaitCheckSendQueue = new AkLinkedList<NetUdpFixedSizePackage>(100);

        private long nLastRequestOrderIdTime = 0;
        private int nLastRequestOrderId = 0;
        private int nContinueSameRequestOrderIdCount = 0;
        private double nLastFrameTime = 0;
        private int nSearchCount = 0;
        private int nMaxSearchCount = UdpCheckMgr.nDefaultSendPackageCount;
        private int nRemainNeedSureCount = 0;

        public ReSendPackageMgr3(UdpClientPeerCommonBase mClientPeer)
        {
            this.mClientPeer = mClientPeer;
            this.nSearchCount = 1;
        }

        public void Add(NetUdpFixedSizePackage mPackage)
        {
            mWaitCheckSendQueue.AddLast(mPackage);
            ArrangeNextSend(mPackage);
        }

        private void CheckOrderIdRepeated(NetUdpFixedSizePackage mPackage)
        {
#if DEBUG
            int nSearchCount = UdpCheckMgr.nDefaultSendPackageCount;
            var mNode = mWaitCheckSendQueue.First;
            while (mNode != null && nSearchCount-- > 0)
            {
                if (mNode.Value.nOrderId == mPackage.nOrderId)
                {
                    NetLog.LogError($"OrderId Not Enough：{Config.nUdpMinOrderId}-{Config.nUdpMaxOrderId}, {mWaitCheckSendQueue.First.Value.nOrderId}-{mWaitCheckSendQueue.Last.Value.nOrderId}-{mWaitCheckSendQueue.Count}, {mPackage.nOrderId}");
                }
                mNode = mNode.Next;
            }
#endif
        }

        //private int SetSearchCount()
        //{
        //    double fLastFrameTime = this.nLastFrameTime;
        //    double fReSendRate = UdpStatistical.GetReSendRate();

        //    double fCoef1 = Math.Clamp(0.1 / fLastFrameTime, 0, 1.0);
        //    double fCoef2 = Math.Clamp(1 - fReSendRate, 0, 1.0);
        //    double fCoef = Math.Min(fCoef1, fCoef2);

        //    fCoef = fCoef1;
        //    int nSearchCount = (int)(fCoef * UdpCheckMgr.nDefaultSendPackageCount);
        //    nSearchCount = Math.Max(nSearchCount, 1);

        //    if (nSearchCount > this.nSearchCount)
        //    {
        //        this.nSearchCount *= 2;
        //    }
        //    else if (nSearchCount < this.nSearchCount)
        //    {
        //        this.nSearchCount--;
        //    }

        //    this.nSearchCount = nSearchCount;
        //    this.nSearchCount = Math.Clamp(this.nSearchCount, 1, UdpCheckMgr.nDefaultSendPackageCount);
        //    return this.nSearchCount;
        //}

        public void Update(double elapsed)
        {
            nLastFrameTime = elapsed;
            int nSearchCount = this.nSearchCount;
            var mNode = mWaitCheckSendQueue.First;
            while (mNode != null && nSearchCount-- > 0)
            {
                NetUdpFixedSizePackage mPackage = mNode.Value;
                if (mPackage.mTimeOutGenerator_ReSend.orTimeOut(elapsed))
                {
                    this.nSearchCount = 1;
                    SendNetPackage(mPackage);
                    ArrangeNextSend(mPackage);
                }
                mNode = mNode.Next;
            }
        }

        public void Reset()
        {
            MainThreadCheck.Check();
            var mNode = mWaitCheckSendQueue.First;
            while (mNode != null)
            {
                var mRemovePackage = mNode.Value;
                mClientPeer.GetObjectPoolManager().NetUdpFixedSizePackage_Recycle(mRemovePackage);
                mNode = mNode.Next;
            }
            mWaitCheckSendQueue.Clear();
        }

        private void ArrangeNextSend(NetUdpFixedSizePackage mPackage)
        {
            long nTimeOutTime = mClientPeer.GetTcpStanardRTOFunc().GetRTOTime();
            double fTimeOutTime = nTimeOutTime / 1000.0;

            if (fTimeOutTime > 3.0)
            {
                NetLog.Log("重发时间: " + fTimeOutTime);
            }

            mPackage.mTimeOutGenerator_ReSend.SetInternalTime(fTimeOutTime);
        }

        //快速重传
        private void QuickReSend(ushort nRequestOrderId)
        {
            if (nRequestOrderId != nLastRequestOrderId)
            {
                nContinueSameRequestOrderIdCount = 0;
                nLastRequestOrderId = nRequestOrderId;
            }

            nContinueSameRequestOrderIdCount++;
            if (nContinueSameRequestOrderIdCount > 3)
            {
                if (UdpStaticCommon.GetNowTime() - nLastRequestOrderIdTime > 5)
                {
                    nLastRequestOrderIdTime = UdpStaticCommon.GetNowTime();

                    var mNode = mWaitCheckSendQueue.First;
                    while (mNode != null)
                    {
                        var mPackage = mNode.Value;
                        if (mPackage.nOrderId == nRequestOrderId)
                        {
                            SendNetPackage(mPackage);

                            this.nMaxSearchCount = this.nSearchCount / 2;
                            this.nSearchCount = this.nSearchCount / 2 + 3;
                            break;
                        }
                        mNode = mNode.Next;
                    }
                }
            }
        }

        public void ReceiveOrderIdRequestPackage(ushort nRequestOrderId)
        {
            bool bHit = false;
            var mNode = mWaitCheckSendQueue.First;
            var nSearchCount = UdpCheckMgr.nDefaultSendPackageCount;
            int nRemoveCount = 0;
            while (mNode != null && nSearchCount-- > 0)
            {
                var mPackage = mNode.Value;
                if (mPackage.nOrderId == nRequestOrderId)
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
                    var mPackage = mWaitCheckSendQueue.First.Value;
                    mPackage.mTcpStanardRTOTimer.FinishRtt(mClientPeer);
                    mClientPeer.GetObjectPoolManager().NetUdpFixedSizePackage_Recycle(mPackage);
                    mWaitCheckSendQueue.RemoveFirst();
                    Sure();
                }

                QuickReSend(nRequestOrderId);
            }
        }

        public void ReceiveOrderIdSurePackage(ushort nSureOrderId)
        {
            var nSearchCount = UdpCheckMgr.nDefaultCacheReceivePackageCount;
            var mNode = mWaitCheckSendQueue.First;
            while (mNode != null && nSearchCount > 0)
            {
                var mPackage = mNode.Value;
                if (mPackage.nOrderId == nSureOrderId)
                {
                    mPackage.mTcpStanardRTOTimer.FinishRtt(mClientPeer);
                    mWaitCheckSendQueue.Remove(mNode);
                    mClientPeer.GetObjectPoolManager().NetUdpFixedSizePackage_Recycle(mPackage);
                    Sure();
                    break;
                }
                mNode = mNode.Next;
            }
        }

        private void Sure()
        {
            this.nRemainNeedSureCount--;
            if (this.nSearchCount < this.nMaxSearchCount)
            {
                this.nSearchCount++;
            }
            else if (this.nRemainNeedSureCount <= 0)
            {
                this.nSearchCount++;
                this.nRemainNeedSureCount = this.nMaxSearchCount;
            }
        }

        private void SendNetPackage(NetUdpFixedSizePackage mCheckPackage)
        {
            mClientPeer.SendNetPackage(mCheckPackage);
            UdpStatistical.AddReSendCheckPackageCount();
        }
    }

}