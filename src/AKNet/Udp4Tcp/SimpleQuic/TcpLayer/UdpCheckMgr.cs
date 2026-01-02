/************************************Copyright*****************************************
*        ProjectName:AKNet
*        Web:https://github.com/825126369/AKNet
*        Description:C#游戏网络库
*        Author:许珂
*        StartTime:2024/11/01 00:00:00
*        ModifyTime:2025/11/30 19:43:17
*        Copyright:MIT软件许可证
************************************Copyright*****************************************/
using System;
using System.Collections.Generic;

namespace AKNet.Udp4Tcp.Common
{
    internal class UdpCheckMgr
    {
        public const int nDefaultSendPackageCount = 1024;
        public const int nDefaultCacheReceivePackageCount = 2048;

        private uint nCurrentWaitReceiveOrderId;
        private readonly ReSendPackageMgr mReSendPackageMgr = null;

        private Connection mConnection = null;
        public UdpCheckMgr(Connection mConnection)
        {
            this.mConnection = mConnection;
            this.mReSendPackageMgr = new ReSendPackageMgr(mConnection, this);
            this.nCurrentWaitReceiveOrderId = Config.nUdpMinOrderId;
        }

        public void AddTcpStream(ReadOnlySpan<byte> buffer)
        {
            mReSendPackageMgr.AddTcpStream(buffer);
        }

        public void AddReceivePackageOrderId(int nLength)
        {
            nCurrentWaitReceiveOrderId = OrderIdHelper.AddOrderId(nCurrentWaitReceiveOrderId, nLength);
            nSameOrderIdSureCount = 0;
        }

        public void ReceiveNetPackage(NetUdpReceiveFixedSizePackage mReceivePackage)
        {
            byte nInnerCommandId = mReceivePackage.GetInnerCommandId();
            SimpleQuicFunc.ThreadCheck(mConnection);
            if (mConnection.Connected)
            {
                this.mConnection.ReceiveHeartBeat();

                if (mReceivePackage.nRequestOrderId > 0)
                {
                    mReSendPackageMgr.ReceiveOrderIdRequestPackage(mReceivePackage.nRequestOrderId);
                }

                if (nInnerCommandId == UdpNetCommand.COMMAND_HEARTBEAT)
                {

                }
                else if (nInnerCommandId == UdpNetCommand.COMMAND_CONNECT)
                {
                    this.mConnection.ReceiveConnect();
                }
                else if (nInnerCommandId == UdpNetCommand.COMMAND_DISCONNECT)
                {
                    this.mConnection.ReceiveDisConnect();
                }

                if (UdpNetCommand.orInnerCommand(nInnerCommandId))
                {
                    mConnection.mLogicWorker.mThreadWorker.mReceivePackagePool.recycle(mReceivePackage);
                }
                else
                {
                    CheckReceivePackageLoss(mReceivePackage);
                }
            }
            else
            {
                if (nInnerCommandId == UdpNetCommand.COMMAND_CONNECT)
                {
                    this.mConnection.ReceiveConnect();
                }
                else if (nInnerCommandId == UdpNetCommand.COMMAND_DISCONNECT)
                {
                    this.mConnection.ReceiveDisConnect();
                }
                mConnection.mLogicWorker.mThreadWorker.mReceivePackagePool.recycle(mReceivePackage);
            }
        }
        
        readonly List<NetUdpReceiveFixedSizePackage> mCacheReceivePackageList = new List<NetUdpReceiveFixedSizePackage>(nDefaultCacheReceivePackageCount);
        long nLastSendSurePackageTime = 0;
        long nSameOrderIdSureCount = 0;
        private void CheckReceivePackageLoss(NetUdpReceiveFixedSizePackage mPackage)
        {
            UdpStatistical.AddReceiveCheckPackageCount();
            if (mPackage.nOrderId == nCurrentWaitReceiveOrderId)
            {
                AddReceivePackageOrderId(mPackage.nBodyLength);
                CheckCombinePackage(mPackage);

                mPackage = mCacheReceivePackageList.Find((x) => x.nOrderId == nCurrentWaitReceiveOrderId);
                while (mPackage != null)
                {
                    mCacheReceivePackageList.Remove(mPackage);
                    AddReceivePackageOrderId(mPackage.nBodyLength);
                    CheckCombinePackage(mPackage);
                    mPackage = mCacheReceivePackageList.Find((x) => x.nOrderId == nCurrentWaitReceiveOrderId);
                }

                for (int i = mCacheReceivePackageList.Count - 1; i >= 0; i--)
                {
                    var mTempPackage = mCacheReceivePackageList[i];
                    if (mTempPackage.nOrderId <= nCurrentWaitReceiveOrderId)
                    {
                        mConnection.mLogicWorker.mThreadWorker.mReceivePackagePool.recycle(mTempPackage);
                        mCacheReceivePackageList.RemoveAt(i);
                    }
                }

                UdpStatistical.AddHitTargetOrderPackageCount();
            }
            else
            {
                if (mCacheReceivePackageList.Find(x => x.nOrderId == mPackage.nOrderId) == null &&
                    OrderIdHelper.orInOrderIdFront(nCurrentWaitReceiveOrderId, mPackage.nOrderId, nDefaultCacheReceivePackageCount * Config.nUdpPackageFixedBodySize) &&
                    mCacheReceivePackageList.Count < nDefaultCacheReceivePackageCount)
                {
                    mCacheReceivePackageList.Add(mPackage);
                    UdpStatistical.AddHitReceiveCachePoolPackageCount();
                }
                else if (mCacheReceivePackageList.Find(x => x.nOrderId == mPackage.nOrderId) != null)
                {
                    UdpStatistical.AddHitReceiveCachePoolPackageCount();
                }
                else
                {
                    UdpStatistical.AddGarbagePackageCount();
                    mConnection.mLogicWorker.mThreadWorker.mReceivePackagePool.recycle(mPackage);
                }
            }

            if (nSameOrderIdSureCount == 0 && mConnection.GetCurrentFrameRemainPackageCount() == 0)
            {
                SendSureOrderIdPackage();
            }
        }

        private void CheckCombinePackage(NetUdpReceiveFixedSizePackage mCheckPackage)
        {
            mConnection.ReceiveTcpStream(mCheckPackage);
            mConnection.mLogicWorker.mThreadWorker.mReceivePackagePool.recycle(mCheckPackage);
        }

        public void ThreadUpdate()
        {
            if (!mConnection.Connected) return;
            mReSendPackageMgr.ThreadUpdate();
        }

        public void SetRequestOrderId(NetUdpSendFixedSizePackage mPackage)
        {
            mPackage.nRequestOrderId = nCurrentWaitReceiveOrderId;
            nSameOrderIdSureCount++;
        }

        private void SendSureOrderIdPackage()
        {
            mConnection.SendInnerNetData(UdpNetCommand.COMMAND_PACKAGE_CHECK_SURE_ORDERID);
            UdpStatistical.AddSendSureOrderIdPackageCount();
        }

        public void Reset()
        {
            mReSendPackageMgr.Reset();
            while (mCacheReceivePackageList.Count > 0)
            {
                int nRemoveIndex = mCacheReceivePackageList.Count - 1;
                NetUdpReceiveFixedSizePackage mRemovePackage = mCacheReceivePackageList[nRemoveIndex];
                mCacheReceivePackageList.RemoveAt(nRemoveIndex);
                mConnection.mLogicWorker.mThreadWorker.mReceivePackagePool.recycle(mRemovePackage);
            }

            nCurrentWaitReceiveOrderId = Config.nUdpMinOrderId;
        }
    }
}