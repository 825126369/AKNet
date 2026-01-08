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

namespace AKNet.Quic.Client
{
    internal class ClientPeerQuicStream :IDisposable
    {
        private bool _disposed;
        private readonly Memory<byte> mReceiveBuffer = new byte[1024];
        private readonly Memory<byte> mSendBuffer = new byte[1024];
        private readonly AkCircularBuffer mSendStreamList = new AkCircularBuffer();
        private readonly NetStreamCircularBuffer mReceiveStreamList = new NetStreamCircularBuffer();
        private bool bSendIOContextUsed = false;

        private QuicStream mQuicStream;
        private ClientPeer mClientPeer;
        private readonly byte nStreamEnumIndex;

        public ClientPeerQuicStream(ClientPeer mClientPeer, QuicStream mStream) //接收流
        {
            this.mClientPeer = mClientPeer;
            this.mQuicStream = mStream;
            this.nStreamEnumIndex = 0;
        }

        public ClientPeerQuicStream(ClientPeer mClientPeer, byte nStreamEnumIndex) //发送流
        {
            this.mClientPeer = mClientPeer;
            this.nStreamEnumIndex = nStreamEnumIndex;
            this.mQuicStream = null;
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
                bSuccess = mClientPeer.mCryptoMgr.Decode(mReceiveStreamList, mClientPeer.mNetPackage);
            }

            if (bSuccess)
            {
                if (TcpNetCommand.orInnerCommand(mClientPeer.mNetPackage.nPackageId))
                {

                }
                else
                {
                    mClientPeer.mPackageManager.NetPackageExecute(this.mClientPeer, mClientPeer.mNetPackage);
                }
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
                while (!_disposed && mSendStreamList.Length > 0)
                {
                    int nLength = 0;
                    lock (mSendStreamList)
                    {
                        nLength = mSendStreamList.WriteToMax(0, mSendBuffer.Span);
                    }

                    if (mQuicStream == null)
                    {
                        this.mQuicStream = await mClientPeer.mQuicConnection.OpenOutboundStreamAsync(QuicStreamType.Unidirectional);
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
            if (_disposed) return;
            _disposed = true;

            mClientPeer = null;

            if (mQuicStream != null)
            {
                mQuicStream.Dispose();
            }
            lock (mReceiveStreamList)
            {
                mReceiveStreamList.Dispose();
            }

            lock (mSendStreamList)
            {
                mSendStreamList.Dispose();
            }
        }

        //发送----------------------------------------------------------------------------------------
        public void SendNetData(ushort nPackageId)
        {
            if (mClientPeer.GetSocketState() == SOCKET_PEER_STATE.CONNECTED)
            {
                mClientPeer.ResetSendHeartBeatTime();
                var mBufferSegment = mClientPeer.mCryptoMgr.Encode(nStreamEnumIndex, nPackageId, ReadOnlySpan<byte>.Empty);
                SendNetStream(mBufferSegment);
            }
            else
            {
                NetLog.LogError("SendNetData Failed");
            }
        }

        public void SendNetData(ushort nPackageId, byte[] data)
        {
            if (mClientPeer.GetSocketState() == SOCKET_PEER_STATE.CONNECTED)
            {
                mClientPeer.ResetSendHeartBeatTime();
                var mBufferSegment = mClientPeer.mCryptoMgr.Encode(nStreamEnumIndex, nPackageId, data);
                SendNetStream(mBufferSegment);
            }
            else
            {
                NetLog.LogError("SendNetData Failed");
            }
        }

        public void SendNetData(NetPackage mNetPackage)
        {
            if (mClientPeer.GetSocketState() == SOCKET_PEER_STATE.CONNECTED)
            {
                mClientPeer.ResetSendHeartBeatTime();
                var mBufferSegment = mClientPeer.mCryptoMgr.Encode(nStreamEnumIndex, mNetPackage.GetPackageId(), mNetPackage.GetData());
                SendNetStream(mBufferSegment);
            }
            else
            {
                NetLog.LogError("SendNetData Failed");
            }
        }

        public void SendNetData(ushort nPackageId, ReadOnlySpan<byte> buffer)
        {
            if (mClientPeer.GetSocketState() == SOCKET_PEER_STATE.CONNECTED)
            {
                mClientPeer.ResetSendHeartBeatTime();
                var mBufferSegment = mClientPeer.mCryptoMgr.Encode(nStreamEnumIndex, nPackageId, buffer);
                SendNetStream(mBufferSegment);
            }
            else
            {
                NetLog.LogError("SendNetData Failed");
            }
        }
    }
}
