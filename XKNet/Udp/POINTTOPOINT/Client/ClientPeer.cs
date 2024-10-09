using XKNet.Common;

namespace XKNet.Udp.Client
{
    public class ClientPeer : UDPLikeTCPPeer
	{
		public override void Update (double elapsed)
		{
			base.Update (elapsed);
		}

		public CLIENT_SOCKET_PEER_STATE GetSocketState()
		{
			return mSocketPeerState;
		}

		public override void Reset()
		{
			base.Reset();
		}

		public override void Release()
		{
			base.Release();
		}
	}
}
