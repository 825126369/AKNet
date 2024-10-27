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
        private SOCKET_PEER_STATE mSocketPeerState = SOCKET_PEER_STATE.NONE;
        private IPEndPoint remoteEndPoint = null;
        private UdpServer mNetServer;
        private string Name = string.Empty;
        
        public ClientPeer(UdpServer mNetServer)
        {
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
            if (this.mSocketPeerState != mState)
            {
                this.mSocketPeerState = mState;
                this.mNetServer.mListenSocketStateFunc?.Invoke(this);
            }
        }

        public SOCKET_PEER_STATE GetSocketState()
		{
			return mSocketPeerState;
		}

        public void Reset()
        {
            mMsgReceiveMgr.Reset();
            mUdpCheckPool.Reset();
            SetSocketState(SOCKET_PEER_STATE.DISCONNECTED);
        }

        public void BindEndPoint(EndPoint remoteEndPoint)
        {
            this.remoteEndPoint = remoteEndPoint as IPEndPoint;
            SetSocketState(SOCKET_PEER_STATE.CONNECTED);
        }

        public IPEndPoint GetIPEndPoint()
        {
            return remoteEndPoint;
        }

        public string GetIPAddress()
        {
            return GetIPEndPoint().Address.ToString();
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

        public void SendNetData(NetPackage mNetPackage)
        {
            mMsgSendMgr.SendNetData(mNetPackage);
        }

        public void SendNetData(ushort nPackageId, ReadOnlySpan<byte> buffer)
        {
            mMsgSendMgr.SendNetData(nPackageId, buffer);
        }

        public void SetName(string name)
        {
            this.Name = name;
        }

        public string GetName()
        {
            return this.Name;
        }
    }
}
