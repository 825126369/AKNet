/************************************Copyright*****************************************
*        ProjectName:AKNet
*        Web:https://github.com/825126369/AKNet
*        Description:这是一个面向 .Net Standard 2.1 的游戏网络库
*        Author:阿珂
*        CreateTime:2024/11/4 20:04:54
*        Copyright:MIT软件许可证
************************************Copyright*****************************************/
using AKNet.Common;
using System;
using System.Collections.Concurrent;
using System.Net.Sockets;

namespace AKNet.Udp.POINTTOPOINT.Common
{
    internal class UdpNetCommandSocketMgr
    {
        private UdpClientPeerCommonBase mClientPeer = null;
        readonly SocketAsyncEventArgs SendArgs = new SocketAsyncEventArgs();
        readonly ConcurrentQueue<NetUdpFixedSizePackage> mSendPackageQueue = new ConcurrentQueue<NetUdpFixedSizePackage>();
        bool bSendIOContexUsed = false;
        Socket mSocket = null;
        private readonly object lock_mSocket_object = new object();

        public UdpNetCommandSocketMgr(Socket mSocket, UdpClientPeerCommonBase mClientPeer)
        {
            this.mSocket = mSocket;
            this.mClientPeer = mClientPeer;
            SendArgs.Completed += ProcessSend;
            SendArgs.SetBuffer(new byte[Config.nUdpPackageFixedSize], 0, Config.nUdpPackageFixedSize);
        }

        private void ProcessSend(object sender, SocketAsyncEventArgs e)
        {
            if (e.SocketError == SocketError.Success)
            {
                SendNetPackage2(e);
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
            MainThreadCheck.Check();
            if (Config.bUseSendAsync)
            {
                mSendPackageQueue.Enqueue(mPackage);
                if (!bSendIOContexUsed)
                {
                    bSendIOContexUsed = true;
                    SendNetPackage2(SendArgs);
                }
            }
            else
            {
                mSocket.SendTo(mPackage.buffer, 0, mPackage.Length, SocketFlags.None, mPackage.remoteEndPoint);
                mClientPeer.GetObjectPoolManager().NetUdpFixedSizePackage_Recycle(mPackage);
            }
        }

        private void SendNetPackage2(SocketAsyncEventArgs e)
        {
            NetUdpFixedSizePackage mPackage = null;
            if (mSendPackageQueue.TryDequeue(out mPackage))
            {
                Array.Copy(mPackage.buffer, e.Buffer, mPackage.Length);
                e.SetBuffer(0, mPackage.Length);
                e.RemoteEndPoint = mPackage.remoteEndPoint;

                StartSendEventArg();
            }
            else
            {
                bSendIOContexUsed = false;
            }
        }

        private void StartSendEventArg()
        {
            bool bIOSyncCompleted = false;
            if (Config.bUseSocketLock)
            {
                lock (lock_mSocket_object)
                {
                    if (mSocket != null)
                    {
                        bIOSyncCompleted = !mSocket.SendToAsync(SendArgs);
                    }
                    else
                    {
                        bSendIOContexUsed = false;
                    }
                }
            }
            else
            {
                if (mSocket != null)
                {
                    try
                    {
                        bIOSyncCompleted = !mSocket.SendToAsync(SendArgs);
                    }
                    catch (Exception e)
                    {
                        bSendIOContexUsed = false;
                    }
                }
                else
                {
                    bSendIOContexUsed = false;
                }
            }

            if (bIOSyncCompleted)
            {
                ProcessSend(null, SendArgs);
            }
        }

        public void Reset()
        {
            NetUdpFixedSizePackage mPackage = null;
            while (mSendPackageQueue.TryDequeue(out mPackage))
            {
                mClientPeer.GetObjectPoolManager().NetUdpFixedSizePackage_Recycle(mPackage);
            }
        }
    }
}
