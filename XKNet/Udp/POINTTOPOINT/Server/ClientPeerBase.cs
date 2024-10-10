using Google.Protobuf;
using System.Net;
using System.Net.Sockets;
using XKNet.Common;

namespace XKNet.Udp.POINTTOPOINT.Server
{
    public interface ClientPeerBase
	{
        SERVER_SOCKET_PEER_STATE GetSocketState();
		void SendNetData(ushort nPackageId, IMessage data = null);
    }
}
