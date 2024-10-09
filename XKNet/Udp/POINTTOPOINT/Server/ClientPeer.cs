using XKNet.Common;

namespace XKNet.Udp.Server
{
    internal class ClientPeer : UDPLikeTCPPeer
	{
		public void Init(NetServer mNetServer)
		{
			base.mNetServer = mNetServer;
		}

		public override void Update(double elapsed)
		{
			base.Update(elapsed);
		}

		public SERVER_SOCKET_PEER_STATE GetSocketState()
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
