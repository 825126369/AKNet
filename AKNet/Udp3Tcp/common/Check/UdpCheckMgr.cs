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
        private uint nLastReceiveOrderId;
        
        private readonly AkCircularBuffer<byte> mSendStreamList = new AkCircularBuffer<byte>();
        private readonly ReSendPackageMgrInterface mReSendPackageMgr = null;

        private UdpClientPeerCommonBase mClientPeer = null;
        public UdpCheckMgr(UdpClientPeerCommonBase mClientPeer)
        {
            this.mClientPeer = mClientPeer;

            mReSendPackageMgr = new ReSendPackageMgr(mClientPeer, this);
            nCurrentWaitReceiveOrderId = Config.nUdpMinOrderId;
        }

        public void AddReceivePackageOrderId()
        {
            nLastReceiveOrderId = nCurrentWaitReceiveOrderId;
            nCurrentWaitReceiveOrderId = OrderIdHelper.AddOrderId(nCurrentWaitReceiveOrderId);
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
            mSendStreamList.WriteFrom(buffer);
            if (!Config.bUdpCheck)
            {
                NetUdpFixedSizePackage mPackage = mClientPeer.GetObjectPoolManager().NetUdpFixedSizePackage_Pop();
                int nLength = GetSendStreamList().WriteToMax(0, mPackage.buffer, Config.nUdpPackageFixedHeadSize, Config.nUdpPackageFixedBodySize);
                mPackage.Length = Config.nUdpPackageFixedHeadSize + nLength;
                mClientPeer.SendNetPackage(mPackage);
            }
        }

        public AkCircularBuffer<byte> GetSendStreamList()
        {
            return mSendStreamList;
        }

        public void ReceiveNetPackage(NetUdpFixedSizePackage mReceivePackage)
        {
            MainThreadCheck.Check();
            if (mClientPeer.GetSocketState() == SOCKET_PEER_STATE.CONNECTED)
            {
                this.mClientPeer.ReceiveHeartBeat();
                if (Config.bUdpCheck)
                {
                    if (mReceivePackage.GetRequestOrderId() > 0)
                    {
                        mReSendPackageMgr.ReceiveOrderIdRequestPackage(mReceivePackage.GetRequestOrderId());
                    }
                }

                if (mReceivePackage.GetPackageId() == UdpNetCommand.COMMAND_HEARTBEAT)
                {

                }
                else if (mReceivePackage.GetPackageId() == UdpNetCommand.COMMAND_CONNECT)
                {
                    this.mClientPeer.ReceiveConnect();
                }
                else if (mReceivePackage.GetPackageId() == UdpNetCommand.COMMAND_DISCONNECT)
                {
                    this.mClientPeer.ReceiveDisConnect();
                }

                if (UdpNetCommand.orInnerCommand(mReceivePackage.GetPackageId()))
                {
                    mClientPeer.GetObjectPoolManager().NetUdpFixedSizePackage_Recycle(mReceivePackage);
                }
                else
                {
                    if (Config.bUdpCheck)
                    {
                        CheckReceivePackageLoss(mReceivePackage);
                    }
                    else
                    {
                        CheckCombinePackage(mReceivePackage);
                    }
                }
            }
            else
            {
                if (mReceivePackage.GetPackageId() == UdpNetCommand.COMMAND_CONNECT)
                {
                    this.mClientPeer.ReceiveConnect();
                }
                else if (mReceivePackage.GetPackageId() == UdpNetCommand.COMMAND_DISCONNECT)
                {
                    this.mClientPeer.ReceiveDisConnect();
                }
                mClientPeer.GetObjectPoolManager().NetUdpFixedSizePackage_Recycle(mReceivePackage);
            }
        }

        //这个缓存有很大问题，比如
        readonly List<NetUdpFixedSizePackage> mCacheReceivePackageList = new List<NetUdpFixedSizePackage>(nDefaultCacheReceivePackageCount);
        long nLastCheckReceivePackageLossTime = 0;
        private void CheckReceivePackageLoss(NetUdpFixedSizePackage mPackage)
        {
            UdpStatistical.AddReceiveCheckPackageCount();
            uint nCurrentWaitSureId = mPackage.nOrderId;
            if (mPackage.nOrderId == nCurrentWaitReceiveOrderId)
            {
                AddReceivePackageOrderId();
                CheckCombinePackage(mPackage);

                mPackage = mCacheReceivePackageList.Find((x) => x.nOrderId == nCurrentWaitReceiveOrderId);
                while (mPackage != null)
                {
                    mCacheReceivePackageList.Remove(mPackage);
                    AddReceivePackageOrderId();
                    CheckCombinePackage(mPackage);
                    mPackage = mCacheReceivePackageList.Find((x) => x.nOrderId == nCurrentWaitReceiveOrderId);
                }

                for (int i = mCacheReceivePackageList.Count - 1; i >= 0; i--)
                {
                    var mTempPackage = mCacheReceivePackageList[i];
                    if (mTempPackage.nOrderId <= nCurrentWaitReceiveOrderId)
                    {
                        mClientPeer.GetObjectPoolManager().NetUdpFixedSizePackage_Recycle(mTempPackage);
                        mCacheReceivePackageList.RemoveAt(i);
                    }
                }

                SendSureOrderIdPackage(nCurrentWaitSureId);
                UdpStatistical.AddHitTargetOrderPackageCount();
            }
            else
            {
                if (mCacheReceivePackageList.Find(x => x.nOrderId == mPackage.nOrderId) == null &&
                    OrderIdHelper.orInOrderIdFront(nCurrentWaitReceiveOrderId, mPackage.nOrderId, nDefaultCacheReceivePackageCount) &&
                    mCacheReceivePackageList.Count < nDefaultCacheReceivePackageCount)
                {
                    SendSureOrderIdPackage(nCurrentWaitSureId);
                    mCacheReceivePackageList.Add(mPackage);
                    UdpStatistical.AddHitReceiveCachePoolPackageCount();
                }
                else if (mCacheReceivePackageList.Find(x => x.nOrderId == mPackage.nOrderId) != null)
                {
                    SendSureOrderIdPackage(nCurrentWaitSureId);
                    UdpStatistical.AddHitReceiveCachePoolPackageCount();
                }
                else
                {
                    UdpStatistical.AddGarbagePackageCount();
                    mClientPeer.GetObjectPoolManager().NetUdpFixedSizePackage_Recycle(mPackage);
                }
            }
        }

        private void CheckCombinePackage(NetUdpFixedSizePackage mCheckPackage)
        {
            mClientPeer.ReceiveTcpStream(mCheckPackage);
            mClientPeer.GetObjectPoolManager().NetUdpFixedSizePackage_Recycle(mCheckPackage);
        }

        public void Update(double elapsed)
        {
            if (mClientPeer.GetSocketState() != SOCKET_PEER_STATE.CONNECTED) return;
            mReSendPackageMgr.Update(elapsed);
            UdpStatistical.AddFrameCount();
        }

        public void SetRequestOrderId(NetUdpFixedSizePackage mPackage)
        {
            mPackage.SetRequestOrderId(nCurrentWaitReceiveOrderId);
        }

        private void SendLastSureOrderIdPackage()
        {
            if (nLastReceiveOrderId > 0)
            {
                if (mClientPeer.GetCurrentFrameRemainPackageCount() == 0)
                {
                    SendSureOrderIdPackage(nLastReceiveOrderId);
                }
            }
        }

        private void SendSureOrderIdPackage(uint nSureOrderId)
        {
            NetUdpFixedSizePackage mPackage = mClientPeer.GetObjectPoolManager().NetUdpFixedSizePackage_Pop();
            mPackage.SetPackageId(UdpNetCommand.COMMAND_PACKAGE_CHECK_SURE_ORDERID);
            mPackage.Length = Config.nUdpPackageFixedHeadSize;
            mClientPeer.SendNetPackage(mPackage);
            UdpStatistical.AddSendSureOrderIdPackageCount();
        }

        public void Reset()
        {
            mSendStreamList.reset();
            mReSendPackageMgr.Reset();
            while (mCacheReceivePackageList.Count > 0)
            {
                int nRemoveIndex = mCacheReceivePackageList.Count - 1;
                NetUdpFixedSizePackage mRemovePackage = mCacheReceivePackageList[nRemoveIndex];
                mCacheReceivePackageList.RemoveAt(nRemoveIndex);
                mClientPeer.GetObjectPoolManager().NetUdpFixedSizePackage_Recycle(mRemovePackage);
            }

            nCurrentWaitReceiveOrderId = Config.nUdpMinOrderId;
            nLastReceiveOrderId = 0;
        }

        public void Release()
        {

        }
    }
}