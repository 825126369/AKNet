/************************************Copyright*****************************************
*        ProjectName:AKNet
*        Web:https://github.com/825126369/AKNet
*        Description:这是一个面向 .Net Standard 2.1 的游戏网络库
*        Author:阿珂
*        CreateTime:2024/11/17 12:39:35
*        Copyright:MIT软件许可证
************************************Copyright*****************************************/
using Google.Protobuf;
using System;
using AKNet.Common;
using AKNet.Udp.POINTTOPOINT.Common;
using System.Net;

namespace AKNet.Udp.POINTTOPOINT.Client
{
    internal class ClientPeer : UdpClientPeerCommonBase, UdpClientPeerBase, ClientPeerBase
    {
        internal readonly MsgSendMgr mMsgSendMgr;
        internal readonly MsgReceiveMgr mMsgReceiveMgr;
        internal readonly SocketUdp mSocketMgr;
        internal readonly UdpPackageMainThreadMgr mUdpPackageMainThreadMgr;
        internal readonly UdpCheckMgr mUdpCheckPool = null;
        internal readonly UDPLikeTCPMgr mUDPLikeTCPMgr = null;
        internal readonly TcpStanardRTOFunc mTcpStanardRTOFunc = new TcpStanardRTOFunc();
        internal readonly Config mConfig;

        private readonly ObjectPoolManager mObjectPoolManager;
        private SOCKET_PEER_STATE mSocketPeerState = SOCKET_PEER_STATE.NONE;

        private bool b_SOCKET_PEER_STATE_Changed = false;
        private event Action<ClientPeerBase> mListenSocketStateFunc = null;
        private string Name = string.Empty;

        public ClientPeer(UdpConfig mUserConfig)
        {
            NetLog.Init();
            MainThreadCheck.Check();
            if (mUserConfig == null)
            {
                mConfig = new Config();
            }
            else
            {
                mConfig = new Config(mUserConfig);
            }

            mObjectPoolManager = new ObjectPoolManager();
            mMsgSendMgr = new MsgSendMgr(this);
            mMsgReceiveMgr = new MsgReceiveMgr(this);
            mSocketMgr = new SocketUdp(this);
            mUdpCheckPool = new UdpCheckMgr(this);
            mUDPLikeTCPMgr = new UDPLikeTCPMgr(this);
            mUdpPackageMainThreadMgr = new UdpPackageMainThreadMgr(this);
        }

        public void Update(double elapsed)
        {
            if (elapsed >= 0.3)
            {
                NetLog.LogWarning("NetClient 帧 时间 太长: " + elapsed);
            }

            if (b_SOCKET_PEER_STATE_Changed)
            {
                OnSocketStateChanged();
                b_SOCKET_PEER_STATE_Changed = false;
            }

            mUdpPackageMainThreadMgr.Update(elapsed);
            mUDPLikeTCPMgr.Update(elapsed);
            mMsgReceiveMgr.Update(elapsed);
            mUdpCheckPool.Update(elapsed);
        }

        public void SetSocketState(SOCKET_PEER_STATE mState)
        {
            if (this.mSocketPeerState != mState)
            {
                this.mSocketPeerState = mState;

                if (MainThreadCheck.orInMainThread())
                {
                    OnSocketStateChanged();
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
            mSocketMgr.Reset();
            mMsgReceiveMgr.Reset();
            mUdpCheckPool.Reset();
        }

        public void Release()
        {
            mSocketMgr.Release();
            mMsgReceiveMgr.Release();
            mUdpCheckPool.Release();

            SetSocketState(SOCKET_PEER_STATE.NONE);
            mListenSocketStateFunc = null;
        }

        public void ConnectServer(string Ip, int nPort)
        {
            mSocketMgr.ConnectServer(Ip, nPort);
        }

        public bool DisConnectServer()
        {
            return mSocketMgr.DisConnectServer();
        }

        public void ReConnectServer()
        {
            mSocketMgr.ReConnectServer();
        }

        public void addNetListenFun(ushort nPackageId, Action<ClientPeerBase, NetPackage> fun)
        {
            mMsgReceiveMgr.addNetListenFun(nPackageId, fun);
        }

        public void removeNetListenFun(ushort nPackageId, Action<ClientPeerBase, NetPackage> fun)
        {
            mMsgReceiveMgr.removeNetListenFun(nPackageId, fun);
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

        public void SendNetPackage(NetUdpFixedSizePackage mPackage)
        {
            bool bCanSendPackage = UdpNetCommand.orInnerCommand(mPackage.nPackageId) ||
                GetSocketState() == SOCKET_PEER_STATE.CONNECTED;

            if (bCanSendPackage)
            {
                UdpStatistical.AddSendPackageCount();
                mUDPLikeTCPMgr.ResetSendHeartBeatCdTime();
                mUdpCheckPool.SetRequestOrderId(mPackage);
                NetPackageEncryption.Encryption(mPackage);
                mPackage.remoteEndPoint = GetIPEndPoint();
                mSocketMgr.SendNetPackage(mPackage);
            }
        }

        public IPEndPoint GetIPEndPoint()
        {
            return mSocketMgr.GetIPEndPoint();
        }

        public string GetIPAddress()
        {
           return mSocketMgr.GetIPEndPoint().Address.ToString();
        }

        public void SendNetData(NetPackage mNetPackage)
        {
            mMsgSendMgr.SendNetData(mNetPackage);
        }

        public void SendNetData(ushort nPackageId, ReadOnlySpan<byte> buffer)
        {
            mMsgSendMgr.SendNetData(nPackageId, buffer);
        }

        private void OnSocketStateChanged()
        {
            MainThreadCheck.Check();
            mListenSocketStateFunc?.Invoke(this);
        }

        public void addListenClientPeerStateFunc(Action<ClientPeerBase> mFunc)
        {
            mListenSocketStateFunc += mFunc;
        }

        public void removeListenClientPeerStateFunc(Action<ClientPeerBase> mFunc)
        {
            mListenSocketStateFunc -= mFunc;
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
            return mObjectPoolManager;
        }

        public TcpStanardRTOFunc GetTcpStanardRTOFunc()
        {
            return mTcpStanardRTOFunc;
        }

        public Config GetConfig()
        {
            return mConfig;
        }
    }
}
