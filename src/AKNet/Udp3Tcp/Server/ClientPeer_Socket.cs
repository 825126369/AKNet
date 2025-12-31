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
using AKNet.Udp3Tcp.Common;
using System;
using System.Net;
using System.Net.Sockets;

namespace AKNet.Udp3Tcp.Server
{
    internal partial class ClientPeer
    {
        public void HandleConnectedSocket(FakeSocket mSocket)
        {
            MainThreadCheck.Check();
            NetLog.Assert(mSocket != null, "mSocket == null");

            this.mSocket = mSocket;
            this.SendArgs.RemoteEndPoint = mSocket.RemoteEndPoint;
            SetSocketState(SOCKET_PEER_STATE.CONNECTED);
        }

        public IPEndPoint GetIPEndPoint()
        {
            if (mSocket != null)
            {
                return mSocket.RemoteEndPoint;
            }
            else
            {
                return null;
            }
        }

        public int GetCurrentFrameRemainPackageCount()
        {
            return mSocket.GetCurrentFrameRemainPackageCount();
        }

        public bool GetReceivePackage(out NetUdpReceiveFixedSizePackage mPackage)
        {
            return mSocket.GetReceivePackage(out mPackage);
        }

        public bool SendToAsync(SocketAsyncEventArgs e)
        {
            bool bIOSyncCompleted = false;
            if (mSocket != null)
            {
                try
                {
                    bIOSyncCompleted = !mSocket.SendToAsync(e);
                }
                catch (Exception ex)
                {
                    bSendIOContexUsed = false;
                    if (mSocket != null)
                    {
                        NetLog.LogException(ex);
                    }
                }
            }
            return !bIOSyncCompleted;
        }

        private void ProcessSend(object sender, SocketAsyncEventArgs e)
        {
            if (e.SocketError == SocketError.Success)
            {
                SendNetStream2(e.BytesTransferred);
            }
            else
            {
                NetLog.LogError(e.SocketError);
                SetSocketState(SOCKET_PEER_STATE.DISCONNECTED);
                bSendIOContexUsed = false;
            }
        }

        public void SendNetPackage2(NetUdpSendFixedSizePackage mPackage)
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
                if (!SendToAsync(SendArgs))
                {
                    ProcessSend(null, SendArgs);
                }
            }
            else
            {
                bSendIOContexUsed = false;
            }
        }

        public void CloseSocket()
        {
            if (mSocket != null)
            {
                mSocket.Close();
                mSocket = null;
            }
        }

    }
}
