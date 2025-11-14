/************************************Copyright*****************************************
*        ProjectName:AKNet
*        Web:https://github.com/825126369/AKNet
*        Description:C#游戏网络库
*        Author:许珂
*        StartTime:2024/11/01 00:00:00
*        ModifyTime:2025/11/14 8:56:46
*        Copyright:MIT软件许可证
************************************Copyright*****************************************/
using AKNet.Common;
using AKNet.Udp.POINTTOPOINT.Common;
using System;
using System.Net;

namespace AKNet.Udp.POINTTOPOINT.Server
{
    internal class ClientPeerPrivate : UdpClientPeerCommonBase, UdpClientPeerBase, ClientPeerBase
	{
        internal MsgSendMgr mMsgSendMgr;
        internal MsgReceiveMgr mMsgReceiveMgr;
        internal ClientPeerSocketMgr mSocketMgr;

        internal UdpCheckMgr mUdpCheckPool = null;
		internal UDPLikeTCPMgr mUDPLikeTCPMgr = null;
        private SOCKET_PEER_STATE mSocketPeerState = SOCKET_PEER_STATE.NONE;
        private UdpServer mNetServer;
        private string Name = string.Empty;
        private uint ID = 0;
        private bool b_SOCKET_PEER_STATE_Changed = false;
        internal readonly TcpStanardRTOFunc mTcpStanardRTOFunc = new TcpStanardRTOFunc();

        public ClientPeerPrivate(UdpServer mNetServer)
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
            this.Name = string.Empty;
            this.ID = 0;
        }

        public void Release()
        {
            mSocketMgr.Release();
        }

        public void HandleConnectedSocket(FakeSocket mSocket)
        {
            SetSocketState(SOCKET_PEER_STATE.CONNECTED);
            mSocketMgr.HandleConnectedSocket(mSocket);
        }

        public IPEndPoint GetIPEndPoint()
        {
            return mSocketMgr.GetIPEndPoint();
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
                        UdpStatistical.AddSendCheckPackageCount();
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

        public void SetID(uint id)
        {
            this.ID = id;
        }

        public uint GetID()
        {
            return this.ID;
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

        public int GetCurrentFrameRemainPackageCount()
        {
            return mMsgReceiveMgr.GetCurrentFrameRemainPackageCount();
        }

        public void NetPackageExecute(NetPackage mPackage)
        {
            mNetServer.GetPackageManager().NetPackageExecute(this, mPackage);
        }
    }
}
