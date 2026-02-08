/************************************Copyright*****************************************
*        ProjectName:AKNet
*        Web:https://github.com/825126369/AKNet
*        Description:C#游戏网络库
*        Author:许珂
*        StartTime:2024/11/01 00:00:00
*        ModifyTime:2026/2/1 20:26:48
*        Copyright:MIT软件许可证
************************************Copyright*****************************************/
using AKNet.Common;
using AKNet.Udp1Tcp.Common;
using System;
using System.Net;
using System.Net.Sockets;

namespace AKNet.Udp1Tcp.Server
{
    internal partial class ClientPeer
    {
        public void HandleConnectedSocket(FakeSocket mSocket)
        {
            MainThreadCheck.Check();
            NetLog.Assert(mSocket != null, "mSocket == null");

            this.mSocket = mSocket;
            this.mIPEndPoint = mSocket.RemoteEndPoint;

            SendArgs.RemoteEndPoint = this.mIPEndPoint;

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
                return mIPEndPoint;
            }
        }

        public int GetCurrentFrameRemainPackageCount()
        {
            return mSocket.GetCurrentFrameRemainPackageCount();
        }

        public bool GetReceivePackage(out NetUdpFixedSizePackage mPackage)
        {
            return mSocket.GetReceivePackage(out mPackage);
        }

        public bool SendToAsync(SocketAsyncEventArgs e)
        {
            bool bIOPending = true;
            if (mSocket != null)
            {
                try
                {
                    bIOPending = mSocket.SendToAsync(e);
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

            return bIOPending;
        }

        private void ProcessSend(object sender, SocketAsyncEventArgs e)
        {
            if (e.SocketError == SocketError.Success)
            {
                if (Config.bUseSendStream)
                {
                    SendNetStream2(e.BytesTransferred);
                }
                else
                {
                    SendNetPackage2(e.BytesTransferred);
                }
            }
            else
            {
                NetLog.LogError(e.SocketError);
                SetSocketState(SOCKET_PEER_STATE.DISCONNECTED);
                bSendIOContexUsed = false;
            }
        }

        public void SendNetPackage1(NetUdpFixedSizePackage mPackage)
        {
            mPackage.remoteEndPoint = GetIPEndPoint();
            UdpPackageEncryption.Encode(mPackage);

            MainThreadCheck.Check();
            if (Config.bUseSendAsync)
            {
                if (Config.bUseSendStream)
                {
                    lock (mSendStreamList)
                    {
                        mSendStreamList.WriteFrom(mPackage.GetBufferSpan());
                    }

                    if (!bSendIOContexUsed)
                    {
                        bSendIOContexUsed = true;
                        SendNetStream2();
                    }
                }
                else
                {
                    mSendPackageQueue.Enqueue(mPackage);
                    if (!bSendIOContexUsed)
                    {
                        bSendIOContexUsed = true;
                        SendNetPackage2();
                    }
                }
            }
            else
            {
                mServerMgr.SendTo(mPackage);
                if (!Config.bUseSendStream)
                {
                    GetObjectPoolManager().NetUdpFixedSizePackage_Recycle(mPackage);
                }
            }
        }

        private void SendNetPackage2(int BytesTransferred = -1)
        {
            NetUdpFixedSizePackage mPackage = null;
            if (mSendPackageQueue.Count > 0)
            {
                int nSendBytesCount = 0;
                if (Config.bSocketSendMultiPackage)
                {
                    while (mSendPackageQueue.TryPeek(out mPackage))
                    {
                        if (mPackage.Length + nSendBytesCount <= SendArgs.Buffer.Length)
                        {
                            if (mSendPackageQueue.TryDequeue(out mPackage))
                            {
                                Buffer.BlockCopy(mPackage.buffer, 0, SendArgs.Buffer, nSendBytesCount, mPackage.Length);
                                nSendBytesCount += mPackage.Length;
                                GetObjectPoolManager().NetUdpFixedSizePackage_Recycle(mPackage);
                            }
                            else
                            {
                                break;
                            }
                        }
                        else
                        {
                            break;
                        }
                    }
                }
                else
                {
                    if (mSendPackageQueue.TryDequeue(out mPackage))
                    {
                        Buffer.BlockCopy(mPackage.buffer, 0, SendArgs.Buffer, nSendBytesCount, mPackage.Length);
                        nSendBytesCount += mPackage.Length;
                        GetObjectPoolManager().NetUdpFixedSizePackage_Recycle(mPackage);
                    }
                }

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
            if (Config.bSocketSendMultiPackage)
            {
                lock (mSendStreamList)
                {
                    nSendBytesCount += mSendStreamList.WriteToMax(mSendArgSpan);
                }
            }
            else
            {
                lock (mSendStreamList)
                {
                    nSendBytesCount += mSendStreamList.WriteTo(mSendArgSpan);
                }
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
