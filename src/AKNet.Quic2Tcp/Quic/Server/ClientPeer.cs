/************************************Copyright*****************************************
*        ProjectName:AKNet
*        Web:https://github.com/825126369/AKNet
*        Description:C#游戏网络库
*        Author:许珂
*        StartTime:2024/11/01 00:00:00
*        ModifyTime:2025/11/30 19:43:15
*        Copyright:MIT软件许可证
************************************Copyright*****************************************/

using AKNet.Common;
using AKNet.Quic.Common;
using System.Net.Quic;
using System.Runtime.CompilerServices;

namespace AKNet.Quic.Server
{
    internal partial class ClientPeer : ClientPeerBase, IPoolItemInterface
	{
		private SOCKET_PEER_STATE mSocketPeerState = SOCKET_PEER_STATE.NONE;
        private SOCKET_PEER_STATE mLastSocketPeerState = SOCKET_PEER_STATE.NONE;

        private double fSendHeartBeatTime = 0.0;
		private double fReceiveHeartBeatTime = 0.0;
		
		private readonly ServerMgr mServerMgr;
		private string Name = string.Empty;
        private uint ID = 0;

        private readonly NetStreamCircularBuffer mReceiveStreamList = new NetStreamCircularBuffer();
        private readonly object lock_mReceiveStreamList_object = new object();

        private readonly Memory<byte> mReceiveBuffer = new byte[1024];
        private readonly byte[] mSendBuffer = new byte[1024];
        CancellationTokenSource mCancellationTokenSource = new CancellationTokenSource();
        private readonly AkCircularBuffer mSendStreamList = new AkCircularBuffer();
        private bool bSendIOContextUsed = false;
        private QuicStream mSendQuicStream;
        private QuicConnection mQuicConnection;

        public ClientPeer(ServerMgr mNetServer)
		{
			this.mServerMgr = mNetServer;
            SetSocketState(SOCKET_PEER_STATE.DISCONNECTED);
        }

		public void Update(double elapsed)
		{
			switch (mSocketPeerState)
			{
				case SOCKET_PEER_STATE.CONNECTED:
                    int nPackageCount = 0;
                    while (NetPackageExecute())
                    {
                        nPackageCount++;
                    }
                    if (nPackageCount > 0)
                    {
                        ReceiveHeartBeat();
                    }

                    //if (nPackageCount > 100)
                    //{
                    //	NetLog.LogWarning("Server ClientPeer 处理逻辑包的数量： " + nPackageCount);
                    //}

                    fSendHeartBeatTime += elapsed;
					if (fSendHeartBeatTime >= Config.fMySendHeartBeatMaxTime)
					{
						SendHeartBeat();
						fSendHeartBeatTime = 0.0;
					}

                    double fHeatTime = Math.Min(0.3, elapsed);
                    fReceiveHeartBeatTime += fHeatTime;
					if (fReceiveHeartBeatTime >= Config.fReceiveHeartBeatTimeOut)
					{
						mSocketPeerState = SOCKET_PEER_STATE.DISCONNECTED;
						fReceiveHeartBeatTime = 0.0;
#if DEBUG
						NetLog.Log("心跳超时");
#endif
					}

					break;
				default:
					break;
			}

            if (this.mSocketPeerState != this.mLastSocketPeerState)
            {
                this.mLastSocketPeerState = mSocketPeerState;
                mServerMgr.mListenClientPeerStateMgr.OnSocketStateChanged(this);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void SendHeartBeat()
		{
			SendNetData(TcpNetCommand.COMMAND_HEARTBEAT);
		}

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void ResetSendHeartBeatTime()
        {
            fSendHeartBeatTime = 0f;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void ReceiveHeartBeat()
		{
			fReceiveHeartBeatTime = 0.0;
		}

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void SetSocketState(SOCKET_PEER_STATE mSocketPeerState)
        {
            this.mSocketPeerState = mSocketPeerState;
        }

        public SOCKET_PEER_STATE GetSocketState()
        {
            return mSocketPeerState;
        }

		public void Reset()
		{
            SetSocketState(SOCKET_PEER_STATE.DISCONNECTED);
            CloseSocket();
            lock (mSendStreamList)
            {
                mSendStreamList.Reset();
            }
            lock (mReceiveStreamList)
            {
                mReceiveStreamList.Reset();
            }
            
			this.Name = string.Empty;
			this.ID = 0;
            fSendHeartBeatTime = 0.0;
            fReceiveHeartBeatTime = 0.0;
        }

        public void Release()
        {
            SetSocketState(SOCKET_PEER_STATE.DISCONNECTED);
            CloseSocket();
            lock (mReceiveStreamList)
            {
                mReceiveStreamList.Dispose();
            }
            lock (mSendStreamList)
            {
                mSendStreamList.Dispose();
            }
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
