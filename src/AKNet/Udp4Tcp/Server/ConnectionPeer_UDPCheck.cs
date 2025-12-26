/************************************Copyright*****************************************
*        ProjectName:AKNet
*        Web:https://github.com/825126369/AKNet
*        Description:C#游戏网络库
*        Author:许珂
*        StartTime:2024/11/01 00:00:00
*        ModifyTime:2025/11/30 19:43:16
*        Copyright:MIT软件许可证
************************************Copyright*****************************************/
using AKNet.Common;
using AKNet.Udp4Tcp.Common;
using System;
using System.Net.Sockets;

namespace AKNet.Udp4Tcp.Server
{
    internal partial class ConnectionPeer : UdpClientPeerCommonBase
    {
        public void SendNetPackage(NetUdpSendFixedSizePackage mPackage)
        {
            bool bCanSendPackage = mPackage.orInnerCommandPackage() ||
                GetSocketState() == SOCKET_PEER_STATE.CONNECTED;

            if (bCanSendPackage)
            {
                UdpStatistical.AddSendPackageCount();
                mUdpCheckPool.SetRequestOrderId(mPackage);
                if (mPackage.orInnerCommandPackage())
                {
                    this.SendNetPackage2(mPackage);
                }
                else
                {
                    UdpStatistical.AddSendCheckPackageCount();
                    this.SendNetPackage2(mPackage);
                }
            }
        }

        public ObjectPoolManager GetObjectPoolManager()
        {
            return mServerMgr.GetObjectPoolManager();
        }

        public void NetPackageExecute(NetPackage mPackage)
        {
            mServerMgr.GetPackageManager().NetPackageExecute(this, mPackage);
        }
    }
}
