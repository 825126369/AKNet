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
    internal partial class ConnectionPeer
    {
        public void AddReceivePackageOrderId(int nLength)
        {
            nCurrentWaitReceiveOrderId = OrderIdHelper.AddOrderId(nCurrentWaitReceiveOrderId, nLength);
            nSameOrderIdSureCount = 0;
        }

        public void SendTcpStream(ReadOnlySpan<byte> buffer)
        {
            MainThreadCheck.Check();
            if (!m_Connected) return;
#if DEBUG
            if (buffer.Length > Config.nMaxDataLength)
            {
                NetLog.LogError("超出允许的最大包尺寸：" + Config.nMaxDataLength);
            }
#endif
            AddTcpStream(buffer);
        }

        public void ReceiveNetPackage(NetUdpReceiveFixedSizePackage mReceivePackage)
        {
            UdpStatistical.AddReceivePackageCount();
            byte nInnerCommandId = mReceivePackage.GetInnerCommandId();
            MainThreadCheck.Check();
            if (m_Connected)
            {
                ReceiveHeartBeat();

                if (mReceivePackage.nRequestOrderId > 0)
                {
                    ReceiveOrderIdRequestPackage(mReceivePackage.nRequestOrderId);
                }

                if (nInnerCommandId == UdpNetCommand.COMMAND_HEARTBEAT)
                {

                }
                else if (nInnerCommandId == UdpNetCommand.COMMAND_CONNECT)
                {
                    ReceiveConnect();
                }
                else if (nInnerCommandId == UdpNetCommand.COMMAND_DISCONNECT)
                {
                    ReceiveDisConnect();
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

            if (nSameOrderIdSureCount == 0 && mClientPeer.GetCurrentFrameRemainPackageCount() == 0)
            {
                SendSureOrderIdPackage();
            }
        }

        private void CheckCombinePackage(NetUdpReceiveFixedSizePackage mCheckPackage)
        {
            mClientPeer.ReceiveTcpStream(mCheckPackage);
            mClientPeer.GetObjectPoolManager().UdpReceivePackage_Recycle(mCheckPackage);
        }

        public void SetRequestOrderId(NetUdpSendFixedSizePackage mPackage)
        {
            mPackage.nRequestOrderId = nCurrentWaitReceiveOrderId;
            nSameOrderIdSureCount++;
        }

        private void SendSureOrderIdPackage()
        {
            mClientPeer.SendInnerNetData(UdpNetCommand.COMMAND_PACKAGE_CHECK_SURE_ORDERID);
            UdpStatistical.AddSendSureOrderIdPackageCount();
        }
    }
}