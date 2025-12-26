/************************************Copyright*****************************************
*        ProjectName:AKNet
*        Web:https://github.com/825126369/AKNet
*        Description:C#游戏网络库
*        Author:许珂
*        StartTime:2024/11/01 00:00:00
*        ModifyTime:2025/11/30 19:43:16
*        Copyright:MIT软件许可证
************************************Copyright*****************************************/
using System;
using System.Collections.Generic;
using System.Net.Sockets;
using AKNet.Common;
using AKNet.Udp4Tcp.Common;

namespace AKNet.Udp4Tcp.Client
{
    internal partial class ClientPeer
    {
        public int GetCurrentFrameRemainPackageCount()
        {
            return nCurrentCheckPackageCount;
        }

        private bool NetTcpPackageExecute()
        {
            bool bSuccess = mCryptoMgr.Decode(mReceiveStreamList, mNetPackage);
            if (bSuccess)
            {
                NetPackageExecute(mNetPackage);
            }
            return bSuccess;
        }

        public void ReceiveTcpStream(NetUdpReceiveFixedSizePackage mPackage)
        {
            mReceiveStreamList.WriteFrom(mPackage.GetTcpBufferSpan());
            while (NetTcpPackageExecute())
            {

            }
        }
    }
}