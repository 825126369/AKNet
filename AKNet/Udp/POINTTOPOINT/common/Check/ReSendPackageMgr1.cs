﻿/************************************Copyright*****************************************
*        ProjectName:AKNet
*        Web:https://github.com/825126369/AKNet
*        Description:这是一个面向 .Net Standard 2.1 的游戏网络库
*        Author:阿珂
*        CreateTime:2024/11/7 21:38:44
*        Copyright:MIT软件许可证
************************************Copyright*****************************************/
using AKNet.Common;
using System;

namespace AKNet.Udp.POINTTOPOINT.Common
{
    internal class ReSendPackageMgr1 : ReSendPackageMgrInterface
    {
        private class CheckPackageInfo
        {
            private readonly CheckPackageInfo_TimeOutGenerator mTimeOutGenerator = new CheckPackageInfo_TimeOutGenerator();
            private bool bInPlaying = false;
            private UdpClientPeerCommonBase mClientPeer;
            private ReSendPackageMgr1 mMgr;
            private double nLastFrameTime = 0;

            public CheckPackageInfo(UdpClientPeerCommonBase mClientPeer, ReSendPackageMgr1 mMgr)
            {
                this.mClientPeer = mClientPeer;
                this.mMgr = mMgr;
            }

            public void Reset()
            {
                this.bInPlaying = false;
                this.mTimeOutGenerator.Reset();
            }

            public void Do()
            {
                if (!this.bInPlaying)
                {
                    this.bInPlaying = true;
                    ArrangeNextSend();
                }
            }

            private void ArrangeNextSend()
            {
                long nTimeOutTime = TcpStanardRTOFunc.GetRTOTime();
                double fTimeOutTime = nTimeOutTime / 1000.0;
#if DEBUG
                if (fTimeOutTime >= Config.fReceiveHeartBeatTimeOut)
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
                    this.bInPlaying = mMgr.mWaitCheckSendQueue.Count > 0;
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

            private void SendPackageFunc()
            {
                double fCoef = Math.Clamp(0.1 / nLastFrameTime, 0, 1.0);
                int nSendCount = (int)(fCoef * UdpCheckMgr.nDefaultSendPackageCount);
                nSendCount = Math.Clamp(nSendCount, 1, UdpCheckMgr.nDefaultSendPackageCount);

                var mQueueIter = mMgr.mWaitCheckSendQueue.First;
                while (mQueueIter != null && nSendCount-- > 0)
                {
                    var mCheckPackage = mQueueIter.Value;
                    mMgr.SendNetPackage(mCheckPackage);
                    mQueueIter = mQueueIter.Next;
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

        private UdpClientPeerCommonBase mClientPeer;
        private readonly CheckPackageInfo mCheckPackageInfo = null;
        public readonly AkLinkedList<NetUdpFixedSizePackage> mWaitCheckSendQueue = null;

        private long nLastRequestOrderIdTime = 0;
        private int nLastRequestOrderId = 0;
        private int nContinueSameRequestOrderIdCount = 0;

        public ReSendPackageMgr1(UdpClientPeerCommonBase mClientPeer)
        {
            this.mClientPeer = mClientPeer;
            mCheckPackageInfo = new CheckPackageInfo(mClientPeer, this);
            mWaitCheckSendQueue = new AkLinkedList<NetUdpFixedSizePackage>();
        }

        public void Add(NetUdpFixedSizePackage mPackage)
        {
            CheckOrderIdRepeated(mPackage);
            mWaitCheckSendQueue.AddLast(mPackage);
            mCheckPackageInfo.Do();
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

        public void Update(double elapsed)
        {
            mCheckPackageInfo.Update(elapsed);
        }

        public void Reset()
        {
            mCheckPackageInfo.Reset();

            var mNode = mWaitCheckSendQueue.First;
            while (mNode != null)
            {
                var mRemovePackage = mNode.Value;
                mClientPeer.GetObjectPoolManager().NetUdpFixedSizePackage_Recycle(mRemovePackage);
                mNode = mNode.Next;
            }
            mWaitCheckSendQueue.Clear();
        }

        public void ReceiveOrderIdSurePackage(ushort nSureOrderId)
        {
            var mNode = mWaitCheckSendQueue.First;
            while (mNode != null)
            {
                var mCheckPackage = mNode.Value;
                if (mCheckPackage.nOrderId == nSureOrderId)
                {
                    mCheckPackage.mTcpStanardRTOTimer.FinishRtt();
                    mClientPeer.GetObjectPoolManager().NetUdpFixedSizePackage_Recycle(mCheckPackage);
                    mWaitCheckSendQueue.Remove(mNode);
                    break;
                }
                mNode = mNode.Next;
            }
        }

        public void ReceiveOrderIdRequestPackage(ushort nRequestOrderId)
        {
            if (mClientPeer.GetSocketState() == SOCKET_PEER_STATE.CONNECTED)
            {
                bool bHit = false;
                var mQueueIter = mWaitCheckSendQueue.First;
                int nRemoveCount = 0;
                while (mQueueIter != null)
                {
                    if (mQueueIter.Value.nOrderId == nRequestOrderId)
                    {
                        bHit = true;
                        break;
                    }
                    else
                    {
                        nRemoveCount++;
                    }
                    mQueueIter = mQueueIter.Next;
                }

                if (bHit)
                {
                    while (nRemoveCount-- > 0)
                    {
                        var mCheckPackage = mWaitCheckSendQueue.First.Value;
                        mCheckPackage.mTcpStanardRTOTimer.FinishRtt();
                        mClientPeer.GetObjectPoolManager().NetUdpFixedSizePackage_Recycle(mCheckPackage);
                        mWaitCheckSendQueue.RemoveFirst();
                    }

                    QuickReSend(nRequestOrderId);
                }
            }
        }

        //快速重传
        private void QuickReSend(ushort nRequestOrderId)
        {
            if (mWaitCheckSendQueue.Count == 0) return;

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

                    int nSearchCount = UdpCheckMgr.nDefaultSendPackageCount;
                    var mQueueIter = mWaitCheckSendQueue.First;
                    while (mQueueIter != null && nSearchCount-- > 0)
                    {
                        var mCheckPackage = mQueueIter.Value;
                        if (mCheckPackage.nOrderId == nRequestOrderId)
                        {
                            SendNetPackage(mCheckPackage);
                            break;
                        }

                        mQueueIter = mQueueIter.Next;
                    }
                }
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