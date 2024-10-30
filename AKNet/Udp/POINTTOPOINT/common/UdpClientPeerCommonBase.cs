/************************************Copyright*****************************************
*        ProjectName:AKNet
*        Web:https://github.com/825126369/AKNet
*        Description:AKNet 网络库, 兼容 C#8.0 和 .Net Standard 2.1
*        Author:阿珂
*        CreateTime:2024/10/30 21:55:41
*        Copyright:MIT软件许可证
************************************Copyright*****************************************/
using System;
using AKNet.Common;

namespace AKNet.Udp.POINTTOPOINT.Common
{
    internal interface UdpClientPeerCommonBase
    {
        SOCKET_PEER_STATE GetSocketState();
        void SendNetPackage(NetUdpFixedSizePackage mPackage);
        void SendInnerNetData(UInt16 id);
        void AddLogicHandleQueue(NetPackage mPackage);
        public void ResetSendHeartBeatCdTime();
        public void ReceiveHeartBeat();
        public void ReceiveConnect();
        public void ReceiveDisConnect();
        public ObjectPoolManager GetObjectPoolManager();
    }
}
