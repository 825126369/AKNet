using System;
using XKNet.Common;

namespace XKNet.Udp.POINTTOPOINT.Common
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
    }
}
