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

namespace AKNet.Quic.Server
{
    internal class ClientPeerQuicStream
    {
        private bool _disposed;
        private readonly Memory<byte> mReceiveBuffer = new byte[Config.nIOContexBufferLength];
        private readonly Memory<byte> mSendBuffer = new byte[Config.nIOContexBufferLength];
        private readonly AkCircularBuffer mSendStreamList = new AkCircularBuffer();
        private readonly NetStreamCircularBuffer mReceiveStreamList = new NetStreamCircularBuffer();
        private bool bSendIOContextUsed = false;

        private ServerMgr mServerMgr;
        private QuicStream mQuicStream;
        private ClientPeer mClientPeer;
        private readonly byte nStreamEnumIndex;

        public ClientPeerQuicStream(ServerMgr mServerMgr, ClientPeer mClientPeer, QuicStream mStream)
        {
            this.mServerMgr = mServerMgr;
            this.mClientPeer = mClientPeer;
            this.mQuicStream = mStream;
            this.nStreamEnumIndex = 0;

            NetLog.Log($"New Accetp Stream: {mStream.Id}");
        }

        public ClientPeerQuicStream(ServerMgr mServerMgr, ClientPeer mClientPeer, byte nStreamEnumIndex)
        {
            this.mServerMgr = mServerMgr;
            this.mClientPeer = mClientPeer;
            this.mQuicStream = null;
            this.nStreamEnumIndex = nStreamEnumIndex;

            NetLog.Log($"New Send Stream: {nStreamEnumIndex}");
        }

        public long GetStreamId()
        {
            return this.mQuicStream.Id;
        }

        public byte GetStreamEnumIndex()
        {
            return nStreamEnumIndex;
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
                if (TcpNetCommand.orInnerCommand(mServerMgr.mNetPackage.nPackageId))
                {

                }
                else
                {
                    mServerMgr.mPackageManager.NetPackageExecute(this.mClientPeer, mServerMgr.mNetPackage);
                }
            }

            return bSuccess;
        }

        public async void StartProcessStreamReceive()
        {
            try
            {
                while (mQuicStream != null)
                {
                    int nLength = await mQuicStream.ReadAsync(mReceiveBuffer).ConfigureAwait(false);
                    if (nLength > 0)
                    {
                        MultiThreadingReceiveSocketStream(mReceiveBuffer.Span.Slice(0, nLength));
                    }
                    else
                    {
                        NetLog.Log($"mQuicStream.ReadAsync Length: {nLength}");
                        DisConnectedWithError();
                        break;
                    }
                }
            }
            catch (Exception e)
            {
                //NetLog.LogError(e.ToString());
                DisConnectedWithError();
            }
        }

        public void SendNetStream(ReadOnlySpan<byte> mBufferSegment)
        {
            if(mBufferSegment.Length > Config.nDataMaxLength)
            {
                throw new Exception($"mBufferSegment.Length: {mBufferSegment.Length}");
            }

            bool bSend = false;
            lock (mSendStreamList)
            {
                mSendStreamList.WriteFrom(mBufferSegment);
                if (!bSendIOContextUsed)
                {
                    bSendIOContextUsed = true;
                    bSend = true;
                }
            }
            
            if (bSend)
            {
                SendNetStream2();
            }
            else
            {
                if (!bSendIOContextUsed && mSendStreamList.Length > 0)
                {
                    throw new Exception("SendNetStream 有数据, 但发送不了啊");
                }
            }
        }

        private async void SendNetStream2()
        {
            try
            {
                while (!_disposed)
                {
                    int nLength = 0;
                    lock (mSendStreamList)
                    {
                        nLength = mSendStreamList.WriteTo(mSendBuffer.Span);
                        if (nLength == 0)
                        {
                            bSendIOContextUsed = false;
                            break;
                        }
                    }
                    
                    if (this.mQuicStream == null)
                    {
                        this.mQuicStream = await mClientPeer.mQuicConnection.OpenOutboundStreamAsync(QuicStreamType.Unidirectional);
                    }
                    await this.mQuicStream.WriteAsync(mSendBuffer.Slice(0, nLength));
                }
            }
            catch (QuicException e)
            {
                NetLog.LogError(e.ToString());
                DisConnectedWithError();
            }
        }

        private void DisConnectedWithError()
        {
            if (mClientPeer != null)
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
        }

        //发送----------------------------------------------------------------------------------------

        public void SendNetData(ushort nPackageId)
        {
            if (mClientPeer.GetSocketState() == SOCKET_PEER_STATE.CONNECTED)
            {
                mClientPeer.ResetSendHeartBeatTime();
                var mBufferSegment = mServerMgr.mCryptoMgr.Encode(nStreamEnumIndex, nPackageId, ReadOnlySpan<byte>.Empty);
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
                var mBufferSegment = mServerMgr.mCryptoMgr.Encode(nStreamEnumIndex, nPackageId, data);
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
                var mBufferSegment = mServerMgr.mCryptoMgr.Encode(nStreamEnumIndex, mNetPackage.GetPackageId(), mNetPackage.GetData());
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
                var mBufferSegment = mServerMgr.mCryptoMgr.Encode(nStreamEnumIndex, nPackageId, buffer);
                SendNetStream(mBufferSegment);
            }
            else
            {
                NetLog.LogError("SendNetData Failed");
            }
        }
    }
}
