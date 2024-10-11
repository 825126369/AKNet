using Google.Protobuf;
using XKNet.Common;

namespace XKNet.Udp.POINTTOPOINT.Server
{
    public interface ClientPeerBase
    {
#if DEBUG
        void Reset();
        void Release();

#endif
        SERVER_SOCKET_PEER_STATE GetSocketState();
        void SendNetData(ushort nPackageId, IMessage data = null);
    }
}
