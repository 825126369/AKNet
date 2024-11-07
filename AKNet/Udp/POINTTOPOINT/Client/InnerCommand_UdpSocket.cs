/************************************Copyright*****************************************
*        ProjectName:AKNet
*        Web:https://github.com/825126369/AKNet
*        Description:这是一个面向 .Net Standard 2.1 的游戏网络库
*        Author:阿珂
*        CreateTime:2024/11/4 20:04:54
*        Copyright:MIT软件许可证
************************************Copyright*****************************************/
using System;
using System.Collections.Concurrent;
using System.Net;
using System.Net.Sockets;
using AKNet.Common;
using AKNet.Udp.POINTTOPOINT.Common;

namespace AKNet.Udp.POINTTOPOINT.Client
{
    internal class InnerCommand_UdpSocket
    {
        private readonly SocketAsyncEventArgs ReceiveArgs;
        private readonly SocketAsyncEventArgs SendArgs;
        private readonly object lock_mSocket_object = new object();
        private readonly ConcurrentQueue<NetUdpFixedSizePackage> mSendPackageQueue = new ConcurrentQueue<NetUdpFixedSizePackage>();

        private Socket mSocket = null;
        private IPEndPoint remoteEndPoint = null;
        bool bReceiveIOContexUsed = false;
        bool bSendIOContexUsed = false;

        ClientPeer mClientPeer;
        public InnerCommand_UdpSocket(ClientPeer mClientPeer)
        {
            this.mClientPeer = mClientPeer;

            mSocket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
            mSocket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, 1);
            NetLog.Log("Default: ReceiveBufferSize: " + mSocket.ReceiveBufferSize);
            mSocket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReceiveBuffer, Config.server_socket_receiveBufferSize);
            NetLog.Log("Fix ReceiveBufferSize: " + mSocket.ReceiveBufferSize);

            ReceiveArgs = new SocketAsyncEventArgs();
            ReceiveArgs.SetBuffer(new byte[Config.nUdpPackageFixedSize], 0, Config.nUdpPackageFixedSize);
            ReceiveArgs.Completed += ProcessReceive;

            SendArgs = new SocketAsyncEventArgs();
            SendArgs.SetBuffer(new byte[Config.nUdpPackageFixedSize], 0, Config.nUdpPackageFixedSize);
            SendArgs.Completed += ProcessSend;

            bReceiveIOContexUsed = false;
            bSendIOContexUsed = false;
        }

        public void ConnectServer(string ip, int nPort)
        {
            remoteEndPoint = new IPEndPoint(IPAddress.Parse(ip), nPort);
            ReceiveArgs.RemoteEndPoint = remoteEndPoint;
            SendArgs.RemoteEndPoint = remoteEndPoint;

            FirstSend();
            StartReceiveEventArg();
        }

        private void FirstSend()
        {
            NetUdpFixedSizePackage mPackage = mClientPeer.GetObjectPoolManager().NetUdpFixedSizePackage_Pop();
            mPackage.nPackageId = UdpNetCommand.COMMAND_FIRST;
            mPackage.Length = Config.nUdpPackageFixedHeadSize;
            NetPackageEncryption.Encryption(mPackage);
            SendNetPackage(mPackage);
        }

        private void StartReceiveEventArg()
        {
            bool bIOSyncCompleted = false;
            if (Config.bUseSocketLock)
            {
                lock (lock_mSocket_object)
                {
                    if (mSocket != null)
                    {
                        bIOSyncCompleted = !mSocket.ReceiveFromAsync(ReceiveArgs);
                    }
                    else
                    {
                        bReceiveIOContexUsed = false;
                    }
                }
            }
            else
            {
                if (mSocket != null)
                {
                    try
                    {
                        bIOSyncCompleted = !mSocket.ReceiveFromAsync(ReceiveArgs);
                    }
                    catch (Exception e)
                    {
                        bReceiveIOContexUsed = false;
                    }
                }
                else
                {
                    bReceiveIOContexUsed = false;
                }
            }

            if (bIOSyncCompleted)
            {
                ProcessReceive(null, ReceiveArgs);
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

        private void ProcessReceive(object sender, SocketAsyncEventArgs e)
        {
            if (e.SocketError == SocketError.Success && e.BytesTransferred > 0)
            {
                NetUdpFixedSizePackage mPackage = mClientPeer.GetObjectPoolManager().NetUdpFixedSizePackage_Pop();
                Buffer.BlockCopy(e.Buffer, e.Offset, mPackage.buffer, 0, e.BytesTransferred);
                mPackage.Length = e.BytesTransferred;
                mClientPeer.mUdpPackageMainThreadMgr.MultiThreadingReceiveNetPackage(mPackage);
            }
            else
            {
                NetLog.LogError(e.SocketError);
            }
            StartReceiveEventArg();
        }

        private void ProcessSend(object sender, SocketAsyncEventArgs e)
        {
            if (e.SocketError == SocketError.Success)
            {
                SendNetPackage2();
            }
            else
            {
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
                    SendNetPackage2();
                }
            }
            else
            {
                mSocket.SendTo(mPackage.buffer, 0, mPackage.Length, SocketFlags.None, remoteEndPoint);
                mClientPeer.GetObjectPoolManager().NetUdpFixedSizePackage_Recycle(mPackage);
            }
        }

        private void SendNetPackage2()
        {
            NetUdpFixedSizePackage mPackage = null;
            if (mSendPackageQueue.TryDequeue(out mPackage))
            {
                Array.Copy(mPackage.buffer, SendArgs.Buffer, mPackage.Length);
                SendArgs.SetBuffer(0, mPackage.Length);
                mClientPeer.GetObjectPoolManager().NetUdpFixedSizePackage_Recycle(mPackage);
                StartSendEventArg();
            }
            else
            {
                bSendIOContexUsed = false;
            }
        }

        private void CloseSocket()
        {
            if (Config.bUseSocketLock)
            {
                lock (lock_mSocket_object)
                {
                    if (mSocket != null)
                    {
                        try
                        {
                            mSocket.Close();
                        }
                        catch (Exception) { }
                        mSocket = null;
                    }
                }
            }
            else
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
        

        public void Reset()
        {
            NetUdpFixedSizePackage mPackage = null;
            while (mSendPackageQueue.TryDequeue(out mPackage))
            {
                mClientPeer.GetObjectPoolManager().NetUdpFixedSizePackage_Recycle(mPackage);
            }
        }

        public void Release()
        {
            CloseSocket();
            NetLog.Log("--------------- Client Release ----------------");
        }
    }
}









