using Google.Protobuf;
using System;

namespace XKNet.Common
{
    public interface ClientPeerBase
    {
        string GetName();
        string GetIPAddress();
        SOCKET_PEER_STATE GetSocketState();
        void SendNetData(ushort nPackageId);
        void SendNetData(ushort nPackageId, IMessage data);
        void SendNetData(ushort nPackageId, byte[] data);
        void SendNetData(NetPackage mNetPackage);
        void SendNetData(UInt16 nPackageId, ReadOnlySpan<byte> buffer);
    }
}
