/************************************Copyright*****************************************
*        ProjectName:AKNet
*        Web:https://github.com/825126369/AKNet
*        Description:这是一个面向 .Net Standard 2.1 的游戏网络库
*        Author:阿珂
*        CreateTime:2024/11/28 7:14:06
*        Copyright:MIT软件许可证
************************************Copyright*****************************************/
using AKNet.Common;
using AKNet.Udp3Tcp.Common;
using System;
using System.Collections.Concurrent;
using System.Net;
using System.Net.Sockets;

namespace AKNet.Udp3Tcp.Server
{
    internal class ClientPeerSocketMgr
    {
        private UdpServer mNetServer = null;
        private ClientPeer mClientPeer = null;

        FakeSocket mSocket = null;
        readonly object lock_mSocket_object =new object();

        readonly SocketAsyncEventArgs SendArgs = new SocketAsyncEventArgs();
        readonly AkCircularSpanBuffer<byte> mSendStreamList = null;
        bool bSendIOContexUsed = false;

        IPEndPoint mIPEndPoint;

        public ClientPeerSocketMgr(UdpServer mNetServer, ClientPeer mClientPeer)
        {
            this.mNetServer = mNetServer;
            this.mClientPeer = mClientPeer;

            SendArgs.Completed += ProcessSend;
            SendArgs.SetBuffer(new byte[Config.nUdpPackageFixedSize], 0, Config.nUdpPackageFixedSize);
            mSendStreamList = new AkCircularSpanBuffer<byte>();
        }

        public void HandleConnectedSocket(FakeSocket mSocket)
        {
            MainThreadCheck.Check();
            NetLog.Assert(mSocket != null, "mSocket == null");

            this.mSocket = mSocket;
            this.mIPEndPoint = mSocket.RemoteEndPoint;

            SendArgs.RemoteEndPoint = this.mIPEndPoint;
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
            bool bIOSyncCompleted = false;
            if (Config.bUseSocketLock)
            {
                lock (lock_mSocket_object)
                {
                    if (mSocket != null)
                    {
                        bIOSyncCompleted = !mSocket.SendToAsync(e);
                    }
                }
            }
            else
            {
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
                mClientPeer.SetSocketState(SOCKET_PEER_STATE.DISCONNECTED);
                bSendIOContexUsed = false;
            }
        }

        public void SendNetPackage(NetUdpFixedSizePackage mPackage)
        {
            mPackage.remoteEndPoint = GetIPEndPoint();
            mNetServer.GetCryptoMgr().Encode(mPackage);

            MainThreadCheck.Check();
            if (Config.bUseSendAsync)
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
                mNetServer.GetSocketMgr().SendTo(mPackage);
            }
        }
        
        public void Reset()
        {
            lock (mSendStreamList)
            {
                mSendStreamList.reset();
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
