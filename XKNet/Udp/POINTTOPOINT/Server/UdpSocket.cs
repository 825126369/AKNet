using System;
using System.Net;
using XKNetCommon;
using XKNetUdpCommon;

namespace XKNetUdpServer
{
    public class SocketUdp : SocketReceivePeer
	{
		private string nClintPeerId = string.Empty;
		private EndPoint remoteEndPoint = null;
		protected SERVER_SOCKET_PEER_STATE mSocketPeerState = SERVER_SOCKET_PEER_STATE.NONE;

		public void AcceptClient(string nPeerId, EndPoint remoteEndPoint)
		{
			this.mSocketPeerState = SERVER_SOCKET_PEER_STATE.CONNECTED;
			this.remoteEndPoint = remoteEndPoint;
			this.nClintPeerId = nPeerId;
		}

		public EndPoint GetIpEndPoint()
		{
			return remoteEndPoint;
		}

		public string GetUUID()
		{
			return nClintPeerId;
		}

		public void SendNetPackage(NetUdpFixedSizePackage mNetPackage)
		{
			NetEndPointPackage mPackage = ObjectPoolManager.Instance.mNetEndPointPackagePool.Pop();
			mPackage.mRemoteEndPoint = remoteEndPoint;
			mPackage.mPackage = mNetPackage;

			try
			{
				mNetServer.SendNetPackage(mPackage);
			}
			catch (Exception)
			{
				mSocketPeerState = SERVER_SOCKET_PEER_STATE.DISCONNECTED;
			}
		}

	}

}









