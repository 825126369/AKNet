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
using System.Net.Quic;

namespace AKNet.Quic.Server
{
    internal class ClientPeerQuicStream:IDisposable
    {
        private readonly Memory<byte> mReceiveBuffer = new byte[1024];
        private readonly Memory<byte> mSendBuffer = new byte[1024];
        private readonly AkCircularBuffer mSendStreamList = new AkCircularBuffer();
        private readonly NetStreamCircularBuffer mReceiveStreamList = new NetStreamCircularBuffer();
        private bool bSendIOContextUsed = false;

        private ServerMgr mServerMgr;
        private QuicStream mQuicStream;
        private ClientPeer mClientPeer;

        public ClientPeerQuicStream(ServerMgr mServerMgr, ClientPeer mClientPeer, QuicStream mStream)
        {
            this.mServerMgr = mServerMgr;
            this.mClientPeer = mClientPeer;
            this.mQuicStream = mStream;
        }

        public void Init()
        {
            StartProcessStreamReceive();
        }

        public long GetStreamId()
        {
            return this.mQuicStream.Id;
        }

        private void MultiThreadingReceiveSocketStream(ReadOnlySpan<byte> e)
        {
            lock (mReceiveStreamList)
            {
                mReceiveStreamList.WriteFrom(e);
            }
        }

        public bool NetPackageExecute()
        {
            bool bSuccess = false;
            lock (mReceiveStreamList)
            {
                bSuccess = mServerMgr.mCryptoMgr.Decode(mReceiveStreamList, mServerMgr.mNetPackage);
            }

            if (bSuccess)
            {
                mServerMgr.mPackageManager.NetPackageExecute(this.mClientPeer, mServerMgr.mNetPackage);
            }

            return bSuccess;
        }

        public async Task StartProcessStreamReceive()
        {
            try
            {
                if (mQuicStream != null)
                {
                    while (true)
                    {
                        int nLength = await mQuicStream.ReadAsync(mReceiveBuffer);
                        if (nLength > 0)
                        {
                            MultiThreadingReceiveSocketStream(mReceiveBuffer.Span.Slice(0, nLength));
                        }
                        else
                        {
                            NetLog.Log($"mQuicStream.ReadAsync Length: {nLength}");
                            break;
                        }
                    }
                }
            }
            catch (Exception e)
            {
                NetLog.LogError(e.ToString());
                DisConnectedWithError();
            }
        }

        public void SendNetStream(ReadOnlySpan<byte> mBufferSegment)
        {
            lock (mSendStreamList)
            {
                mSendStreamList.WriteFrom(mBufferSegment);
            }

            if (!Volatile.Read(ref bSendIOContextUsed))
            {
                bSendIOContextUsed = true;
                SendNetStream2();
            }
        }

        private async void SendNetStream2()
        {
            try
            {
                while(mSendStreamList.Length > 0)
                {
                    int nLength = 0;
                    lock (mSendStreamList)
                    {
                        nLength = mSendStreamList.WriteToMax(0, mSendBuffer.Span);
                    }
                    await mQuicStream.WriteAsync(mSendBuffer.Slice(0, nLength));
                }
                bSendIOContextUsed = false;
            }
            catch (QuicException e)
            {
                NetLog.LogError(e.ToString());
                DisConnectedWithError();
            }
        }

        private void DisConnectedWithError()
        {
            var mSocketPeerState = mClientPeer.GetSocketState();
            if (mSocketPeerState == SOCKET_PEER_STATE.DISCONNECTING)
            {
                mClientPeer.SetSocketState(SOCKET_PEER_STATE.DISCONNECTED);
            }
            else if (mSocketPeerState == SOCKET_PEER_STATE.CONNECTED)
            {
                mClientPeer.SetSocketState(SOCKET_PEER_STATE.RECONNECTING);
            }
        }
        
        public void Dispose()
        {
            mQuicStream.Dispose();
            lock (mReceiveStreamList)
            {
                mReceiveStreamList.Dispose();
            }

            lock (mSendStreamList)
            {
                mSendStreamList.Dispose();
            }
        }
    }
}
