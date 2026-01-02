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
using AKNet.Udp4Tcp.Common;
using System;
using System.Net;

namespace AKNet.Udp4Tcp.Client
{
    internal partial class ClientPeer
    {
        public void ReConnectServer()
        {
            bool Connected = false;
            try
            {
                Connected = mConnection != null && mConnection.Connected;
            }
            catch { }
            
            if (Connected)
            {
                SetSocketState(SOCKET_PEER_STATE.CONNECTED);
            }
            else
            {
                ConnectServer(this.ServerIp, this.nServerPort);
            }
        }

        public void ConnectServer(string ServerAddr, int ServerPort)
        {
            this.ServerIp = ServerAddr;
            this.nServerPort = ServerPort;

            SetSocketState(SOCKET_PEER_STATE.CONNECTING);
            NetLog.Log("Client 正在连接服务器: " + this.ServerIp + " | " + this.nServerPort);

            Reset();

            mConnection = new Connection();
            if (mIPEndPoint == null)
            {
                IPAddress mIPAddress = IPAddress.Parse(ServerAddr);
                mIPEndPoint = new IPEndPoint(mIPAddress, ServerPort);
            }

            if (!bConnectIOContexUsed)
            {
                bConnectIOContexUsed = true;
                ConnectArgs.RemoteEndPoint = mIPEndPoint;
                StartConnectEventArg();
            }
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
                    DisConnectArgs.RemoteEndPoint = mIPEndPoint;
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

        private void StartConnectEventArg()
        {
            if (mConnection != null)
            {
                try
                {
                    bool bIOPending = mConnection.ConnectAsync(ConnectArgs);
                    if (!bIOPending)
                    {
                        this.ProcessConnect(ConnectArgs);
                    }
                }
                catch (Exception e)
                {
                    bConnectIOContexUsed = false;
                    DisConnectedWithException(e);
                }
            }
            else
            {
                bConnectIOContexUsed = false;
            }
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

        private void StartSendEventArg()
        {
            bool bIOPending = false;
            if (mConnection != null)
            {
                try
                {
                    bIOPending = mConnection.SendAsync(SendArgs);
                    if (!bIOPending)
                    {
                        this.ProcessSend(SendArgs);
                    }
                }
                catch (Exception e)
                {
                    bSendIOContexUsed = false;
                    DisConnectedWithException(e);
                }
            }
            else
            {
                bSendIOContexUsed = false;
            } 
        }

        private void OnIOCompleted(object sender, ConnectionEventArgs e)
        {
            switch (e.LastOperation)
            {
                case ConnectionAsyncOperation.Connect:
                    ProcessConnect(e);
                    break;
                case ConnectionAsyncOperation.Disconnect:
                    ProcessDisconnect(e);
                    break;
                case ConnectionAsyncOperation.Receive:
                    this.ProcessReceive(e);
                    break;
                case ConnectionAsyncOperation.Send:
                    this.ProcessSend(e);
                    break;
                default:
                    NetLog.LogError("The last operation completed on the socket was not a receive or send");
                    break;
            }
        }

        private void ProcessConnect(ConnectionEventArgs e)
        {
            if (e.ConnectionError == ConnectionError.Success)
            {
                NetLog.Log(string.Format("Client 连接服务器: {0}:{1} 成功", this.ServerIp, this.nServerPort));
                SetSocketState(SOCKET_PEER_STATE.CONNECTED);

                if (!bReceiveIOContexUsed)
                {
                    bReceiveIOContexUsed = true;
                    StartReceiveEventArg();
                }
            }
            else
            {
                NetLog.Log(string.Format("Client 连接服务器: {0}:{1} 失败：{2}", this.ServerIp, this.nServerPort, e.ConnectionError));
                if (GetSocketState() == SOCKET_PEER_STATE.CONNECTING)
                {
                    SetSocketState(SOCKET_PEER_STATE.RECONNECTING);
                }
            }

            e.RemoteEndPoint = null;
            bConnectIOContexUsed = false;
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

            e.RemoteEndPoint = null;
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

        private void ProcessSend(ConnectionEventArgs e)
        {
            if (e.ConnectionError == ConnectionError.Success)
            {
                if (e.BytesTransferred > 0)
                {
                    SendNetStream1(e.BytesTransferred);
                }
                else
                {
                    DisConnectedWithNormal();
                    bSendIOContexUsed = false;
                }
            }
            else
            {
                DisConnectedWithSocketError(e.ConnectionError);
                bSendIOContexUsed = false;
            }
        }

        public void SendNetStream(ReadOnlySpan<byte> mBufferSegment)
        {
            ResetSendHeartBeatTime();
            lock (mSendStreamList)
            {
                mSendStreamList.WriteFrom(mBufferSegment);
            }

            if (!bSendIOContexUsed)
            {
                bSendIOContexUsed = true;
                SendNetStream1();
            }
        }

        private void SendNetStream1(int BytesTransferred = 0)
        {
            if (BytesTransferred > 0)
            {
                lock (mSendStreamList)
                {
                    mSendStreamList.ClearBuffer(BytesTransferred);
                }
            }

            int nLength = mSendStreamList.Length;
            if (nLength > 0)
            {
                nLength = Math.Min(SendArgs.MemoryBuffer.Length, nLength);
                lock (mSendStreamList)
                {
                    mSendStreamList.CopyTo(SendArgs.MemoryBuffer.Span.Slice(0, nLength));
                }

                SendArgs.SetBuffer(0, nLength);
                StartSendEventArg();
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

        public IPEndPoint GetIPEndPoint()
        {
            IPEndPoint mRemoteEndPoint = null;
            try
            {
                if (mConnection != null)
                {
                    mRemoteEndPoint = mConnection.RemoteEndPoint;
                }
            }
            catch { }

            return mRemoteEndPoint;
        }

        private void CloseSocket()
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
