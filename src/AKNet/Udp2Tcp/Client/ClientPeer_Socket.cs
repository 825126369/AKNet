/************************************Copyright*****************************************
*        ProjectName:AKNet
*        Web:https://github.com/825126369/AKNet
*        Description:C#游戏网络库
*        Author:许珂
*        StartTime:2024/11/01 00:00:00
*        ModifyTime:2026/2/1 20:26:49
*        Copyright:MIT软件许可证
************************************Copyright*****************************************/
using AKNet.Common;
using AKNet.Udp2Tcp.Common;
using System;
using System.Net;
using System.Net.Sockets;

namespace AKNet.Udp2Tcp.Client
{
    internal partial class ClientPeer
    {
        public void ConnectServer(string ip, int nPort)
        {
            this.ServerPort = nPort;
            this.ServerIp = ip;
            this.remoteEndPoint = new IPEndPoint(IPAddress.Parse(ServerIp), ServerPort);
            //mSocket.Connect(remoteEndPoint);
            ReceiveArgs.RemoteEndPoint = remoteEndPoint;
            SendArgs.RemoteEndPoint = remoteEndPoint;
            ConnectServer();
            StartReceiveEventArg();
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
            bool bIOPending = false;
            if (mSocket != null)
            {
                try
                {
                    bIOPending = mSocket.ReceiveFromAsync(ReceiveArgs);
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
            
            UdpStatistical.AddReceiveIOCount(!bIOPending);
            if (!bIOPending)
            {
                ProcessReceive(null, ReceiveArgs);
            }
        }

        private void StartSendEventArg()
        {
            bool bIOPending = false;
            if (mSocket != null)
            {
                try
                {
                    bIOPending = mSocket.SendToAsync(SendArgs);
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
            
            UdpStatistical.AddSendIOCount(!bIOPending);
            if (!bIOPending)
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

        public void SendNetPackage2(NetUdpFixedSizePackage mPackage)
        {
            UdpPackageEncryption.Encode(mPackage);
            MainThreadCheck.Check();
            if (Config.bUseSendAsync)
            {
                lock (mSendStreamList)
                {
                    mSendStreamList.WriteFromOneSpan(mPackage.GetBufferSpan());
                }

                if (!bSendIOContexUsed)
                {
                    bSendIOContexUsed = true;
                    SendNetStream2();
                }
            }
            else
            {
                mSocket.SendTo(mPackage.buffer, 0, mPackage.Length, SocketFlags.None, remoteEndPoint);
            }
        }

        int nLastSendBytesCount = 0;
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









