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
    internal class CheckPackageMgr1 : CheckPackageMgrInterface
    {
        private class CheckPackageInfo
        {
            private readonly CheckPackageInfo_TimeOutGenerator mTimeOutGenerator = new CheckPackageInfo_TimeOutGenerator();
            private bool bInPlaying = false;
            private UdpClientPeerCommonBase mClientPeer;
            private CheckPackageMgr1 mUdpCheckMgr;
            private readonly TcpStanardRTOFunc mRTOFuc = new TcpStanardRTOFunc();
            private NetUdpFixedSizePackage currentCheckRTOPackage = null;
            private double nLastFrameTime = 0;

            private long nLastRequestOrderIdTime = 0;
            private int nLastRequestOrderId = 0;
            private int nContinueSameSureOrderIdCount = 0;

            public CheckPackageInfo(UdpClientPeerCommonBase mClientPeer, CheckPackageMgr1 mUdpCheckMgr)
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

            public void ReceiveOrderIdRequestPackage(ushort nRequestOrderId)
            {
                if (mClientPeer.GetSocketState() == SOCKET_PEER_STATE.CONNECTED)
                {
                    bool bHaveOrderId = false;
                    var mQueueIter = mUdpCheckMgr.mWaitCheckSendQueue.GetEnumerator();
                    int nRemoveCount = 0;
                    while (mQueueIter.MoveNext())
                    {
                        if (mQueueIter.Current.nOrderId == nRequestOrderId)
                        {
                            bHaveOrderId = true;
                            break;
                        }
                        else
                        {
                            nRemoveCount++;
                        }
                    }

                    if (bHaveOrderId)
                    {
                        while (nRemoveCount-- > 0)
                        {
                            var mCheckPackage = mUdpCheckMgr.mWaitCheckSendQueue.First.Value;
                            FinishRtt(mCheckPackage);
                            mClientPeer.GetObjectPoolManager().NetUdpFixedSizePackage_Recycle(mCheckPackage);

                            mUdpCheckMgr.mWaitCheckSendQueue.RemoveFirst();
                        }
                    }
                    else
                    {
                        QuickReSend(nRequestOrderId);
                    }
                }
            }

            //快速重传
            private void QuickReSend(ushort nRequestOrderId)
            {
                if (nRequestOrderId != nLastRequestOrderId)
                {
                    nContinueSameSureOrderIdCount = 0;
                    nLastRequestOrderId = nRequestOrderId;
                    nLastRequestOrderIdTime = UdpStaticCommon.GetNowTime();
                }

                nContinueSameSureOrderIdCount++;
                if (nContinueSameSureOrderIdCount > 3)
                {
                    if (UdpStaticCommon.GetNowTime() - nLastRequestOrderIdTime < 3000)
                    {
                        if (mUdpCheckMgr.mWaitCheckSendQueue.Count > 0)
                        {
                            int nSearchCount = UdpCheckMgr.nDefaultSendPackageCount;
                            var mQueueIter = mUdpCheckMgr.mWaitCheckSendQueue.GetEnumerator();
                            while (mQueueIter.MoveNext() && nSearchCount-- > 0)
                            {
                                var mCheckPackage = mQueueIter.Current;
                                if (mCheckPackage.nOrderId == nRequestOrderId)
                                {
                                    var mSendPackage = mClientPeer.GetObjectPoolManager().NetUdpFixedSizePackage_Pop();
                                    mSendPackage.CopyFrom(mCheckPackage);
                                    mClientPeer.SendNetPackage(mSendPackage);
                                    break;
                                }
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
                    this.bInPlaying = true;
                    ArrangeNextSend();
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
                int nSendCount = (int)(fCoef * UdpCheckMgr.nDefaultSendPackageCount);
                nSendCount = Math.Clamp(nSendCount, 1, UdpCheckMgr.nDefaultSendPackageCount);

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

        private UdpClientPeerCommonBase mClientPeer;
        private readonly CheckPackageInfo mCheckPackageInfo = null;
        public readonly LinkedList<NetUdpFixedSizePackage> mWaitCheckSendQueue = null;
        public readonly TcpStanardRTOFunc mRTOFuc = new TcpStanardRTOFunc();

        public CheckPackageMgr1(UdpClientPeerCommonBase mClientPeer)
        {
            this.mClientPeer = mClientPeer;
            mCheckPackageInfo = new CheckPackageInfo(mClientPeer, this);
            mWaitCheckSendQueue = new LinkedList<NetUdpFixedSizePackage>();
        }

        public void Add(NetUdpFixedSizePackage mPackage)
        {
            mWaitCheckSendQueue.AddLast(mPackage);
            mCheckPackageInfo.Do();
        }

        public void Update(double elapsed)
        {
            mCheckPackageInfo.Update(elapsed);
        }

        public void Reset()
        {
            foreach (var mRemovePackage in mWaitCheckSendQueue)
            {
                mClientPeer.GetObjectPoolManager().NetUdpFixedSizePackage_Recycle(mRemovePackage);
            }

            mWaitCheckSendQueue.Clear();
        }

        public void ReceiveOrderIdRequestPackage(ushort nRequestOrderId)
        {
            mCheckPackageInfo.ReceiveOrderIdRequestPackage(nRequestOrderId);
        }

        public void ReceiveOrderIdSurePackage(ushort nSureOrderId)
        {
            var mNode = mWaitCheckSendQueue.First;
            while (mNode != null)
            {
                var mPackage = mNode.Value;
                if (mPackage.nOrderId == nSureOrderId)
                {
                    mClientPeer.GetObjectPoolManager().NetUdpFixedSizePackage_Recycle(mPackage);
                    mWaitCheckSendQueue.Remove(mNode);
                    break;
                }
                mNode = mNode.Next;
            }
        }
    }
}