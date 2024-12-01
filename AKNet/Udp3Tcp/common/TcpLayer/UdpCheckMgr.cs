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
    internal class UdpCheckMgr
    {
        public const int nDefaultSendPackageCount = 1024;
        public const int nDefaultCacheReceivePackageCount = 2048;

        private uint nCurrentWaitReceiveOrderId;
        private readonly ReSendPackageMgrInterface mReSendPackageMgr = null;

        private UdpClientPeerCommonBase mClientPeer = null;
        public UdpCheckMgr(UdpClientPeerCommonBase mClientPeer)
        {
            this.mClientPeer = mClientPeer;

            mReSendPackageMgr = new ReSendPackageMgr(mClientPeer, this);
            nCurrentWaitReceiveOrderId = Config.nUdpMinOrderId;
        }

        public void AddReceivePackageOrderId(int nLength)
        {
            nCurrentWaitReceiveOrderId = OrderIdHelper.AddOrderId(nCurrentWaitReceiveOrderId, nLength);
        }

        public void SendTcpStream(ReadOnlySpan<byte> buffer)
        {
            MainThreadCheck.Check();
            if (this.mClientPeer.GetSocketState() != SOCKET_PEER_STATE.CONNECTED) return;
#if DEBUG
            if (buffer.Length > Config.nMaxDataLength)
            {
                NetLog.LogError("超出允许的最大包尺寸：" + Config.nMaxDataLength);
            }
#endif
            mReSendPackageMgr.AddTcpStream(buffer);
        }

        public void ReceiveNetPackage(NetUdpReceiveFixedSizePackage mReceivePackage)
        {
            byte nInnerCommandId = mReceivePackage.GetInnerCommandId();
            MainThreadCheck.Check();
            if (mClientPeer.GetSocketState() == SOCKET_PEER_STATE.CONNECTED)
            {
                this.mClientPeer.ReceiveHeartBeat();

                if (mReceivePackage.nRequestOrderId > 0)
                {
                    mReSendPackageMgr.ReceiveOrderIdRequestPackage(mReceivePackage.nRequestOrderId);
                }

                if (nInnerCommandId == UdpNetCommand.COMMAND_HEARTBEAT)
                {

                }
                else if (nInnerCommandId == UdpNetCommand.COMMAND_CONNECT)
                {
                    this.mClientPeer.ReceiveConnect();
                }
                else if (nInnerCommandId == UdpNetCommand.COMMAND_DISCONNECT)
                {
                    this.mClientPeer.ReceiveDisConnect();
                }

                if (UdpNetCommand.orInnerCommand(nInnerCommandId))
                {
                    mClientPeer.GetObjectPoolManager().UdpReceivePackage_Recycle(mReceivePackage);
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
                    this.mClientPeer.ReceiveConnect();
                }
                else if (nInnerCommandId == UdpNetCommand.COMMAND_DISCONNECT)
                {
                    this.mClientPeer.ReceiveDisConnect();
                }
                mClientPeer.GetObjectPoolManager().UdpReceivePackage_Recycle(mReceivePackage);
            }
        }
        
        readonly List<NetUdpReceiveFixedSizePackage> mCacheReceivePackageList = new List<NetUdpReceiveFixedSizePackage>(nDefaultCacheReceivePackageCount);
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
                        mClientPeer.GetObjectPoolManager().UdpReceivePackage_Recycle(mTempPackage);
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
                    mClientPeer.GetObjectPoolManager().UdpReceivePackage_Recycle(mPackage);
                }
            }

            if (mClientPeer.GetCurrentFrameRemainPackageCount() <= 0)
            {
                SendSureOrderIdPackage();
            }
        }

        private void CheckCombinePackage(NetUdpReceiveFixedSizePackage mCheckPackage)
        {
            mClientPeer.ReceiveTcpStream(mCheckPackage);
            mClientPeer.GetObjectPoolManager().UdpReceivePackage_Recycle(mCheckPackage);
        }

        public void Update(double elapsed)
        {
            if (mClientPeer.GetSocketState() != SOCKET_PEER_STATE.CONNECTED) return;
            mReSendPackageMgr.Update(elapsed);
            UdpStatistical.AddFrameCount();
        }

        public void SetRequestOrderId(NetUdpSendFixedSizePackage mPackage)
        {
            mPackage.nRequestOrderId = nCurrentWaitReceiveOrderId;
        }

        private void SendSureOrderIdPackage()
        {
            mClientPeer.SendInnerNetData(UdpNetCommand.COMMAND_PACKAGE_CHECK_SURE_ORDERID);
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
                mClientPeer.GetObjectPoolManager().UdpReceivePackage_Recycle(mRemovePackage);
            }

            nCurrentWaitReceiveOrderId = Config.nUdpMinOrderId;
        }

        public void Release()
        {

        }
    }
}