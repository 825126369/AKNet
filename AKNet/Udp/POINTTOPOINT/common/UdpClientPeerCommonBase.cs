/************************************Copyright*****************************************
*        ProjectName:AKNet
*        Web:https://github.com/825126369/AKNet
*        Description:这是一个面向 .Net Standard 2.1 的游戏网络库
*        Author:阿珂
*        CreateTime:2024/11/17 12:39:36
*        Copyright:MIT软件许可证
************************************Copyright*****************************************/
using System;
using System.Net;
using AKNet.Common;

namespace AKNet.Udp.POINTTOPOINT.Common
{
    internal interface UdpClientPeerCommonBase
    {
        void SetSocketState(SOCKET_PEER_STATE mState);
        SOCKET_PEER_STATE GetSocketState();
        void SendNetPackage(NetUdpFixedSizePackage mPackage);
        void SendInnerNetData(UInt16 id);
        void AddLogicHandleQueue(NetPackage mPackage);
        public void ResetSendHeartBeatCdTime();
        public void ReceiveHeartBeat();
        public void ReceiveConnect();
        public void ReceiveDisConnect();
        public ObjectPoolManager GetObjectPoolManager();
        IPEndPoint GetIPEndPoint();
        TcpStanardRTOFunc GetTcpStanardRTOFunc();
    }
}
