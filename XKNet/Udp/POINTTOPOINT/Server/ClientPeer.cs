using Google.Protobuf;
using System;
using System.Net;
using XKNet.Common;
using XKNet.Udp.POINTTOPOINT.Common;

namespace XKNet.Udp.POINTTOPOINT.Server
{
    internal class ClientPeer : UdpClientPeerBase, ClientPeerBase
	{
        internal MsgSendMgr mMsgSendMgr;
        internal MsgReceiveMgr mMsgReceiveMgr;

        internal UdpCheckMgr mUdpCheckPool = null;
		internal UDPLikeTCPMgr mUDPLikeTCPMgr = null;

        protected SOCKET_PEER_STATE mSocketPeerState = SOCKET_PEER_STATE.NONE;

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

            SetSocketState(SOCKET_PEER_STATE.NONE);
        }

        public void Update(double elapsed)
        {
            if (mMsgReceiveMgr != null)
            {
                mMsgReceiveMgr.Update(elapsed);
            }

            if (mUDPLikeTCPMgr != null)
            {
                mUDPLikeTCPMgr.Update(elapsed);
            }
        }

        public void SetSocketState(SOCKET_PEER_STATE mState)
        {
            this.mSocketPeerState = mState;
        }

        public SOCKET_PEER_STATE GetSocketState()
		{
			return mSocketPeerState;
		}

        public void Reset()
        {
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

            SetSocketState(SOCKET_PEER_STATE.CONNECTED);
        }

        public EndPoint GetIPEndPoint()
        {
            return remoteEndPoint;
        }

        public void SendNetPackage(NetUdpFixedSizePackage mPackage)
        {
            try
            {
                mPackage.remoteEndPoint = remoteEndPoint;
                mNetServer.mSocketMgr.SendNetPackage(mPackage);
            }
            catch (Exception)
            {
                mSocketPeerState = SOCKET_PEER_STATE.DISCONNECTED;
            }
        }

        public void SendInnerNetData(UInt16 id, IMessage data = null)
        {
            mMsgSendMgr.SendInnerNetData(id, data);
        }

        public void SendNetData(ushort nPackageId)
        {
            mMsgSendMgr.SendNetData(nPackageId);
        }

        public void SendNetData(ushort nPackageId, IMessage data)
        {
            mMsgSendMgr.SendNetData(nPackageId, data);
        }

        public void SendNetData(ushort nPackageId, byte[] data)
        {
            mMsgSendMgr.SendNetData(nPackageId, data);
        }

        public string GetUUID()
        {
            return nClintPeerId;
        }
    }
}
