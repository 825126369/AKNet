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
using System.Collections;
using System.Net;
using System.Net.Quic;
using System.Runtime.CompilerServices;

namespace AKNet.Quic.Client
{
    internal partial class ClientPeer : QuicClientInterface, QuicClientPeerBase
    {
        internal readonly QuicStreamReceivePackage mNetPackage = new QuicStreamReceivePackage();
        internal readonly QuicListenNetPackageMgr mPackageManager = new QuicListenNetPackageMgr();
        internal QuicStreamEncryption mCryptoMgr = new QuicStreamEncryption();
        internal QuicConnection mQuicConnection = null;

        private readonly QuicListenClientPeerStateMgr mListenClientPeerStateMgr = new QuicListenClientPeerStateMgr();
        private readonly Dictionary<byte, ClientPeerQuicStream> mSendStreamEnumDic = new Dictionary<byte, ClientPeerQuicStream>();
        private readonly Dictionary<long, ClientPeerQuicStream> mAcceptStreamDic = new Dictionary<long, ClientPeerQuicStream>();
        private readonly Queue<ClientPeerQuicStream> mPendingAcceptStreamQueue = new Queue<ClientPeerQuicStream>();

        private double fReConnectServerCdTime = 0.0;
        private double fSendHeartBeatTime = 0.0;
        private double fReceiveHeartBeatTime = 0.0;

        private SOCKET_PEER_STATE mSocketPeerState = SOCKET_PEER_STATE.NONE;
        private SOCKET_PEER_STATE mLastSocketPeerState = SOCKET_PEER_STATE.NONE;
        private string Name = string.Empty;
        private uint ID = 0;

        private string ServerIp = "";
        private int nServerPort = 0;
        private IPEndPoint mIPEndPoint = null;

        public ClientPeer()
        {
            SetSocketState(SOCKET_PEER_STATE.NONE);
        }

		public void Update(double elapsed)
		{
            if (elapsed >= 0.3)
            {
                NetLog.LogWarning("帧 时间 太长: " + elapsed);
            }
            
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

                    //if (nPackageCount > 100)
                    //{
                    //	NetLog.LogWarning("Client 处理逻辑包的数量： " + nPackageCount);
                    //}

                    fSendHeartBeatTime += elapsed;
					if (fSendHeartBeatTime >= Config.fMySendHeartBeatMaxTime)
					{
                        fSendHeartBeatTime = 0.0;
                        SendHeartBeat();
					}

                    double fHeatTime = Math.Min(0.3, elapsed);
                    fReceiveHeartBeatTime += fHeatTime;
                    if (fReceiveHeartBeatTime >= Config.fReceiveHeartBeatTimeOut)
                    {
                        fReceiveHeartBeatTime = 0.0;
                        fReConnectServerCdTime = 0.0;
                        mSocketPeerState = SOCKET_PEER_STATE.RECONNECTING;
#if DEBUG
                        NetLog.Log($"{GetName()} 心跳超时");
#endif
                    }
                    
					break;
				case SOCKET_PEER_STATE.RECONNECTING:
					fReConnectServerCdTime += elapsed;
					if (fReConnectServerCdTime >= Config.fReConnectMaxCdTime)
					{
                        fReConnectServerCdTime = 0.0;
                        mSocketPeerState = SOCKET_PEER_STATE.CONNECTING;
						ReConnectServer();
					}
					break;
				default:
					break;
			}

            if (this.mSocketPeerState != this.mLastSocketPeerState)
            {
                this.mLastSocketPeerState = mSocketPeerState;
                mListenClientPeerStateMgr.OnSocketStateChanged(this);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void SendHeartBeat()
        {
            SendNetData(0, TcpNetCommand.COMMAND_HEARTBEAT);
            //NetLog.Log("发送心跳");
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void ResetSendHeartBeatTime()
        {
            fSendHeartBeatTime = 0f;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void ReceiveHeartBeat()
		{
			fReceiveHeartBeatTime = 0f;
            //NetLog.Log("接收心跳");
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void SetSocketState(SOCKET_PEER_STATE mSocketPeerState)
        {
            this.mSocketPeerState = mSocketPeerState;
        }

        public SOCKET_PEER_STATE GetSocketState()
        {
			return this.mSocketPeerState;
        }

		public void Release()
		{
            SetSocketState(SOCKET_PEER_STATE.DISCONNECTED);
            CloseSocket();
        }

        public void addNetListenFunc(ushort nPackageId, Action<QuicClientPeerBase, QuicNetPackage> fun)
        {
            mPackageManager.addNetListenFunc(nPackageId, fun);
        }

        public void removeNetListenFunc(ushort nPackageId, Action<QuicClientPeerBase, QuicNetPackage> fun)
        {
            mPackageManager.removeNetListenFunc(nPackageId, fun);
        }

        public void addNetListenFunc(Action<QuicClientPeerBase, QuicNetPackage> func)
        {
            mPackageManager.addNetListenFunc(func);
        }

        public void removeNetListenFunc(Action<QuicClientPeerBase, QuicNetPackage> func)
        {
            mPackageManager.removeNetListenFunc(func);
        }

        public void addListenClientPeerStateFunc(Action<QuicClientPeerBase, SOCKET_PEER_STATE> mFunc)
        {
            mListenClientPeerStateMgr.addListenClientPeerStateFunc(mFunc);
        }

        public void removeListenClientPeerStateFunc(Action<QuicClientPeerBase, SOCKET_PEER_STATE> mFunc)
        {
            mListenClientPeerStateMgr.removeListenClientPeerStateFunc(mFunc);
        }

        public void addListenClientPeerStateFunc(Action<QuicClientPeerBase> mFunc)
        {
            mListenClientPeerStateMgr.addListenClientPeerStateFunc(mFunc);
        }

        public void removeListenClientPeerStateFunc(Action<QuicClientPeerBase> mFunc)
        {
            mListenClientPeerStateMgr.removeListenClientPeerStateFunc(mFunc);
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


