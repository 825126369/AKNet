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

        internal UdpCheck3Pool mUdpCheckPool = null;
		internal UDPLikeTCPMgr mUDPLikeTCPMgr = null;

        protected SERVER_SOCKET_PEER_STATE mSocketPeerState = SERVER_SOCKET_PEER_STATE.NONE;

        private string nClintPeerId = string.Empty;
        private EndPoint remoteEndPoint = null;
        NetServer mNetServer;
        public void Init(NetServer mNetServer)
        {
            this.mNetServer = mNetServer;
            mMsgReceiveMgr = new MsgReceiveMgr(mNetServer, this);
            mMsgSendMgr = new MsgSendMgr(mNetServer, this);

            mUdpCheckPool = new UdpCheck3Pool(this);
            mUDPLikeTCPMgr = new UDPLikeTCPMgr(mNetServer, this);

            SetSocketState(SERVER_SOCKET_PEER_STATE.NONE);
        }

		public void Update(double elapsed)
		{
            mMsgReceiveMgr.Update(elapsed);
            mUdpCheckPool.Update(elapsed);
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
            mMsgReceiveMgr.Reset();
            mUdpCheckPool.Reset();
        }

		public void Release()
		{
            mMsgReceiveMgr.Release();
            mUdpCheckPool.Release();
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

        public void SendNetData(ushort nPackageId, IMessage data = null)
        {
            mMsgSendMgr.SendNetData(nPackageId, data);
        }

        public string GetUUID()
        {
            return nClintPeerId;
        }

        public NetUdpFixedSizePackage GetUdpSystemPackage(UInt16 id, IMessage data = null)
        {
            NetLog.Assert(UdpNetCommand.orNeedCheck(id) == false);

            var mPackage = ObjectPoolManager.Instance.mUdpFixedSizePackagePool.Pop();
            mPackage.nOrderId = 0;
            mPackage.nGroupCount = 0;
            mPackage.nPackageId = id;
            mPackage.Length = Config.nUdpPackageFixedHeadSize;

            if (data != null)
            {
                byte[] cacheSendBuffer = ObjectPoolManager.Instance.nSendBufferPool.Pop(Config.nUdpCombinePackageFixedSize);
                Span<byte> stream = Protocol3Utility.SerializePackage(data, cacheSendBuffer);
                mPackage.Length += stream.Length;
                for (int i = 0; i < stream.Length; i++)
                {
                    mPackage.buffer[Config.nUdpPackageFixedHeadSize + i] = stream[i];
                }
                ObjectPoolManager.Instance.nSendBufferPool.recycle(cacheSendBuffer);
            }

            NetPackageEncryption.Encryption(mPackage);
            return mPackage;
        }
    }
}
