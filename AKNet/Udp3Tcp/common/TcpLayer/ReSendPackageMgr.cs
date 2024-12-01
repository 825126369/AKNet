/************************************Copyright*****************************************
*        ProjectName:AKNet
*        Web:https://github.com/825126369/AKNet
*        Description:这是一个面向 .Net Standard 2.1 的游戏网络库
*        Author:阿珂
*        CreateTime:2024/11/28 7:14:07
*        Copyright:MIT软件许可证
************************************Copyright*****************************************/
using AKNet.Common;
using System;
using System.Collections.Generic;

namespace AKNet.Udp3Tcp.Common
{
    internal class ReSendPackageMgr : ReSendPackageMgrInterface
    {
        private UdpClientPeerCommonBase mClientPeer;
        private UdpCheckMgr mUdpCheckMgr;

        private readonly AkCircularBuffer<byte> mSendStreamList = new AkCircularBuffer<byte>();
        private readonly Queue<NetUdpSendFixedSizePackage> mWaitCheckSendQueue = new Queue<NetUdpSendFixedSizePackage>();

        public uint nCurrentWaitSendOrderId;
        public uint nCurrentSendStreamListBeginOrderId;

        private long nLastRequestOrderIdTime = 0;
        private uint nLastRequestOrderId = 0;
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
            nCurrentSendStreamListBeginOrderId = Config.nUdpMinOrderId;
        }

        public void AddTcpStream(ReadOnlySpan<byte> buffer)
        {
            mSendStreamList.WriteFrom(buffer);
        }

        public void AddSendPackageOrderId(int nLength)
        {
            nCurrentWaitSendOrderId = OrderIdHelper.AddOrderId(nCurrentWaitSendOrderId, nLength);
        }

        public void AddCircularBufferOrderId(uint nRequestOrderId)
        {
            uint nOriOffsetId = nCurrentSendStreamListBeginOrderId;
            int nClearLength = OrderIdHelper.GetOrderIdLength(nOriOffsetId, nRequestOrderId);
            mSendStreamList.ClearBuffer(nClearLength);
            nCurrentSendStreamListBeginOrderId = nRequestOrderId;
            NetLog.Log($"{nOriOffsetId}-{nClearLength}-{nCurrentSendStreamListBeginOrderId}-{nRequestOrderId}");
        }

        private void AddPackage()
        {
            int nSendStreamListOffset = OrderIdHelper.GetOrderIdLength(nCurrentSendStreamListBeginOrderId, nCurrentWaitSendOrderId);
            while (nSendStreamListOffset < mSendStreamList.Length)
            {
                NetLog.Assert(nSendStreamListOffset >= 0);
                var mPackage = mClientPeer.GetObjectPoolManager().UdpSendPackage_Pop();
                mPackage.nOrderId = nCurrentWaitSendOrderId;
                mPackage.nOffset = nSendStreamListOffset;

                int nRemainLength = mSendStreamList.Length - nSendStreamListOffset;
                if (Config.nUdpPackageFixedBodySize <= nRemainLength)
                {
                    mPackage.nBodyLength = Config.nUdpPackageFixedBodySize;
                }
                else
                {
                    mPackage.nBodyLength = (int)nRemainLength;
                }

                mPackage.mBuffer = mSendStreamList;

                mWaitCheckSendQueue.Enqueue(mPackage);
                mPackage.mTimeOutGenerator_ReSend.Reset();
                AddSendPackageOrderId(mPackage.nBodyLength);

                //NetLog.Log($"{mPackage.nOrderId}-{mPackage.nBodyLength}-{nCurrentWaitSendOrderId}");

                nSendStreamListOffset = OrderIdHelper.GetOrderIdLength(nCurrentSendStreamListBeginOrderId, nCurrentWaitSendOrderId);
            }
        }

        public void Update(double elapsed)
        {
            UdpStatistical.AddSearchCount(this.nSearchCount);
            nLastFrameTime = elapsed;

            AddPackage();

            bool bTimeOut = false;
            int nSearchCount = this.nSearchCount;
            foreach (var mPackage in mWaitCheckSendQueue)
            {
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

                if (--nSearchCount <= 0)
                {
                    break;
                }
            }

            if (bTimeOut)
            {
                this.nMaxSearchCount = this.nSearchCount - 1;
                this.nSearchCount = Math.Max(1, this.nSearchCount / 2);
            }
        }

        public void Reset()
        {
            MainThreadCheck.Check();
            nCurrentWaitSendOrderId = Config.nUdpMinOrderId;
            nCurrentSendStreamListBeginOrderId = Config.nUdpMinOrderId;
            mSendStreamList.reset();

            foreach(var mRemovePackage in mWaitCheckSendQueue)
            {
                mClientPeer.GetObjectPoolManager().UdpSendPackage_Recycle(mRemovePackage);
            }
            mWaitCheckSendQueue.Clear();
        }
        
        private void ArrangeNextSend(NetUdpSendFixedSizePackage mPackage)
        {
            long nTimeOutTime = mClientPeer.GetTcpStanardRTOFunc().GetRTOTime();
            double fTimeOutTime = nTimeOutTime / 1000.0;
            mPackage.mTimeOutGenerator_ReSend.SetInternalTime(fTimeOutTime);
        }

        //快速重传
        private void QuickReSend(uint nRequestOrderId)
        {
            if (nRequestOrderId != nLastRequestOrderId)
            {
                nContinueSameRequestOrderIdCount = 0;
                nLastRequestOrderId = nRequestOrderId;
            }

            nContinueSameRequestOrderIdCount++;
            if (nContinueSameRequestOrderIdCount >= 3)
            {
                nContinueSameRequestOrderIdCount = 0;
                // if (UdpStaticCommon.GetNowTime() - nLastRequestOrderIdTime > 5)
                {
                    nLastRequestOrderIdTime = UdpStaticCommon.GetNowTime();
                    foreach (var mPackage in mWaitCheckSendQueue)
                    {
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
                    }
                }
            }
        }

        public void ReceiveOrderIdRequestPackage(uint nRequestOrderId)
        {
            AddPackage();

            bool bHit = false;
            int nRemoveCount = 0;
            foreach (var mPackage in mWaitCheckSendQueue)
            {
                if (mPackage.nOrderId == nRequestOrderId)
                {
                    bHit = true;
                    break;
                }
                else
                {
                    nRemoveCount++;
                }
            }

            if (bHit)
            {
                while (nRemoveCount-- > 0)
                {
                    var mPackage = mWaitCheckSendQueue.Dequeue();
                    mPackage.mTcpStanardRTOTimer.FinishRtt(mClientPeer);
                    mClientPeer.GetObjectPoolManager().UdpSendPackage_Recycle(mPackage);
                    Sure();
                }

                AddCircularBufferOrderId(nRequestOrderId);
                AddPackage();
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

        private void SendNetPackage(NetUdpSendFixedSizePackage mCheckPackage)
        {
            mClientPeer.SendNetPackage(mCheckPackage);
        }
    }

}