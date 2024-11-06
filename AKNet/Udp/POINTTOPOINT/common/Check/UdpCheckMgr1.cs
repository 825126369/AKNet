/************************************Copyright*****************************************
*        ProjectName:AKNet
*        Web:https://github.com/825126369/AKNet
*        Description:这是一个面向 .Net Standard 2.1 的游戏网络库
*        Author:阿珂
*        CreateTime:2024/11/4 20:04:54
*        Copyright:MIT软件许可证
************************************Copyright*****************************************/
using Google.Protobuf.Collections;
using System;
using System.Collections.Generic;
using AKNet.Common;

namespace AKNet.Udp.POINTTOPOINT.Common
{
    internal class UdpCheckMgr1: UdpCheckMgrInterface
    {
        public const int nDefaultSendPackageCount = 100;
        public const int nDefaultCacheReceivePackageCount = 200;
        private ushort nCurrentWaitSendOrderId;
        private ushort nCurrentWaitReceiveOrderId;
        private ushort nTellMyWaitReceiveOrderId;
        
        public readonly CheckPackageMgr mCheckPackageMgr = null;
        private NetCombinePackage mCombinePackage = null;

        private UdpClientPeerCommonBase mClientPeer = null;
        public UdpCheckMgr1(UdpClientPeerCommonBase mClientPeer)
        {
            this.mClientPeer = mClientPeer;

            mCheckPackageMgr = new CheckPackageMgr(mClientPeer);
            nCurrentWaitSendOrderId = Config.nUdpMinOrderId;
            nCurrentWaitReceiveOrderId = Config.nUdpMinOrderId;
        }

        private void AddSendPackageOrderId()
        {
            nCurrentWaitSendOrderId = OrderIdHelper.AddOrderId(nCurrentWaitSendOrderId);
        }

        private void AddReceivePackageOrderId()
        {
            nTellMyWaitReceiveOrderId = nCurrentWaitReceiveOrderId;
            nCurrentWaitReceiveOrderId = OrderIdHelper.AddOrderId(nCurrentWaitReceiveOrderId);
        }

        public void SetSureOrderId(NetUdpFixedSizePackage mPackage)
        {
            mPackage.nSureOrderId = nTellMyWaitReceiveOrderId;
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
            this.mClientPeer.ReceiveHeartBeat();
            if (Config.bUdpCheck)
            {
                if (mReceivePackage.nSureOrderId > 0)
                {
                    mCheckPackageMgr.ReceiveCheckPackage(mReceivePackage.nSureOrderId);
                }
            }

            if (mReceivePackage.nPackageId == UdpNetCommand.COMMAND_PACKAGECHECK)
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
                //NetLog.Log("CheckReceivePackageLoss: " + mPackage.nOrderId + " | " + nTellMyWaitReceiveOrderId + " | " + nCurrentWaitReceiveOrderId);
                if (OrderIdHelper.orInOrderIdFront(nCurrentWaitReceiveOrderId, mPackage.nOrderId, nDefaultSendPackageCount) &&
                    mCacheReceivePackageList.Find(x => x.nOrderId == mPackage.nOrderId) == null &&
                    mCacheReceivePackageList.Count < mCacheReceivePackageList.Capacity)
                {
                    mCacheReceivePackageList.Add(mPackage);
                }
                else
                {
                    mClientPeer.GetObjectPoolManager().NetUdpFixedSizePackage_Recycle(mPackage);
                }
            }
            nTellMyWaitReceiveOrderId = OrderIdHelper.MinusOrderId(nCurrentWaitReceiveOrderId);
            mClientPeer.SendInnerNetData(UdpNetCommand.COMMAND_PACKAGECHECK);
        }

        private void CheckCombinePackage(NetUdpFixedSizePackage mPackage)
        {
            if (mPackage.nGroupCount > 1)
            {
                mCombinePackage = mClientPeer.GetObjectPoolManager().NetCombinePackage_Pop();
                mCombinePackage.Init(mPackage);
                mClientPeer.GetObjectPoolManager().NetUdpFixedSizePackage_Recycle(mPackage);
            }
            else if (mPackage.nGroupCount == 1)
            {
                mClientPeer.AddLogicHandleQueue(mPackage);
            }
            else if (mPackage.nGroupCount == 0)
            {
                if (mCombinePackage != null)
                {
                    if (mCombinePackage.Add(mPackage))
                    {
                        if (mCombinePackage.CheckCombineFinish())
                        {
                            mClientPeer.AddLogicHandleQueue(mCombinePackage);
                            mCombinePackage = null;
                        }
                    }
                    else
                    {
                        //残包
                        NetLog.Assert(false, "残包");
                    }
                }
                else
                {
                    //残包
                    NetLog.Assert(false, "残包");
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

        public void Reset()
        {
            mCheckPackageMgr.Reset();

            if (mCombinePackage != null)
            {
                mClientPeer.GetObjectPoolManager().NetCombinePackage_Recycle(mCombinePackage);
                mCombinePackage = null;
            }
            
            while (mCacheReceivePackageList.Count > 0)
            {
                int nRemoveIndex = mCacheReceivePackageList.Count - 1;
                NetUdpFixedSizePackage mRemovePackage = mCacheReceivePackageList[nRemoveIndex];
                mCacheReceivePackageList.RemoveAt(nRemoveIndex);
                mClientPeer.GetObjectPoolManager().NetUdpFixedSizePackage_Recycle(mRemovePackage);
            }

            nCurrentWaitReceiveOrderId = Config.nUdpMinOrderId;
            nCurrentWaitSendOrderId = Config.nUdpMinOrderId;
            nTellMyWaitReceiveOrderId = 0;
        }

        public void Release()
        {

        }
    }
}