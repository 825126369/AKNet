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
using System.Collections.Generic;

namespace AKNet.Udp1Tcp.Common
{
    internal class UdpCheckMgr
    {
        public const int nDefaultSendPackageCount = 1024;
        public const int nDefaultCacheReceivePackageCount = 2048;

        private ushort nCurrentWaitSendOrderId;
        private ushort nCurrentWaitReceiveOrderId;
        private ushort nLastWaitReceiveOrderId;

        public readonly ReSendPackageMgrInterface mReSendPackageMgr = null;
        private readonly NetCombinePackage mCombinePackage = new NetCombinePackage();

        private UdpClientPeerCommonBase mClientPeer = null;
        public UdpCheckMgr(UdpClientPeerCommonBase mClientPeer)
        {
            this.mClientPeer = mClientPeer;

            mReSendPackageMgr = new ReSendPackageMgr3(mClientPeer);
            nCurrentWaitSendOrderId = Config.nUdpMinOrderId;
            nCurrentWaitReceiveOrderId = Config.nUdpMinOrderId;
        }

        private void AddSendPackageOrderId()
        {
            nCurrentWaitSendOrderId = OrderIdHelper.AddOrderId(nCurrentWaitSendOrderId);
        }

        private void AddReceivePackageOrderId()
        {
            nLastWaitReceiveOrderId = nCurrentWaitReceiveOrderId;
            nCurrentWaitReceiveOrderId = OrderIdHelper.AddOrderId(nCurrentWaitReceiveOrderId);
        }

        public void SendLogicPackage(UInt16 id, ReadOnlySpan<byte> buffer)
        {
            if (this.mClientPeer.GetSocketState() != SOCKET_PEER_STATE.CONNECTED) return;

#if DEBUG
            NetLog.Assert(UdpNetCommand.orNeedCheck(id));
            if (buffer.Length > Config.nMaxDataLength)
            {
                NetLog.LogError("超出允许的最大包尺寸：" + Config.nMaxDataLength);
            }
#endif
            if (!buffer.IsEmpty)
            {
                int readBytes = 0;
                int nBeginIndex = 0;

                UInt16 groupCount = 0;
                if (buffer.Length % Config.nUdpPackageFixedBodySize == 0)
                {
                    groupCount = (UInt16)(buffer.Length / Config.nUdpPackageFixedBodySize);
                }
                else
                {
                    groupCount = (UInt16)(buffer.Length / Config.nUdpPackageFixedBodySize + 1);
                }

                while (nBeginIndex < buffer.Length)
                {
                    if (nBeginIndex + Config.nUdpPackageFixedBodySize > buffer.Length)
                    {
                        readBytes = buffer.Length - nBeginIndex;
                    }
                    else
                    {
                        readBytes = Config.nUdpPackageFixedBodySize;
                    }

                    var mPackage = mClientPeer.GetObjectPoolManager().NetUdpFixedSizePackage_Pop();
                    mPackage.nOrderId = (UInt16)nCurrentWaitSendOrderId;
                    mPackage.nGroupCount = groupCount;
                    mPackage.nPackageId = id;
                    mPackage.Length = Config.nUdpPackageFixedHeadSize;
                    mPackage.CopyFrom(buffer.Slice(nBeginIndex, readBytes));

                    groupCount = 0;
                    nBeginIndex += readBytes;
                    AddSendPackageOrderId();
                    AddSendCheck(mPackage);
                }
            }
            else
            {
                var mPackage = mClientPeer.GetObjectPoolManager().NetUdpFixedSizePackage_Pop();
                mPackage.nOrderId = (UInt16)nCurrentWaitSendOrderId;
                mPackage.nGroupCount = 1;
                mPackage.nPackageId = id;
                mPackage.Length = Config.nUdpPackageFixedHeadSize;
                AddSendPackageOrderId();
                AddSendCheck(mPackage);
            }
        }

        private void AddSendCheck(NetUdpFixedSizePackage mCheckPackage)
        {
            NetLog.Assert(mCheckPackage.nOrderId >= Config.nUdpMinOrderId && mCheckPackage.nOrderId <= Config.nUdpMaxOrderId);
            if (Config.bUdpCheck)
            {
                mReSendPackageMgr.Add(mCheckPackage);
            }
            else
            {
                mClientPeer.SendNetPackage(mCheckPackage);
            }
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

                    if (mReceivePackage.nPackageId == UdpNetCommand.COMMAND_PACKAGE_CHECK_SURE_ORDERID)
                    {
                        UdpStatistical.AddReceiveSureOrderIdPackageCount();
                        mReSendPackageMgr.ReceiveOrderIdSurePackage(mReceivePackage.GetPackageCheckSureOrderId());
                    }
                }

                if (mReceivePackage.nPackageId == UdpNetCommand.COMMAND_HEARTBEAT)
                {

                }
                else if (mReceivePackage.nPackageId == UdpNetCommand.COMMAND_CONNECT)
                {
                    this.mClientPeer.ReceiveConnect();
                }
                else if (mReceivePackage.nPackageId == UdpNetCommand.COMMAND_DISCONNECT)
                {
                    this.mClientPeer.ReceiveDisConnect();
                }

                if (UdpNetCommand.orInnerCommand(mReceivePackage.nPackageId))
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
                if (mReceivePackage.nPackageId == UdpNetCommand.COMMAND_CONNECT)
                {
                    this.mClientPeer.ReceiveConnect();
                }
                else if (mReceivePackage.nPackageId == UdpNetCommand.COMMAND_DISCONNECT)
                {
                    this.mClientPeer.ReceiveDisConnect();
                }
                mClientPeer.GetObjectPoolManager().NetUdpFixedSizePackage_Recycle(mReceivePackage);
            }
        }

        readonly List<NetUdpFixedSizePackage> mCacheReceivePackageList = new List<NetUdpFixedSizePackage>(nDefaultCacheReceivePackageCount);
        long nLastCheckReceivePackageLossTime = 0;
        private void CheckReceivePackageLoss(NetUdpFixedSizePackage mPackage)
        {
            UdpStatistical.AddReceiveCheckPackageCount();

            ushort nCurrentWaitSureId = mPackage.nOrderId;
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
                else
                {
                    UdpStatistical.AddGarbagePackageCount();
                    mClientPeer.GetObjectPoolManager().NetUdpFixedSizePackage_Recycle(mPackage);
                }
            }

        }

        private void CheckCombinePackage(NetUdpFixedSizePackage mPackage)
        {
            if (mPackage.nGroupCount > 1)
            {
                if (mCombinePackage.CheckReset())
                {
                    mCombinePackage.Init(mPackage);
                }
                else
                {
                    //残包
                    NetLog.Assert(false, "残包: " + mCombinePackage.nOrderId + " | " + mPackage.nOrderId);
                }
            }
            else if (mPackage.nGroupCount == 1)
            {
                mClientPeer.NetPackageExecute(mPackage);
            }
            else if (mPackage.nGroupCount == 0)
            {
                if (mCombinePackage.Add(mPackage))
                {
                    if (mCombinePackage.CheckCombineFinish())
                    {
                        mClientPeer.NetPackageExecute(mCombinePackage);
                        mCombinePackage.Reset();
                    }
                }
                else
                {
                    //残包
                    NetLog.Assert(false, "残包: " + mCombinePackage.nOrderId + " | " + mPackage.nOrderId);
                }
            }
            else
            {
                NetLog.Assert(false);
            }
            mClientPeer.GetObjectPoolManager().NetUdpFixedSizePackage_Recycle(mPackage);
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

        private void SendLastSureOrderIdPackage(ushort nSureOrderId)
        {
            if (mClientPeer.GetCurrentFrameRemainPackageCount() == 0)
            {
                SendSureOrderIdPackage(nSureOrderId);
            }
        }

        private void SendSureOrderIdPackage(ushort nSureOrderId)
        {
            NetUdpFixedSizePackage mPackage = mClientPeer.GetObjectPoolManager().NetUdpFixedSizePackage_Pop();
            mPackage.nPackageId = UdpNetCommand.COMMAND_PACKAGE_CHECK_SURE_ORDERID;
            mPackage.Length = Config.nUdpPackageFixedHeadSize;
            mPackage.SetPackageCheckSureOrderId(nSureOrderId);
            mClientPeer.SendNetPackage(mPackage);
            UdpStatistical.AddSendSureOrderIdPackageCount();
        }

        public void Reset()
        {
            mReSendPackageMgr.Reset();
            mCombinePackage.Reset();
            while (mCacheReceivePackageList.Count > 0)
            {
                int nRemoveIndex = mCacheReceivePackageList.Count - 1;
                NetUdpFixedSizePackage mRemovePackage = mCacheReceivePackageList[nRemoveIndex];
                mCacheReceivePackageList.RemoveAt(nRemoveIndex);
                mClientPeer.GetObjectPoolManager().NetUdpFixedSizePackage_Recycle(mRemovePackage);
            }

            nCurrentWaitReceiveOrderId = Config.nUdpMinOrderId;
            nCurrentWaitSendOrderId = Config.nUdpMinOrderId;
        }

        public void Release()
        {

        }
    }
}