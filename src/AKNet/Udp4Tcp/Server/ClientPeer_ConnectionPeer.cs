/************************************Copyright*****************************************
*        ProjectName:AKNet
*        Web:https://github.com/825126369/AKNet
*        Description:C#游戏网络库
*        Author:许珂
*        StartTime:2024/11/01 00:00:00
*        ModifyTime:2025/11/30 19:43:16
*        Copyright:MIT软件许可证
************************************Copyright*****************************************/
using AKNet.Common;
using AKNet.Udp4Tcp.Common;
using System;
using System.Net;

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
        }

        public bool DisConnectServer()
        {
            NetLog.Log("客户端 主动 断开服务器 Begin......");

            MainThreadCheck.Check();
            if (!bDisConnectIOContexUsed)
            {
                bDisConnectIOContexUsed = true;

                bool Connected = false;
                try
                {
                    Connected = mConnection != null && mConnection.Connected;
                }
                catch { }

                if (Connected)
                {
                    SetSocketState(SOCKET_PEER_STATE.DISCONNECTING);
                    StartDisconnectEventArg();
                }
                else
                {
                    NetLog.Log("客户端 主动 断开服务器 Finish......");
                    SetSocketState(SOCKET_PEER_STATE.DISCONNECTED);
                    bDisConnectIOContexUsed = false;

                }
            }

            return GetSocketState() == SOCKET_PEER_STATE.DISCONNECTED;
        }

        private void StartDisconnectEventArg()
        {
            if (mConnection != null)
            {
                try
                {
                    bool bIOPending = mConnection.DisconnectAsync(DisConnectArgs);
                    if (!bIOPending)
                    {
                        this.ProcessDisconnect(DisConnectArgs);
                    }
                }
                catch (Exception e)
                {
                    bDisConnectIOContexUsed = false;
                    DisConnectedWithException(e);
                }
            }
            else
            {
                bDisConnectIOContexUsed = false;
            }
        }


        private void StartReceiveEventArg()
        {
            if (mConnection != null)
            {
                try
                {
                    bool bIOPending = mConnection.ReceiveAsync(ReceiveArgs);
                    if (!bIOPending)
                    {
                        this.ProcessReceive(ReceiveArgs);
                    }
                }
                catch (Exception e)
                {
                    bReceiveIOContexUsed = false;
                    DisConnectedWithException(e);
                }
            }
            else
            {
                bReceiveIOContexUsed = false;
            }
        }

        private void OnIOCompleted(object sender, ConnectionEventArgs e)
        {
            switch (e.LastOperation)
            {
                case ConnectionAsyncOperation.Disconnect:
                    ProcessDisconnect(e);
                    break;
                case ConnectionAsyncOperation.Receive:
                    this.ProcessReceive(e);
                    break;
                default:
                    NetLog.LogError("The last operation completed on the socket was not a receive or send");
                    break;
            }
        }

        private void ProcessDisconnect(ConnectionEventArgs e)
        {
            if (e.ConnectionError == ConnectionError.Success)
            {
                SetSocketState(SOCKET_PEER_STATE.DISCONNECTED);
                NetLog.Log("客户端 主动 断开服务器 Finish");
            }
            else
            {
                DisConnectedWithSocketError(e.ConnectionError);
            }

            bDisConnectIOContexUsed = false;
        }

        private void ProcessReceive(ConnectionEventArgs e)
        {
            if (e.ConnectionError == ConnectionError.Success)
            {
                if (e.BytesTransferred > 0)
                {
                    MultiThreadingReceiveStream(e);
                    StartReceiveEventArg();
                }
                else
                {
                    bReceiveIOContexUsed = false;
                    DisConnectedWithNormal();
                }
            }
            else
            {
                bReceiveIOContexUsed = false;
                DisConnectedWithSocketError(e.ConnectionError);
            }
        }

        public void SendNetStream(ReadOnlySpan<byte> mBufferSegment)
        {
            mConnection.Send(mBufferSegment);
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
