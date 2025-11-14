/************************************Copyright*****************************************
*        ProjectName:AKNet
*        Web:https://github.com/825126369/AKNet
*        Description:C#游戏网络库
*        Author:许珂
*        ModifyTime:2025/11/14 8:26:51
*        Copyright:MIT软件许可证
************************************Copyright*****************************************/
using System;
using System.Net;
using AKNet.Common;

namespace AKNet.Udp2Tcp.Common
{
    internal interface UdpClientPeerCommonBase
    {
        void SetSocketState(SOCKET_PEER_STATE mState);
        SOCKET_PEER_STATE GetSocketState();
        void ReceiveTcpStream(NetUdpFixedSizePackage mPackage);
        void SendNetPackage(NetUdpFixedSizePackage mPackage);
        void SendInnerNetData(UInt16 id);
        void NetPackageExecute(NetPackage mPackage);
        public void ResetSendHeartBeatCdTime();
        public void ReceiveHeartBeat();
        public void ReceiveConnect();
        public void ReceiveDisConnect();
        public ObjectPoolManager GetObjectPoolManager();
        IPEndPoint GetIPEndPoint();
        TcpStanardRTOFunc GetTcpStanardRTOFunc();
        Config GetConfig();
        int GetCurrentFrameRemainPackageCount();
    }
}
