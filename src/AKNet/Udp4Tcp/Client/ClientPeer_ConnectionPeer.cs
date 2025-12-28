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
                Connected = mSocket != null && mSocket.Connected;
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
            mSocket = new Connection();

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
                    Connected = mSocket != null && mSocket.Connected;
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
            bool bIOSyncCompleted = false;

            if (mSocket != null)
            {
                try
                {
                    bIOSyncCompleted = !mSocket.ConnectAsync(ConnectArgs);
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

            if (bIOSyncCompleted)
            {
                this.ProcessConnect(ConnectArgs);
            }
        }

        private void StartDisconnectEventArg()
        {
            bool bIOSyncCompleted = false;
            if (mSocket != null)
            {
                try
                {
                    bIOSyncCompleted = !mSocket.DisconnectAsync(DisConnectArgs);
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
                
            if (bIOSyncCompleted)
            {
                this.ProcessDisconnect(DisConnectArgs);
            }
        }


        private void StartReceiveEventArg()
        {
            bool bIOSyncCompleted = false;

            if (mSocket != null)
            {
                try
                {
                    bIOSyncCompleted = !mSocket.ReceiveAsync(ReceiveArgs);
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
            
            if (bIOSyncCompleted)
            {
                this.ProcessReceive(ReceiveArgs);
            }
        }

        private void StartSendEventArg()
        {
            bool bIOSyncCompleted = false;

            if (mSocket != null)
            {
                try
                {
                    bIOSyncCompleted = !mSocket.SendAsync(SendArgs);
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


            if (bIOSyncCompleted)
            {
                this.ProcessSend(SendArgs);
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
                    MultiThreadingReceiveSocketStream(e);
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
            if (mSocket != null)
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
            var mSocketPeerState = GetSocketState();
            if (mSocketPeerState == SOCKET_PEER_STATE.DISCONNECTING)
            {
                SetSocketState(SOCKET_PEER_STATE.DISCONNECTED);
            }
            else if (mSocketPeerState == SOCKET_PEER_STATE.CONNECTED)
            {
                SetSocketState(SOCKET_PEER_STATE.RECONNECTING);
            }
        }

        public IPEndPoint GetIPEndPoint()
        {
            IPEndPoint mRemoteEndPoint = null;
            try
            {
                if (mSocket != null && mSocket.RemoteEndPoint != null)
                {
                    mRemoteEndPoint = mSocket.RemoteEndPoint as IPEndPoint;
                }
            }
            catch { }

            return mRemoteEndPoint;
        }

        private void CloseSocket()
        {
            if (mSocket != null)
            {
                Connection mSocket2 = mSocket;
                mSocket = null;

                try
                {
                    mSocket2.Close();
                }
                catch { }
            }
        }
    }
}
