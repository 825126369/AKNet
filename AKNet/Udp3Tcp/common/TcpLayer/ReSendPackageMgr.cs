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
    internal partial class ReSendPackageMgr : ReSendPackageMgrInterface
    {
        private UdpClientPeerCommonBase mClientPeer;
        private UdpCheckMgr mUdpCheckMgr;

        private readonly TcpSlidingWindow mTcpSlidingWindow = new TcpSlidingWindow();
        private readonly Queue<NetUdpSendFixedSizePackage> mWaitCheckSendQueue = new Queue<NetUdpSendFixedSizePackage>();
        public uint nCurrentWaitSendOrderId;

        private long nLastRequestOrderIdTime = 0;
        private uint nLastRequestOrderId = 0;
        private int nContinueSameRequestOrderIdCount = 0;
        private double nLastFrameTime = 0;
        private int nSearchCount = 0;

        private const int nMinSearchCount = 10;
        private int nMaxSearchCount = int.MaxValue;
        private int nRemainNeedSureCount = 0;

        public ReSendPackageMgr(UdpClientPeerCommonBase mClientPeer, UdpCheckMgr mUdpCheckMgr)
        {
            this.mClientPeer = mClientPeer;
            this.mUdpCheckMgr = mUdpCheckMgr;

            this.nSearchCount = nMinSearchCount;
            nMaxSearchCount = this.nSearchCount * 2;
            nCurrentWaitSendOrderId = Config.nUdpMinOrderId;

            InitRTO();
            ArrangeReSendTimeOut();
        }

        public void AddTcpStream(ReadOnlySpan<byte> buffer)
        {
            mTcpSlidingWindow.WriteFrom(buffer);
            if (mWaitCheckSendQueue.Count == 0)
            {
                FirstWindowEmit();
            }
        }

        private void AddSendPackageOrderId(int nLength)
        {
            nCurrentWaitSendOrderId = OrderIdHelper.AddOrderId(nCurrentWaitSendOrderId, nLength);
        }

        void DoTcpSlidingWindowForward(uint nRequestOrderId)
        {
            mTcpSlidingWindow.DoWindowForward(nRequestOrderId);
            AddPackage();
        }

        private void AddPackage()
        {
            int nOffset = mTcpSlidingWindow.GetWindowOffset(nCurrentWaitSendOrderId);
            while (nOffset < mTcpSlidingWindow.Length)
            {
                NetLog.Assert(nOffset >= 0);

                var mPackage = mClientPeer.GetObjectPoolManager().UdpSendPackage_Pop();
                mPackage.mTcpSlidingWindow = this.mTcpSlidingWindow;
                mPackage.nOrderId = nCurrentWaitSendOrderId;

                int nRemainLength = mTcpSlidingWindow.Length - nOffset;
                NetLog.Assert(nRemainLength >= 0);

                if (Config.nUdpPackageFixedBodySize <= nRemainLength)
                {
                    mPackage.nBodyLength = Config.nUdpPackageFixedBodySize;
                }
                else
                {
                    mPackage.nBodyLength = (ushort)nRemainLength;
                }

                mWaitCheckSendQueue.Enqueue(mPackage);
                AddSendPackageOrderId(mPackage.nBodyLength);

                nOffset = mTcpSlidingWindow.GetWindowOffset(nCurrentWaitSendOrderId);
            }
        }

        public void Update(double elapsed)
        {
            UdpStatistical.AddSearchCount(this.nSearchCount);
            UdpStatistical.AddFrameCount();
            nLastFrameTime = elapsed;
            if (mTimeOutGenerator_ReSend.orTimeOut(elapsed))
            {
                WindowEmit_ForTimeOut();
            }
        }

        private void FirstWindowEmit()
        {
            AddPackage();
            if (mWaitCheckSendQueue.Count == 0) return;
            int nSearchCount = this.nSearchCount;
            foreach (var mPackage in mWaitCheckSendQueue)
            {
                mPackage.mReSendCount++;
                if (mPackage.mReSendCount == 1)
                {
                    UdpStatistical.AddFirstSendCheckPackageCount();
                    SendNetPackage(mPackage);
                }

                if (--nSearchCount <= 0)
                {
                    break;
                }
            }
        }

        private void WindowEmit_ForTimeOut()
        {
            AddPackage();
            if (mWaitCheckSendQueue.Count == 0) return;

            ArrangeReSendTimeOut();
            mTcpStanardRTOTimer.BeginRtt();
            int nSearchCount = this.nSearchCount;
            foreach (var mPackage in mWaitCheckSendQueue)
            {
                UdpStatistical.AddReSendCheckPackageCount();
                mPackage.mReSendCount++;
                SendNetPackage(mPackage);
                if (--nSearchCount <= 0)
                {
                    break;
                }
            }
            this.nSearchCount = Math.Max(this.nSearchCount / 2 + 1, nMinSearchCount);
        }

        public void Reset()
        {
            MainThreadCheck.Check();
            nCurrentWaitSendOrderId = Config.nUdpMinOrderId;
            mTcpSlidingWindow.WindowReset();

            foreach (var mRemovePackage in mWaitCheckSendQueue)
            {
                mClientPeer.GetObjectPoolManager().UdpSendPackage_Recycle(mRemovePackage);
            }
            mWaitCheckSendQueue.Clear();
        }

        private void ArrangeReSendTimeOut()
        {
            long nTimeOutTime = GetRTOTime();
            double fTimeOutTime = nTimeOutTime / 1000.0;
            mTimeOutGenerator_ReSend.SetInternalTime(fTimeOutTime);
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
                            //this.nMaxSearchCount = Math.Max(nMinSearchCount, this.nSearchCount / 2);
                            //this.nSearchCount = this.nMaxSearchCount + 3;
                            //this.nSearchCount = Math.Max(nMinSearchCount, this.nSearchCount / 2);

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
                bool bHaveRemove = nRemoveCount > 0;
                while (nRemoveCount-- > 0)
                {
                    var mPackage = mWaitCheckSendQueue.Dequeue();
                    mClientPeer.GetObjectPoolManager().UdpSendPackage_Recycle(mPackage);
                    Sure();
                }

                if (bHaveRemove)
                {
                    DoTcpSlidingWindowForward(nRequestOrderId);
                    mTcpStanardRTOTimer.FinishRtt(this);
                    FirstWindowEmit();
                }
                else
                {
                    QuickReSend(nRequestOrderId);
                }
            }
        }

        private void Sure()
        {
            this.nRemainNeedSureCount--;
            if (this.nSearchCount < nMaxSearchCount)
            {
                this.nSearchCount = (this.nSearchCount + this.nMaxSearchCount) / 2 + 1;
                this.nSearchCount = Math.Max(nMinSearchCount, this.nSearchCount);
            }
            else if (this.nRemainNeedSureCount <= 0)
            {
                this.nSearchCount = this.nSearchCount + 1;
                this.nMaxSearchCount = Math.Max(this.nSearchCount / 2 + 1, this.nMaxSearchCount);

                this.nRemainNeedSureCount = this.nMaxSearchCount / 3 + 1;
            }
        }

        private void SendNetPackage(NetUdpSendFixedSizePackage mCheckPackage)
        {
            mClientPeer.SendNetPackage(mCheckPackage);
        }
    }

}