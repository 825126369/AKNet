/************************************Copyright*****************************************
*        ProjectName:AKNet
*        Web:https://github.com/825126369/AKNet
*        Description:C#游戏网络库
*        Author:许珂
*        StartTime:2024/11/01 00:00:00
*        ModifyTime:2025/11/29 4:33:43
*        Copyright:MIT软件许可证
************************************Copyright*****************************************/
using AKNet.Common;
using System;
using System.Collections.Generic;

namespace AKNet.Udp2Tcp.Common
{
    internal class ReSendPackageMgr : ReSendPackageMgrInterface
    {
        private UdpClientPeerCommonBase mClientPeer;
        private UdpCheckMgr mUdpCheckMgr;

        private readonly LinkedList<NetUdpFixedSizePackage> mWaitCheckSendQueue = new LinkedList<NetUdpFixedSizePackage>();
        public ushort nCurrentWaitSendOrderId;

        private long nLastRequestOrderIdTime = 0;
        private int nLastRequestOrderId = 0;
        private int nContinueSameRequestOrderIdCount = 0;
        private double nLastFrameTime = 0;
        private int nSearchCount = 0;
        private int nMaxSearchCount = int.MaxValue;
        private int nRemainNeedSureCount = 0;

        public ReSendPackageMgr(UdpClientPeerCommonBase mClientPeer, UdpCheckMgr mUdpCheckMgr)
        {
            this.mClientPeer = mClientPeer;
            this.mUdpCheckMgr = mUdpCheckMgr;
            
            this.nSearchCount = 4;
            nCurrentWaitSendOrderId = Config.nUdpMinOrderId;
        }

        public void AddSendPackageOrderId()
        {
            nCurrentWaitSendOrderId = OrderIdHelper.AddOrderId(nCurrentWaitSendOrderId);
        }
        
        private void AddCheckPackage()
        {
            while (mUdpCheckMgr.GetSendStreamList().Length > 0)
            {
                NetUdpFixedSizePackage mPackage = mClientPeer.GetObjectPoolManager().NetUdpFixedSizePackage_Pop();
                mPackage.nOrderId = nCurrentWaitSendOrderId;
                int nLength = mUdpCheckMgr.GetSendStreamList().WriteToMax(0, mPackage.buffer, Config.nUdpPackageFixedHeadSize, Config.nUdpPackageFixedBodySize);
                mPackage.Length = Config.nUdpPackageFixedHeadSize + nLength;
                mWaitCheckSendQueue.AddLast(mPackage.mEntry);
                mPackage.mTimeOutGenerator_ReSend.Reset();
                AddSendPackageOrderId();
            }
        }

        public void Update(double elapsed)
        {
            if (!Config.bUdpCheck) return;
            AddCheckPackage();

            if (mWaitCheckSendQueue.Count > 0)
            {
                UdpStatistical.AddSearchCount(this.nSearchCount);
                nLastFrameTime = elapsed;
                bool bTimeOut = false;
                int nSearchCount = this.nSearchCount;

                var mNode = mWaitCheckSendQueue.First;
                while (mNode != null && nSearchCount-- > 0)
                {
                    NetUdpFixedSizePackage mPackage = mNode.Value;
                    if (mPackage.mTimeOutGenerator_ReSend.orSetInternalTime())
                    {
                        if (mPackage.mTimeOutGenerator_ReSend.orTimeOut(elapsed))
                        {
                            UdpStatistical.AddReSendCheckPackageCount();
                            SendNetPackage(mPackage);
                            ArrangeNextSend(mPackage);
                            bTimeOut = true;
                        }
                    }
                    else
                    {
                        UdpStatistical.AddFirstSendCheckPackageCount();
                        SendNetPackage(mPackage);
                        ArrangeNextSend(mPackage);
                    }
                    mNode = mNode.Next;
                }

                if (bTimeOut)
                {
                    this.nMaxSearchCount = this.nSearchCount - 1;
                    this.nSearchCount = Math.Max(1, this.nSearchCount / 2);
                }
            }
        }

        public void Reset()
        {
            MainThreadCheck.Check();
            nCurrentWaitSendOrderId = Config.nUdpMinOrderId;

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
                nContinueSameRequestOrderIdCount = 0;
               // if (UdpStaticCommon.GetNowTime() - nLastRequestOrderIdTime > 5)
                {
                    nLastRequestOrderIdTime = UdpStaticCommon.GetNowTime();
                    AddCheckPackage();

                    var mNode = mWaitCheckSendQueue.First;
                    while (mNode != null)
                    {
                        var mPackage = mNode.Value;
                        if (mPackage.nOrderId == nRequestOrderId)
                        {
                            SendNetPackage(mPackage);
                            ArrangeNextSend(mPackage);

                            //this.nMaxSearchCount = Math.Max(1, this.nSearchCount / 2);
                           // this.nSearchCount = this.nMaxSearchCount + 3;
                           // this.nSearchCount = Math.Max(1, this.nSearchCount / 2);

                            UdpStatistical.AddQuickReSendCount();
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
            int nSearchCount = Config.nUdpMaxOrderId - Config.nUdpMinOrderId + 1;
            var mNode = mWaitCheckSendQueue.First;
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

        private void Sure()
        {
            this.nRemainNeedSureCount--;
            if (this.nSearchCount < this.nMaxSearchCount)
            {
                this.nSearchCount += 2;
            }
            else if (this.nRemainNeedSureCount <= 0)
            {
                this.nSearchCount += 2;
                this.nRemainNeedSureCount = this.nMaxSearchCount;
            }
        }

        private void SendNetPackage(NetUdpFixedSizePackage mCheckPackage)
        {
            mClientPeer.SendNetPackage(mCheckPackage);
        }
    }

}