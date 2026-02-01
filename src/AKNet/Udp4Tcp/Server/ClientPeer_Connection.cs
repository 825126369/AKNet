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
using AKNet.Udp4Tcp.Common;
using System;
using System.Net;
using System.Threading.Tasks;

namespace AKNet.Udp4Tcp.Server
{
    internal partial class ClientPeer
    {
        public IPEndPoint GetIPEndPoint()
        {
            if (mConnection != null)
            {
                return mConnection.RemoteEndPoint;
            }
            else
            {
                return null;
            }
        }

        public void HandleConnectedSocket(Connection mConnection)
        {
            MainThreadCheck.Check();
            NetLog.Assert(mConnection != null, "mConnectionPeer == null");
            this.mConnection = mConnection;
            SetSocketState(SOCKET_PEER_STATE.CONNECTED);

            StartReceiveEventArg();
        }

        public bool DisConnectServer()
        {
            DisConnectServer2();
            return true;
        }

        private async ValueTask<bool> DisConnectServer2()
        {
            NetLog.Log("客户端 主动 断开服务器 Begin......");
            MainThreadCheck.Check();

            SetSocketState(SOCKET_PEER_STATE.DISCONNECTING);
            try
            {
                await mConnection.DisconnectAsync().ConfigureAwait(false);
            }
            catch (Exception e)
            {

            }
            
            SetSocketState(SOCKET_PEER_STATE.DISCONNECTED);
            return GetSocketState() == SOCKET_PEER_STATE.DISCONNECTED;
        }
        
        private async void StartReceiveEventArg()
        {
            await Task.Delay(1).ConfigureAwait(false); //这里主要是 不要在主线程中循环
            while (true)
            {
                int nReadLength = 0;
                try
                {
                    nReadLength = await mConnection.ReceiveAsync(ReceiveArgs).ConfigureAwait(false);
                }
                catch(Exception e)
                {
                    DisConnectedWithException(e);
                    break;
                }
                MultiThreadingReceiveStream(ReceiveArgs.Span.Slice(0, nReadLength));
            }
        }

        public void SendNetStream(ReadOnlySpan<byte> mBufferSegment)
        {
            ResetSendHeartBeatCdTime();
            lock (mSendStreamList)
            {
                mSendStreamList.WriteFrom(mBufferSegment);
            }

            if (!bSendIOContexUsed)
            {
                bSendIOContexUsed = true;
                SendNetStream1();
            }
            else
            {
                if (!bSendIOContexUsed && mSendStreamList.Length > 0)
                {
                    throw new Exception("SendNetStream 有数据, 但发送不了啊");
                }
            }
        }

        private async void SendNetStream1()
        {
            int nLength = mSendStreamList.Length;
            if (nLength > 0)
            {
                nLength = Math.Min(SendArgs.Length, nLength);
                Memory<byte> mMemory = SendArgs.Slice(0, nLength);
                lock (mSendStreamList)
                {
                    mSendStreamList.CopyTo(mMemory.Span);
                }

                int BytesTransferred = await mConnection.SendAsync(mMemory).ConfigureAwait(false);
                if (BytesTransferred > 0)
                {
                    lock (mSendStreamList)
                    {
                        mSendStreamList.ClearBuffer(BytesTransferred);
                    }
                }

                SendNetStream1();
            }
            else
            {
                bSendIOContexUsed = false;
            }
        }

        private void DisConnectedWithNormal()
        {
#if DEBUG
            NetLog.Log("客户端 正常 断开服务器 ");
#endif
            DisConnectedWithError();
        }

        private void DisConnectedWithException(Exception e)
        {
#if DEBUG
            if (mConnection != null)
            {
                NetLog.LogException(e);
            }
#endif
            DisConnectedWithError();
        }

        private void DisConnectedWithSocketError(ConnectionError mError)
        {
#if DEBUG
            NetLog.LogError(mError);
#endif
            DisConnectedWithError();
        }

        private void DisConnectedWithError()
        {
            var mConnectionPeerState = GetSocketState();
            if (mConnectionPeerState == SOCKET_PEER_STATE.DISCONNECTING)
            {
                SetSocketState(SOCKET_PEER_STATE.DISCONNECTED);
            }
            else if (mConnectionPeerState == SOCKET_PEER_STATE.CONNECTED)
            {
                SetSocketState(SOCKET_PEER_STATE.RECONNECTING);
            }
        }

        public void CloseSocket()
        {
            if (mConnection != null)
            {
                Connection mConnection2 = mConnection;
                mConnection = null;

                try
                {
                    mConnection2.Dispose();
                }
                catch { }
            }
        }

    }
}
