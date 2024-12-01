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
using System.Runtime.CompilerServices;

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

        public void AddCircularBufferOrderId(NetUdpSendFixedSizePackage mPackage)
        {
            mSendStreamList.ClearBuffer(mPackage.Length);
            nCurrentSendStreamListBeginOrderId = OrderIdHelper.AddOrderId(nCurrentSendStreamListBeginOrderId, mPackage.Length);
        }

        private void AddPackage()
        {
            int nSendStreamListOffset = OrderIdHelper.GetOrderIdLength(nCurrentSendStreamListBeginOrderId, nCurrentWaitSendOrderId);
            while (nSendStreamListOffset < mSendStreamList.Length)
            {
                var mPackage = mClientPeer.GetObjectPoolManager().UdpSendPackage_Pop();
                mPackage.nOrderId = nCurrentWaitSendOrderId;
                mPackage.nOffset = nSendStreamListOffset;
                if (nSendStreamListOffset + Config.nUdpPackageFixedBodySize <= mSendStreamList.Length)
                {
                    mPackage.nRequestOrderId = mPackage.nOrderId + Config.nUdpPackageFixedBodySize;
                }
                else
                {
                    mPackage.nRequestOrderId = mPackage.nOrderId + (uint)mSendStreamList.Length;
                }
                mPackage.mBuffer = mSendStreamList;

                mWaitCheckSendQueue.Enqueue(mPackage);
                mPackage.mTimeOutGenerator_ReSend.Reset();
                AddSendPackageOrderId(mPackage.Length);

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
            foreach (var v in mWaitCheckSendQueue)
            {
                NetUdpSendFixedSizePackage mPackage = v;
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

                if (--nSearchCount > 0)
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
                        if (mPackage.nRequestOrderId == nRequestOrderId)
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
                if (mPackage.nRequestOrderId == nRequestOrderId)
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
                    AddCircularBufferOrderId(mPackage);
                    mClientPeer.GetObjectPoolManager().UdpSendPackage_Recycle(mPackage);
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

        private void SendNetPackage(NetUdpSendFixedSizePackage mCheckPackage)
        {
            mClientPeer.SendNetPackage(mCheckPackage);
        }
    }

}