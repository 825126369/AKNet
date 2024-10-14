using Google.Protobuf;

namespace XKNet.Common
{
    public interface ClientPeerBase
    {
        SOCKET_PEER_STATE GetSocketState();
        void SendNetData(ushort nPackageId);
        void SendNetData(ushort nPackageId, IMessage data);
        void SendNetData(ushort nPackageId, byte[] data);
    }
}
