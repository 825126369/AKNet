/************************************Copyright*****************************************
*        ProjectName:AKNet
*        Web:https://github.com/825126369/AKNet
*        Description:这是一个面向 .Net Standard 2.1 的游戏网络库
*        Author:阿珂
*        CreateTime:2024/11/23 22:12:37
*        Copyright:MIT软件许可证
************************************Copyright*****************************************/
using Google.Protobuf;
using System;
using System.Net;
using AKNet.Common;
using AKNet.Udp.POINTTOPOINT.Common;

namespace AKNet.Udp.POINTTOPOINT.Server
{
    internal class ClientPeer : UdpClientPeerCommonBase, UdpClientPeerBase, ClientPeerBase
	{
        internal MsgSendMgr mMsgSendMgr;
        internal MsgReceiveMgr mMsgReceiveMgr;
        internal ClientPeerSocketMgr mSocketMgr;

        internal UdpCheckMgr mUdpCheckPool = null;
		internal UDPLikeTCPMgr mUDPLikeTCPMgr = null;
        private SOCKET_PEER_STATE mSocketPeerState = SOCKET_PEER_STATE.NONE;
        private IPEndPoint remoteEndPoint = null;
        private UdpServer mNetServer;
        private string Name = string.Empty;
        private bool b_SOCKET_PEER_STATE_Changed = false;
        internal readonly TcpStanardRTOFunc mTcpStanardRTOFunc = new TcpStanardRTOFunc();

        public ClientPeer(UdpServer mNetServer)
        {
            this.mNetServer = mNetServer;
            mSocketMgr = new ClientPeerSocketMgr(mNetServer, this);
            mMsgReceiveMgr = new MsgReceiveMgr(mNetServer, this);
            mMsgSendMgr = new MsgSendMgr(mNetServer, this);
            mUdpCheckPool = new UdpCheckMgr(this);
            mUDPLikeTCPMgr = new UDPLikeTCPMgr(mNetServer, this);
            SetSocketState(SOCKET_PEER_STATE.NONE);
        }

        public void Update(double elapsed)
        {
            if (b_SOCKET_PEER_STATE_Changed)
            {
                this.mNetServer.OnSocketStateChanged(this);
                b_SOCKET_PEER_STATE_Changed = false;
            }

            mMsgReceiveMgr.Update(elapsed);
            mUDPLikeTCPMgr.Update(elapsed);
            mUdpCheckPool.Update(elapsed);
        }

        public void SetSocketState(SOCKET_PEER_STATE mState)
        {
            if (this.mSocketPeerState != mState)
            {
                this.mSocketPeerState = mState;
                if (MainThreadCheck.orInMainThread())
                {
                    this.mNetServer.OnSocketStateChanged(this);
                }
                else
                {
                    b_SOCKET_PEER_STATE_Changed = true;
                }
            }
        }

        public SOCKET_PEER_STATE GetSocketState()
		{
			return mSocketPeerState;
		}

        public void Reset()
        {
            mUDPLikeTCPMgr.Reset();
            mMsgReceiveMgr.Reset();
            mUdpCheckPool.Reset();
            mSocketMgr.Reset();
        }

        public void BindEndPoint(EndPoint remoteEndPoint)
        {
            this.remoteEndPoint = remoteEndPoint as IPEndPoint;
            mSocketMgr.SetRemoteEndPoint(this.remoteEndPoint);
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
            bool bCanSendPackage = UdpNetCommand.orInnerCommand(mPackage.nPackageId) ||
                GetSocketState() == SOCKET_PEER_STATE.CONNECTED;

            if (bCanSendPackage)
            {
                UdpStatistical.AddSendPackageCount();
                mUDPLikeTCPMgr.ResetSendHeartBeatCdTime();

                if (Config.bUdpCheck)
                {
                    mUdpCheckPool.SetRequestOrderId(mPackage);
                    if (UdpNetCommand.orInnerCommand(mPackage.nPackageId))
                    {
                        this.mSocketMgr.SendNetPackage(mPackage);
                    }
                    else
                    {
                        mPackage.mTcpStanardRTOTimer.BeginRtt();
                        if (Config.bUseSendStream)
                        {
                            this.mSocketMgr.SendNetPackage(mPackage);
                        }
                        else
                        {
                            var mCopyPackage = GetObjectPoolManager().NetUdpFixedSizePackage_Pop();
                            mCopyPackage.CopyFrom(mPackage);
                            this.mSocketMgr.SendNetPackage(mCopyPackage);
                        }
                    }
                }
                else
                {
                    this.mSocketMgr.SendNetPackage(mPackage);
                }
            }
        }

        public void SendInnerNetData(UInt16 id)
        {
            mMsgSendMgr.SendInnerNetData(id);
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

        public void AddLogicHandleQueue(NetPackage mPackage)
        {
            this.mMsgReceiveMgr.AddLogicHandleQueue(mPackage);
        }

        public void ResetSendHeartBeatCdTime()
        {
            this.mUDPLikeTCPMgr.ResetSendHeartBeatCdTime();
        }

        public void ReceiveHeartBeat()
        {
            this.mUDPLikeTCPMgr.ReceiveHeartBeat();
        }

        public void ReceiveConnect()
        {
            this.mUDPLikeTCPMgr.ReceiveConnect();
        }

        public void ReceiveDisConnect()
        {
            this.mUDPLikeTCPMgr.ReceiveDisConnect();
        }

        public ObjectPoolManager GetObjectPoolManager()
        {
            return mNetServer.GetObjectPoolManager();
        }

        public TcpStanardRTOFunc GetTcpStanardRTOFunc()
        {
            return mTcpStanardRTOFunc;
        }

        public Config GetConfig()
        {
            return mNetServer.GetConfig();
        }
    }
}
