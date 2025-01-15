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

namespace AKNet.Udp4LinuxTcp.Common
{
    internal class UdpCheckMgr
    {
        private UdpClientPeerCommonBase mClientPeer = null;
        public UdpCheckMgr(UdpClientPeerCommonBase mClientPeer)
        {
            this.mClientPeer = mClientPeer;
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
            //mReSendPackageMgr.AddTcpStream(buffer);
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
                   // mReSendPackageMgr.ReceiveOrderIdRequestPackage(mReceivePackage.nRequestOrderId);
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
                   // CheckReceivePackageLoss(mReceivePackage);
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

        private void CheckCombinePackage(NetUdpReceiveFixedSizePackage mCheckPackage)
        {
            mClientPeer.ReceiveTcpStream(mCheckPackage);
            mClientPeer.GetObjectPoolManager().UdpReceivePackage_Recycle(mCheckPackage);
        }

        public void Update(double elapsed)
        {
            if (mClientPeer.GetSocketState() != SOCKET_PEER_STATE.CONNECTED) return;
            //mReSendPackageMgr.Update(elapsed);
        }

        public void SetRequestOrderId(NetUdpSendFixedSizePackage mPackage)
        {
            //mPackage.nRequestOrderId = nCurrentWaitReceiveOrderId;
            //nSameOrderIdSureCount++;
        }

        private void SendSureOrderIdPackage()
        {
            mClientPeer.SendInnerNetData(UdpNetCommand.COMMAND_PACKAGE_CHECK_SURE_ORDERID);
            UdpStatistical.AddSendSureOrderIdPackageCount();
        }

        public void Reset()
        {
            //mReSendPackageMgr.Reset();
            //while (mCacheReceivePackageList.Count > 0)
            //{
            //    int nRemoveIndex = mCacheReceivePackageList.Count - 1;
            //    NetUdpReceiveFixedSizePackage mRemovePackage = mCacheReceivePackageList[nRemoveIndex];
            //    mCacheReceivePackageList.RemoveAt(nRemoveIndex);
            //    mClientPeer.GetObjectPoolManager().UdpReceivePackage_Recycle(mRemovePackage);
            //}

            //nCurrentWaitReceiveOrderId = Config.nUdpMinOrderId;
        }

        public void Release()
        {

        }
    }
}