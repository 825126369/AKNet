/************************************Copyright*****************************************
*        ProjectName:AKNet
*        Web:https://github.com/825126369/AKNet
*        Description:这是一个面向 .Net Standard 2.1 的游戏网络库
*        Author:阿珂
*        CreateTime:2024/11/7 21:38:44
*        Copyright:MIT软件许可证
************************************Copyright*****************************************/
using AKNet.Common;
using System;
using System.Collections.Generic;

namespace AKNet.Udp.POINTTOPOINT.Common
{
    internal class UdpCheckMgr
    {
        public const int nDefaultSendPackageCount = 100;
        public const int nDefaultCacheReceivePackageCount = 200;

        private ushort nCurrentWaitSendOrderId;
        private ushort nCurrentWaitReceiveOrderId;

        public readonly CheckPackageMgrInterface mCheckPackageMgr = null;
        private readonly NetCombinePackage mCombinePackage = new NetCombinePackage();

        private UdpClientPeerCommonBase mClientPeer = null;
        public UdpCheckMgr(UdpClientPeerCommonBase mClientPeer)
        {
            this.mClientPeer = mClientPeer;

            mCheckPackageMgr = new CheckPackageMgr1(mClientPeer);
            nCurrentWaitSendOrderId = Config.nUdpMinOrderId;
            nCurrentWaitReceiveOrderId = Config.nUdpMinOrderId;
        }

        private void AddSendPackageOrderId()
        {
            nCurrentWaitSendOrderId = OrderIdHelper.AddOrderId(nCurrentWaitSendOrderId);
        }

        private void AddReceivePackageOrderId()
        {
            nCurrentWaitReceiveOrderId = OrderIdHelper.AddOrderId(nCurrentWaitReceiveOrderId);
        }

        public void SendLogicPackage(UInt16 id, ReadOnlySpan<byte> buffer)
        {
            if (this.mClientPeer.GetSocketState() != SOCKET_PEER_STATE.CONNECTED) return;

            NetLog.Assert(buffer.Length <= Config.nMsgPackageBufferMaxLength, "超出允许的最大包尺寸：" + Config.nMsgPackageBufferMaxLength);
            NetLog.Assert(UdpNetCommand.orNeedCheck(id));
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
                    mPackage.CopyFromMsgStream(buffer, nBeginIndex, readBytes);

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

        private void AddSendCheck(NetUdpFixedSizePackage mPackage)
        {
            NetLog.Assert(mPackage.nOrderId >= Config.nUdpMinOrderId && mPackage.nOrderId <= Config.nUdpMaxOrderId);
            if (Config.bUdpCheck)
            {
                mCheckPackageMgr.Add(mPackage);
            }
            else
            {
                mClientPeer.SendNetPackage(mPackage);
            }
        }

        public void ReceiveNetPackage(NetUdpFixedSizePackage mReceivePackage)
        {
            MainThreadCheck.Check();
            this.mClientPeer.ReceiveHeartBeat();

            if (Config.bUdpCheck)
            {
                if (mReceivePackage.GetRequestOrderId() > 0)
                {
                    mCheckPackageMgr.ReceiveOrderIdRequestPackage(mReceivePackage.GetRequestOrderId());
                }
            }

            if (mReceivePackage.nPackageId == UdpNetCommand.COMMAND_PACKAGE_CHECK_SURE_ORDERID)
            {
                mCheckPackageMgr.ReceiveOrderIdSurePackage(mReceivePackage.GetPackageCheckSureOrderId());
            }
            else if (mReceivePackage.nPackageId == UdpNetCommand.COMMAND_PACKAGE_CHECK_REQUEST_ORDERID)
            {
                
            }
            else if (mReceivePackage.nPackageId == UdpNetCommand.COMMAND_HEARTBEAT)
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
                if (mClientPeer.GetSocketState() == SOCKET_PEER_STATE.CONNECTED)
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
                else
                {
                    mClientPeer.GetObjectPoolManager().NetUdpFixedSizePackage_Recycle(mReceivePackage);
                }
            }
        }

        readonly List<NetUdpFixedSizePackage> mCacheReceivePackageList = new List<NetUdpFixedSizePackage>(nDefaultCacheReceivePackageCount);
        private void CheckReceivePackageLoss(NetUdpFixedSizePackage mPackage)
        {
            if (mPackage.nOrderId == nCurrentWaitReceiveOrderId)
            {
                SendSureOrderIdPackage(mPackage.nOrderId);

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
            }
            else
            {
                if (OrderIdHelper.orInOrderIdFront(nCurrentWaitReceiveOrderId, mPackage.nOrderId, nDefaultSendPackageCount) &&
                    mCacheReceivePackageList.Find(x => x.nOrderId == mPackage.nOrderId) == null &&
                    mCacheReceivePackageList.Count < mCacheReceivePackageList.Capacity)
                {
                    SendSureOrderIdPackage(mPackage.nOrderId);
                    mCacheReceivePackageList.Add(mPackage);
                }
                else
                {
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

                mClientPeer.GetObjectPoolManager().NetUdpFixedSizePackage_Recycle(mPackage);
            }
            else if (mPackage.nGroupCount == 1)
            {
                mClientPeer.AddLogicHandleQueue(mPackage);
            }
            else if (mPackage.nGroupCount == 0)
            {
                if (mCombinePackage.Add(mPackage))
                {
                    if (mCombinePackage.CheckCombineFinish())
                    {
                        mClientPeer.AddLogicHandleQueue(mCombinePackage);
                        mCombinePackage.Reset();
                    }
                }
                else
                {
                    //残包
                    NetLog.Assert(false, "残包: " + mCombinePackage.nOrderId + " | " + mPackage.nOrderId);
                }
                mClientPeer.GetObjectPoolManager().NetUdpFixedSizePackage_Recycle(mPackage);
            }
            else
            {
                NetLog.Assert(false);
            }
        }

        public void Update(double elapsed)
        {
            mCheckPackageMgr.Update(elapsed);
        }

        public void SetRequestOrderId(NetUdpFixedSizePackage mPackage)
        {
            mPackage.SetRequestOrderId(nCurrentWaitReceiveOrderId);
        }

        private void SendSureOrderIdPackage(ushort nSureOrderId)
        {
            NetUdpFixedSizePackage mPackage = mClientPeer.GetObjectPoolManager().NetUdpFixedSizePackage_Pop();
            mPackage.nPackageId = UdpNetCommand.COMMAND_PACKAGE_CHECK_SURE_ORDERID;
            mPackage.Length = Config.nUdpPackageFixedHeadSize;
            mPackage.SetPackageCheckSureOrderId(nSureOrderId);
            mClientPeer.SendNetPackage(mPackage);
        }

        public void Reset()
        {
            mCheckPackageMgr.Reset();
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