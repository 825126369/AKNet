/************************************Copyright*****************************************
*        ProjectName:AKNet
*        Web:https://github.com/825126369/AKNet
*        Description:C#游戏网络库
*        Author:许珂
*        StartTime:2024/11/01 00:00:00
*        ModifyTime:2025/11/30 19:43:17
*        Copyright:MIT软件许可证
************************************Copyright*****************************************/
using AKNet.Common;
using System;

namespace AKNet.Udp4Tcp.Common
{
    internal partial class Connection
    {
        public void AddTcpStream(ReadOnlySpan<byte> buffer)
        {
            mTcpSlidingWindow.WriteFrom(buffer);
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

                var mPackage = mThreadWorker.mSendPackagePool.Pop();
                mPackage.mThreadWorker = mThreadWorker;
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

        private void ArrangeReSendTimeOut(NetUdpSendFixedSizePackage mPackage)
        {
            long nTimeOutTime = GetRTOTime();
            UdpStatistical.AddRTO(nTimeOutTime);
            mPackage.SetInternalTime(nTimeOutTime);
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
                            SendUDPPackage(mPackage);
                            mPackage.nSendCount++;
                            UdpStatistical.AddQuickReSendCount();
                            
                            //this.nMaxSearchCount = Math.Max(nMinSearchCount, this.nSearchCount / 2);
                            //this.nSearchCount = this.nMaxSearchCount + 3;
                            //this.nSearchCount = Math.Max(nMinSearchCount, this.nSearchCount / 2);
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
                    if (mPackage.nSendCount == 1)
                    {
                        mPackage.mTcpStanardRTOTimer.FinishRtt(this);
                    }

                    mThreadWorker.mSendPackagePool.recycle(mPackage);
                    Sure();
                }

                if (bHaveRemove)
                {
                    DoTcpSlidingWindowForward(nRequestOrderId);
                }
                else
                {
                    QuickReSend(nRequestOrderId);
                }

                //DoTcpSlidingWindowForward(nRequestOrderId);
                //QuickReSend(nRequestOrderId);
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
    }

}