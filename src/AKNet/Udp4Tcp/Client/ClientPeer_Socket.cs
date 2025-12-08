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
using System.Net.Sockets;

namespace AKNet.Udp4Tcp.Client
{
    internal partial class ClientPeer
    {
        public void ConnectServer(string ip, int nPort)
        {
            this.port = nPort;
            this.ip = ip;
            this.remoteEndPoint = new IPEndPoint(IPAddress.Parse(ip), port);
            this.ReceiveArgs.RemoteEndPoint = remoteEndPoint;
            this.SendArgs.RemoteEndPoint = remoteEndPoint;
            this.ConnectServer();
            this.StartReceiveEventArg();
        }

        public void ConnectServer()
        {
            SendConnect();
        }

        public void ReConnectServer()
        {
            SendConnect();
        }

        public IPEndPoint GetIPEndPoint()
        {
            return remoteEndPoint;
        }

        public bool DisConnectServer()
        {
            if (mSocketPeerState == SOCKET_PEER_STATE.CONNECTED || mSocketPeerState == SOCKET_PEER_STATE.CONNECTING)
            {
                SendDisConnect();
                return false;
            }
            else
            {
                return true;
            }
        }

        private void StartReceiveEventArg()
        {
            bool bIOSyncCompleted = false;
            if (mSocket != null)
            {
                try
                {
                    bIOSyncCompleted = !mSocket.ReceiveFromAsync(ReceiveArgs);
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
                ProcessReceive(null, ReceiveArgs);
            }
        }

        private void StartSendEventArg()
        {
            bool bIOSyncCompleted = false;

            if (mSocket != null)
            {
                try
                {
                    bIOSyncCompleted = !mSocket.SendToAsync(SendArgs);
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
                ProcessSend(null, SendArgs);
            }
        }

        private void ProcessReceive(object sender, SocketAsyncEventArgs e)
        {
            if (e.SocketError == SocketError.Success && e.BytesTransferred > 0)
            {
                MultiThreading_ReceiveWaitCheckNetPackage(e);
            }
            
            StartReceiveEventArg();
        }

        private void ProcessSend(object sender, SocketAsyncEventArgs e)
        {
            if (e.SocketError == SocketError.Success)
            {
                SendNetStream2(e.BytesTransferred);
            }
            else
            {
                bSendIOContexUsed = false;
                DisConnectedWithSocketError(e.SocketError);
            }
        }

        private void SendNetPackage2(NetUdpSendFixedSizePackage mPackage)
        {
            MainThreadCheck.Check();

            lock (mSendStreamList)
            {
                var mBufferItem = mSendStreamList.BeginSpan();
                mBufferItem.AddSpan(UdpPackageEncryption.EncodeHead(mPackage));
                if (mPackage.WindowBuff != null)
                {
                    mPackage.WindowBuff.CopyTo(mBufferItem.GetCanWriteSpan(), mPackage.WindowOffset, mPackage.WindowLength);
                    mBufferItem.nSpanLength += mPackage.WindowLength;
                }
                mSendStreamList.FinishSpan();
            }

            if (!bSendIOContexUsed)
            {
                bSendIOContexUsed = true;
                SendNetStream2();
            }
        }
        
        private void SendNetStream2(int BytesTransferred = -1)
        {
            if (BytesTransferred >= 0)
            {
                if (BytesTransferred != nLastSendBytesCount)
                {
                    NetLog.LogError("UDP 发生短写");
                }
            }

            var mSendArgSpan = SendArgs.Buffer.AsSpan();
            int nSendBytesCount = 0;
            lock (mSendStreamList)
            {
                nSendBytesCount += mSendStreamList.WriteToMax(mSendArgSpan);
            }

            if (nSendBytesCount > 0)
            {
                nLastSendBytesCount = nSendBytesCount;
                SendArgs.SetBuffer(0, nSendBytesCount);
                StartSendEventArg();
            }
            else
            {
                bSendIOContexUsed = false;
            }
        }

        public void DisConnectedWithNormal()
        {
            NetLog.Log("客户端 正常 断开服务器 ");
            SetSocketState(SOCKET_PEER_STATE.DISCONNECTED);
        }

        private void DisConnectedWithException(Exception e)
        {
            if (mSocket != null)
            {
                NetLog.LogException(e);
            }
            DisConnectedWithError();
        }

        private void DisConnectedWithSocketError(SocketError e)
        {
            DisConnectedWithError();
        }

        private void DisConnectedWithError()
        {
            if (mSocketPeerState == SOCKET_PEER_STATE.DISCONNECTING)
            {
                SetSocketState(SOCKET_PEER_STATE.DISCONNECTED);
            }
            else if (mSocketPeerState == SOCKET_PEER_STATE.CONNECTED || mSocketPeerState == SOCKET_PEER_STATE.CONNECTING)
            {
                SetSocketState(SOCKET_PEER_STATE.RECONNECTING);
            }
        }

        private void CloseSocket()
        {
            if (mSocket != null)
            {
                Socket mSocket2 = mSocket;
                mSocket = null;

                try
                {
                    mSocket2.Close();
                }
                catch (Exception) { }
            }
        }
    }
}









