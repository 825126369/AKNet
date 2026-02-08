/************************************Copyright*****************************************
*        ProjectName:AKNet
*        Web:https://github.com/825126369/AKNet
*        Description:C#游戏网络库
*        Author:许珂
*        StartTime:2024/11/01 00:00:00
*        ModifyTime:2026/2/1 20:27:03
*        Copyright:MIT软件许可证
************************************Copyright*****************************************/

using AKNet.Common;
using AKNet.Quic.Common;
using System.Net.Quic;
using System.Runtime.CompilerServices;

namespace AKNet.Quic.Server
{
    internal partial class ClientPeer : QuicClientPeerBase, IPoolItemInterface
	{
		private SOCKET_PEER_STATE mSocketPeerState;
        private SOCKET_PEER_STATE mLastSocketPeerState;

        private double fSendHeartBeatTime = 0.0;
		private double fReceiveHeartBeatTime = 0.0;
		
		private readonly ServerMgr mServerMgr;
		private string Name = string.Empty;
        private uint ID = 0;
        
        internal QuicConnection mQuicConnection;
        private readonly Dictionary<byte, ClientPeerQuicStream> mSendStreamEnumDic = new Dictionary<byte, ClientPeerQuicStream>();
        private readonly Dictionary<long, ClientPeerQuicStream> mAcceptStreamDic = new Dictionary<long, ClientPeerQuicStream>();
        private readonly Queue<ClientPeerQuicStream> mPendingAcceptStreamQueue = new Queue<ClientPeerQuicStream>();

        public ClientPeer(ServerMgr mNetServer)
		{
			this.mServerMgr = mNetServer;
            ResetSocketState();
        }

		public void Update(double elapsed)
		{
			switch (mSocketPeerState)
			{
				case SOCKET_PEER_STATE.CONNECTED:

                    if (mPendingAcceptStreamQueue.Count > 0)
                    {
                        lock (mPendingAcceptStreamQueue)
                        {
                            while (mPendingAcceptStreamQueue.TryDequeue(out var v))
                            {
                                mAcceptStreamDic.Add(v.GetStreamId(), v);
                            }
                        }
                    }

                    int nPackageCount = 0;
                    foreach (var mStream in mAcceptStreamDic)
                    {
                        while (mStream.Value.NetPackageExecute())
                        {
                            nPackageCount++;
                        }
                    }
                    if (nPackageCount > 0)
                    {
                        ReceiveHeartBeat();
                    }

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
						SetSocketState(SOCKET_PEER_STATE.DISCONNECTED);
						fReceiveHeartBeatTime = 0.0;
#if DEBUG
                        NetLog.Log($"{GetName()} 心跳超时");
#endif
                    }

					break;
				default:
					break;
			}

            OnSocketStateChanged();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void SendHeartBeat()
		{
			SendNetData(0, TcpNetCommand.COMMAND_HEARTBEAT);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void ResetSendHeartBeatTime()
        {
            fSendHeartBeatTime = 0f;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void ReceiveHeartBeat()
		{
			fReceiveHeartBeatTime = 0.0;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void SetSocketState(SOCKET_PEER_STATE mState)
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

        public void Reset()
		{
            OnSocketStateChanged();
            ResetSocketState();

            CloseSocket();
			this.Name = string.Empty;
			this.ID = 0;
            fSendHeartBeatTime = 0.0;
            fReceiveHeartBeatTime = 0.0;
        }

        public void Release()
        {
            Reset();
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
