﻿/************************************Copyright*****************************************
*        ProjectName:AKNet
*        Web:https://github.com/825126369/AKNet
*        Description:这是一个面向 .Net Standard 2.1 的游戏网络库
*        Author:阿珂
*        CreateTime:2024/12/20 10:55:53
*        Copyright:MIT软件许可证
************************************Copyright*****************************************/
using System;
using System.Net;
using AKNet.Common;

namespace AKNet.Udp3Tcp.Common
{
    internal interface UdpClientPeerCommonBase
    {
        void SetSocketState(SOCKET_PEER_STATE mState);
        SOCKET_PEER_STATE GetSocketState();
        void ReceiveTcpStream(NetUdpReceiveFixedSizePackage mPackage);
        void SendNetPackage(NetUdpSendFixedSizePackage mPackage);
        void SendInnerNetData(byte id);
        void NetPackageExecute(NetPackage mPackage);
        public void ResetSendHeartBeatCdTime();
        public void ReceiveHeartBeat();
        public void ReceiveConnect();
        public void ReceiveDisConnect();
        public ObjectPoolManager GetObjectPoolManager();
        IPEndPoint GetIPEndPoint();
        Config GetConfig();
        int GetCurrentFrameRemainPackageCount();
    }
}
