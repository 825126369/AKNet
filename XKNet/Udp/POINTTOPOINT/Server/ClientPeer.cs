using Google.Protobuf;
using System;
using System.Net;
using XKNet.Common;
using XKNet.Udp.POINTTOPOINT.Common;

namespace XKNet.Udp.POINTTOPOINT.Server
{
    internal class ClientPeer : ClientPeerBase
	{
        internal MsgSendMgr mMsgSendMgr;
        internal MsgReceiveMgr mMsgReceiveMgr;

        internal UdpCheckMgr mUdpCheckPool = null;
		internal UDPLikeTCPMgr mUDPLikeTCPMgr = null;

        protected SERVER_SOCKET_PEER_STATE mSocketPeerState = SERVER_SOCKET_PEER_STATE.NONE;

        private string nClintPeerId = string.Empty;
        private EndPoint remoteEndPoint = null;
        private NetServer mNetServer;

        private bool bInit = false;
        public void Init(NetServer mNetServer)
        {
            if (bInit) return;
            bInit = true;

            this.mNetServer = mNetServer;
            mMsgReceiveMgr = new MsgReceiveMgr(mNetServer, this);
            mMsgSendMgr = new MsgSendMgr(mNetServer, this);

            mUdpCheckPool = new UdpCheckMgr(this);
            mUDPLikeTCPMgr = new UDPLikeTCPMgr(mNetServer, this);

            SetSocketState(SERVER_SOCKET_PEER_STATE.NONE);
        }

        public void Update(double elapsed)
        {
            if (!bInit) return;
            mMsgReceiveMgr.Update(elapsed);
            mUDPLikeTCPMgr.Update(elapsed);
        }

        public void SetSocketState(SERVER_SOCKET_PEER_STATE mState)
        {
            this.mSocketPeerState = mState;
        }

        public SERVER_SOCKET_PEER_STATE GetSocketState()
		{
			return mSocketPeerState;
		}

        public void Reset()
        {
            bInit = false;
            mMsgReceiveMgr.Reset();
            mUdpCheckPool.Reset();
        }

		public void Release()
		{
            Reset();
        }

        public void BindEndPoint(string nPeerId, EndPoint remoteEndPoint)
        {
            this.remoteEndPoint = remoteEndPoint;
            this.nClintPeerId = nPeerId;

            SetSocketState(SERVER_SOCKET_PEER_STATE.CONNECTED);
        }

        private NetEndPointPackage GetNetEndPointPackage(NetUdpFixedSizePackage mNetPackage)
        {
            NetEndPointPackage mPackage = ObjectPoolManager.Instance.mNetEndPointPackagePool.Pop();
            mPackage.mRemoteEndPoint = remoteEndPoint;
            mPackage.mPackage = mNetPackage;
            return mPackage;
        }

        public EndPoint GetIPEndPoint()
        {
            return remoteEndPoint;
        }

        public void SendNetPackage(NetUdpFixedSizePackage mPackage)
        {
            try
            {
                NetEndPointPackage mPackage2 = GetNetEndPointPackage(mPackage);
                mNetServer.mSocketMgr.SendNetPackage(mPackage2);
            }
            catch (Exception)
            {
                mSocketPeerState = SERVER_SOCKET_PEER_STATE.DISCONNECTED;
            }
        }

        public void SendInnerNetData(UInt16 id, IMessage data = null)
        {
            mMsgSendMgr.SendInnerNetData(id, data);
        }

        public void SendNetData(ushort nPackageId, IMessage data = null)
        {
            mMsgSendMgr.SendNetData(nPackageId, data);
        }

        public string GetUUID()
        {
            return nClintPeerId;
        }
    }
}
