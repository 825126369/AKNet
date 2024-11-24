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
    internal class ReSendPackageMgr2: ReSendPackageMgrInterface
    {
        private class CheckPackageInfo : IPoolItemInterface
        {
            private readonly CheckPackageInfo_TimeOutGenerator mTimeOutGenerator = new CheckPackageInfo_TimeOutGenerator();
            private UdpClientPeerCommonBase mClientPeer;
            public NetUdpFixedSizePackage mPackage = null;
            private ReSendPackageMgr2 mMgr;
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

            //快速重传
            public void QuickReSend()
            {
                mMgr.SendNetPackage(mPackage);
            }

            public void Do(UdpClientPeerCommonBase mClientPeer, ReSendPackageMgr2 mMgr, NetUdpFixedSizePackage currentCheckRTOPackage)
            {
                this.mClientPeer = mClientPeer;
                this.mPackage = currentCheckRTOPackage;
                this.mMgr = mMgr;
                ArrangeNextSend();
            }

            private void ArrangeNextSend()
            {
                long nTimeOutTime = mClientPeer.GetTcpStanardRTOFunc().GetRTOTime();
                double fTimeOutTime = nTimeOutTime / 1000.0;
#if DEBUG
                if (fTimeOutTime >= 3)
                {
                    NetLog.Log("重发时间：" + fTimeOutTime);
                }
#endif
                mTimeOutGenerator.SetInternalTime(fTimeOutTime);
            }

            private void DelayedCallFunc()
            {
                if (mClientPeer.GetSocketState() == SOCKET_PEER_STATE.CONNECTED)
                {
                    mMgr.SendNetPackage(mPackage);
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
        private readonly AkLinkedList<CheckPackageInfo> mWaitCheckSendQueue = new AkLinkedList<CheckPackageInfo>(100);
        private long nLastRequestOrderIdTime = 0;
        private int nLastRequestOrderId = 0;
        private int nContinueSameRequestOrderIdCount = 0;
        private double nLastFrameTime = 0;

        public ReSendPackageMgr2(UdpClientPeerCommonBase mClientPeer)
        {
            this.mClientPeer = mClientPeer;
        }

        public void Add(NetUdpFixedSizePackage mPackage)
        {
            CheckOrderIdRepeated(mPackage);
            CheckPackageInfo mCheckPackageInfo = mCheckPackagePool.Pop();
            mWaitCheckSendQueue.AddLast(mCheckPackageInfo);
            mCheckPackageInfo.Do(this.mClientPeer, this, mPackage);
        }

        private void CheckOrderIdRepeated(NetUdpFixedSizePackage mPackage)
        {
#if DEBUG
            int nSearchCount = UdpCheckMgr.nDefaultSendPackageCount;
            var mNode = mWaitCheckSendQueue.First;
            while (mNode != null && nSearchCount-- > 0)
            {
                if (mNode.Value.mPackage.nOrderId == mPackage.nOrderId)
                {
                    NetLog.LogError($"OrderId Not Enough：{Config.nUdpMinOrderId}-{Config.nUdpMaxOrderId}, {mWaitCheckSendQueue.First.Value.mPackage.nOrderId}-{mWaitCheckSendQueue.Last.Value.mPackage.nOrderId}-{mWaitCheckSendQueue.Count}, {mPackage.nOrderId}");
                }
                mNode = mNode.Next;
            }
#endif
        }

        public void Update(double elapsed)
        {
            nLastFrameTime = elapsed;
            double fCoef = Math.Clamp(0.1 / nLastFrameTime, 0, 1.0);
            int nSearchCount = (int)(fCoef * UdpCheckMgr.nDefaultSendPackageCount);
            nSearchCount = Math.Clamp(nSearchCount, 1, UdpCheckMgr.nDefaultSendPackageCount);

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
                var mRemovePackage = mNode.Value;
                mCheckPackagePool.recycle(mRemovePackage);
                mNode = mNode.Next;
            }
            mWaitCheckSendQueue.Clear();
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
                        if (mNode.Value.orIsMe(nRequestOrderId))
                        {
                            mNode.Value.QuickReSend();
                            break;
                        }
                        mNode = mNode.Next;
                    }
                }
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
                        mCheckPackage.mPackage.mTcpStanardRTOTimer.FinishRtt(mClientPeer);
                        mCheckPackagePool.recycle(mCheckPackage);
                        mWaitCheckSendQueue.RemoveFirst();
                    }

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
                    mRemoveCheckPackage.mPackage.mTcpStanardRTOTimer.FinishRtt(mClientPeer);
                    mWaitCheckSendQueue.Remove(mNode);
                    mCheckPackagePool.recycle(mRemoveCheckPackage);

                    break;
                }
                mNode = mNode.Next;
            }
        }

        public void SendNetPackage(NetUdpFixedSizePackage mCheckPackage)
        {
            mCheckPackage.mTcpStanardRTOTimer.BeginRtt();
            var mSendPackage = mClientPeer.GetObjectPoolManager().NetUdpFixedSizePackage_Pop();
            mSendPackage.CopyFrom(mCheckPackage);
            mClientPeer.SendNetPackage(mSendPackage);
            UdpStatistical.AddReSendCheckPackageCount();
        }
    }

}