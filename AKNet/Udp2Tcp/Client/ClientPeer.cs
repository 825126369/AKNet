﻿/************************************Copyright*****************************************
*        ProjectName:AKNet
*        Web:https://github.com/825126369/AKNet
*        Description:这是一个面向 .Net Standard 2.1 的游戏网络库
*        Author:阿珂
*        ModifyTime:2025/2/27 22:28:11
*        Copyright:MIT软件许可证
************************************Copyright*****************************************/
using AKNet.Common;
using AKNet.Udp2Tcp.Common;
using System;
using System.Net;
using System.Xml.Linq;

namespace AKNet.Udp2Tcp.Client
{
    internal class ClientPeer : UdpClientPeerCommonBase, NetClientInterface, ClientPeerBase
    {
        internal readonly ListenNetPackageMgr mPackageManager = new ListenNetPackageMgr();
        internal readonly ListenClientPeerStateMgr mListenClientPeerStateMgr = new ListenClientPeerStateMgr();

        internal readonly MsgSendMgr mMsgSendMgr;
        internal readonly MsgReceiveMgr mMsgReceiveMgr;
        internal readonly SocketUdp mSocketMgr;
        internal readonly UdpCheckMgr mUdpCheckPool = null;
        internal readonly UDPLikeTCPMgr mUDPLikeTCPMgr = null;
        internal readonly TcpStanardRTOFunc mTcpStanardRTOFunc = new TcpStanardRTOFunc();
        internal readonly Config mConfig;
        internal readonly CryptoMgr mCryptoMgr;

        private readonly ObjectPoolManager mObjectPoolManager;
        private SOCKET_PEER_STATE mSocketPeerState = SOCKET_PEER_STATE.NONE;
        private bool b_SOCKET_PEER_STATE_Changed = false;
        private string Name = string.Empty;
        private uint ID = 0;

        public ClientPeer()
        {
            NetLog.Init();
            MainThreadCheck.Check();
            mConfig = new Config();
            mCryptoMgr = new CryptoMgr();
            mObjectPoolManager = new ObjectPoolManager();
            mMsgSendMgr = new MsgSendMgr(this);
            mMsgReceiveMgr = new MsgReceiveMgr(this);
            mSocketMgr = new SocketUdp(this);
            mUdpCheckPool = new UdpCheckMgr(this);
            mUDPLikeTCPMgr = new UDPLikeTCPMgr(this);
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
            this.Name = string.Empty;
        }

        public void Release()
        {
            mSocketMgr.Release();
            mMsgReceiveMgr.Release();
            mUdpCheckPool.Release();

            SetSocketState(SOCKET_PEER_STATE.NONE);
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

        public void SendNetPackage(NetUdpFixedSizePackage mPackage)
        {
            bool bCanSendPackage = UdpNetCommand.orInnerCommand(mPackage.GetPackageId()) ||
                GetSocketState() == SOCKET_PEER_STATE.CONNECTED;

            if (bCanSendPackage)
            {
                UdpStatistical.AddSendPackageCount();
                mUDPLikeTCPMgr.ResetSendHeartBeatCdTime();

                if (Config.bUdpCheck)
                {
                    mUdpCheckPool.SetRequestOrderId(mPackage);
                    if (UdpNetCommand.orInnerCommand(mPackage.GetPackageId()))
                    {
                        this.mSocketMgr.SendNetPackage(mPackage);
                    }
                    else
                    {
                        UdpStatistical.AddSendCheckPackageCount();
                        mPackage.mTcpStanardRTOTimer.BeginRtt();
                        this.mSocketMgr.SendNetPackage(mPackage);
                    }
                }
                else
                {
                    this.mSocketMgr.SendNetPackage(mPackage);
                }
            }
        }

        public IPEndPoint GetIPEndPoint()
        {
            return mSocketMgr.GetIPEndPoint();
        }

        public void SendNetData(NetPackage mNetPackage)
        {
            mMsgSendMgr.SendNetData(mNetPackage);
        }

        public void SendNetData(ushort nPackageId, ReadOnlySpan<byte> buffer)
        {
            mMsgSendMgr.SendNetData(nPackageId, buffer);
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

        public CryptoMgr GetCryptoMgr()
        {
            return mCryptoMgr;
        }

        public int GetCurrentFrameRemainPackageCount()
        {
            return mMsgReceiveMgr.GetCurrentFrameRemainPackageCount();
        }

        public void NetPackageExecute(NetPackage mPackage)
        {
            mPackageManager.NetPackageExecute(this, mPackage);
        }

        public void addNetListenFunc(ushort nPackageId, Action<ClientPeerBase, NetPackage> fun)
        {
            mPackageManager.addNetListenFunc(nPackageId, fun);
        }

        public void removeNetListenFunc(ushort nPackageId, Action<ClientPeerBase, NetPackage> fun)
        {
            mPackageManager.removeNetListenFunc(nPackageId, fun);
        }

        public void addNetListenFunc(Action<ClientPeerBase, NetPackage> mFunc)
        {
            mPackageManager.addNetListenFunc(mFunc);
        }

        public void removeNetListenFunc(Action<ClientPeerBase, NetPackage> mFunc)
        {
            mPackageManager.removeNetListenFunc(mFunc);
        }

        private void OnSocketStateChanged()
        {
            mListenClientPeerStateMgr.OnSocketStateChanged(this);
        }

        public void addListenClientPeerStateFunc(Action<ClientPeerBase, SOCKET_PEER_STATE> mFunc)
        {
            mListenClientPeerStateMgr.addListenClientPeerStateFunc(mFunc);
        }

        public void removeListenClientPeerStateFunc(Action<ClientPeerBase, SOCKET_PEER_STATE> mFunc)
        {
            mListenClientPeerStateMgr.removeListenClientPeerStateFunc(mFunc);
        }

        public void addListenClientPeerStateFunc(Action<ClientPeerBase> mFunc)
        {
            mListenClientPeerStateMgr.addListenClientPeerStateFunc(mFunc);
        }

        public void removeListenClientPeerStateFunc(Action<ClientPeerBase> mFunc)
        {
            mListenClientPeerStateMgr.removeListenClientPeerStateFunc(mFunc);
        }

        public void ReceiveTcpStream(NetUdpFixedSizePackage mPackage)
        {
            mMsgReceiveMgr.ReceiveTcpStream(mPackage);
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
    }
}
