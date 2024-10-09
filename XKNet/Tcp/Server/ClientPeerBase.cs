using Google.Protobuf;
using System.Net;
using System.Net.Sockets;
using XKNet.Common;
using XKNet.Tcp.Common;

namespace XKNet.Tcp.Server
{
	public interface ClientPeerBase
	{
		void ConnectClient(Socket mSocket);

        SERVER_SOCKET_PEER_STATE GetSocketState();
		void Update(double elapsed);
		void SendNetData(ushort nPackageId, IMessage data = null);
		void Reset();

        IPEndPoint GetIPEndPoint();
		uint GetUUID();
    }
}
