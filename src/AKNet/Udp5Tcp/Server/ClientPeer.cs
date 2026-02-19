/************************************Copyright*****************************************
*        ProjectName:AKNet
*        Web:https://github.com/825126369/AKNet
*        Description:C#游戏网络库
*        Author:许珂
*        StartTime:2024/11/01 00:00:00
*        ModifyTime:2026/2/1 20:26:51
*        Copyright:MIT软件许可证
************************************Copyright*****************************************/
using AKNet.Common;
using AKNet.Udp5Tcp.Common;
using System;
using System.Runtime.CompilerServices;

namespace AKNet.Udp5Tcp.Server
{
    internal partial class ClientPeer : ClientPeerBase
	{
        private SOCKET_PEER_STATE mSocketPeerState;
        private SOCKET_PEER_STATE mLastSocketPeerState;
        private ServerMgr mServerMgr;

        private string Name = string.Empty;
        private uint ID = 0;
        private double fReceiveHeartBeatTime = 0.0;
        private double fSendHeartBeatTime = 0.0;

        private readonly AkCircularManyBuffer mSendStreamList = new AkCircularManyBuffer();
        private readonly NetStreamCircularBuffer mReceiveStreamList = new NetStreamCircularBuffer();
        private Connection mConnection = null;

        private readonly Memory<byte> ReceiveArgs = new byte[Config.nUdpPackageFixedSize];
        private readonly Memory<byte> SendArgs = new byte[Config.nUdpPackageFixedSize];
        private bool bSendIOContexUsed = false;

        public ClientPeer(ServerMgr mNetServer)
        {
            this.mServerMgr = mNetServer;
            bSendIOContexUsed = false;
            ResetSocketState();
        }

        public void Update(double elapsed)
        {
            switch (mSocketPeerState)
            {
                case SOCKET_PEER_STATE.CONNECTED:
                    {
                        int nPackageCount = 0;
                        while (NetPackageExecute())
                        {
                            nPackageCount++;
                        }
                        if (nPackageCount > 0)
                        {
                            ReceiveHeartBeat();
                        }

                        fSendHeartBeatTime += elapsed;
                        if (fSendHeartBeatTime >= Config.fSendHeartBeatMaxTime)
                        {
                            fSendHeartBeatTime = 0.0;
                            SendHeartBeat();
                        }

                        // 有可能网络流量大的时候，会while循环卡住
                        double fHeatTime = Math.Min(0.3, elapsed);
                        fReceiveHeartBeatTime += fHeatTime;
                        if (fReceiveHeartBeatTime >= Config.fReceiveHeartBeatTimeOut)
                        {
                            fReceiveHeartBeatTime = 0.0;
#if DEBUG
                            NetLog.Log("Server 接收服务器心跳 超时 ");
#endif
                            SetSocketState(SOCKET_PEER_STATE.DISCONNECTED);
                        }
                        break;
                    }
                default:
                    break;
            }

            OnSocketStateChanged();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void SetSocketState(SOCKET_PEER_STATE mState)
        {
            NetLog.Assert(mState == SOCKET_PEER_STATE.CONNECTED || mState == SOCKET_PEER_STATE.DISCONNECTED);
            this.mSocketPeerState = mState;
        }

        public SOCKET_PEER_STATE GetSocketState()
		{
			return mSocketPeerState;
		}

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void OnSocketStateChanged()
        {
            if (this.mSocketPeerState != this.mLastSocketPeerState)
            {
                this.mLastSocketPeerState = mSocketPeerState;
                mServerMgr.OnSocketStateChanged(this);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void ResetSocketState()
        {
            this.mSocketPeerState = this.mLastSocketPeerState = SOCKET_PEER_STATE.DISCONNECTED;
        }


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void SendHeartBeat()
        {
            SendNetData(TcpNetCommand.COMMAND_HEARTBEAT);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void ResetSendHeartBeatCdTime()
        {
            fSendHeartBeatTime = 0.0;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void ReceiveHeartBeat()
        {
            fReceiveHeartBeatTime = 0.0;
        }

        public void Reset()
        {
            OnSocketStateChanged();
            ResetSocketState();

            CloseSocket();
            lock (mReceiveStreamList)
            {
                mReceiveStreamList.Reset();
            }

            lock (mSendStreamList)
            {
                mSendStreamList.Reset();
            }

            bSendIOContexUsed = false;
            fSendHeartBeatTime = 0.0;
            fReceiveHeartBeatTime = 0.0;
            this.Name = string.Empty;
            this.ID = 0;
        }

        public void Release()
        {
            Reset();

            lock (mReceiveStreamList)
            {
                mReceiveStreamList.Dispose();
            }

            lock (mSendStreamList)
            {
                mSendStreamList.Dispose();
            }
        }

        public void NetPackageExecute(NetPackage mPackage)
        {
            mServerMgr.GetPackageManager().NetPackageExecute(this, mPackage);
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
