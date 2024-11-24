/************************************Copyright*****************************************
*        ProjectName:AKNet
*        Web:https://github.com/825126369/AKNet
*        Description:这是一个面向 .Net Standard 2.1 的游戏网络库
*        Author:阿珂
*        CreateTime:2024/11/23 22:12:37
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
    internal class SocketUdp
    {
        private readonly SocketAsyncEventArgs ReceiveArgs;
        private readonly SocketAsyncEventArgs SendArgs;
        private readonly object lock_mSocket_object = new object();

        readonly ConcurrentQueue<NetUdpFixedSizePackage> mSendPackageQueue = null;
        readonly AkCircularSpanBuffer<byte> mSendStreamList = null;

        private Socket mSocket = null;
        private IPEndPoint remoteEndPoint = null;
        private string ip;
        private int port;
        
        bool bReceiveIOContexUsed = false;
        bool bSendIOContexUsed = false;

        ClientPeer mClientPeer;
        public SocketUdp(ClientPeer mClientPeer)
        {
            this.mClientPeer = mClientPeer;
            mClientPeer.SetSocketState(SOCKET_PEER_STATE.NONE);

            mSocket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
            mSocket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, 1);
            NetLog.Log("Default: ReceiveBufferSize: " + mSocket.ReceiveBufferSize);
            mSocket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReceiveBuffer, mClientPeer.GetConfig().client_socket_receiveBufferSize);
            NetLog.Log("Fix ReceiveBufferSize: " + mSocket.ReceiveBufferSize);

            ReceiveArgs = new SocketAsyncEventArgs();
            ReceiveArgs.SetBuffer(new byte[Config.nUdpPackageFixedSize], 0, Config.nUdpPackageFixedSize);
            ReceiveArgs.Completed += ProcessReceive;

            SendArgs = new SocketAsyncEventArgs();
            SendArgs.SetBuffer(new byte[Config.nUdpPackageFixedSize], 0, Config.nUdpPackageFixedSize);
            SendArgs.Completed += ProcessSend;

            bReceiveIOContexUsed = false;
            bSendIOContexUsed = false;

            if (Config.bUseSendStream)
            {
                mSendStreamList = new AkCircularSpanBuffer<byte>();
            }
            else
            {
                mSendPackageQueue = new ConcurrentQueue<NetUdpFixedSizePackage>();
            }
        }

        public void ConnectServer(string ip, int nPort)
        {
            this.port = nPort;
            this.ip = ip;
            remoteEndPoint = new IPEndPoint(IPAddress.Parse(ip), port);
            ReceiveArgs.RemoteEndPoint = remoteEndPoint;
            SendArgs.RemoteEndPoint = remoteEndPoint;

            ConnectServer();
            StartReceiveEventArg();
        }

        public void ConnectServer()
        {
            mClientPeer.mUDPLikeTCPMgr.SendConnect();
        }

        public void ReConnectServer()
        {
            mClientPeer.mUDPLikeTCPMgr.SendConnect();
        }

        public IPEndPoint GetIPEndPoint()
        {
            return remoteEndPoint;
        }

        public bool DisConnectServer()
        {
            var mSocketPeerState = mClientPeer.GetSocketState();
            if (mSocketPeerState == SOCKET_PEER_STATE.CONNECTED || mSocketPeerState == SOCKET_PEER_STATE.CONNECTING)
            {
                mClientPeer.mUDPLikeTCPMgr.SendDisConnect();
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
                        DisConnectedWithException(e);
                    }
                }
                else
                {
                    bReceiveIOContexUsed = false;
                }
            }

            UdpStatistical.AddReceiveIOCount(bIOSyncCompleted);
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
                        DisConnectedWithException(e);
                    }
                }
                else
                {
                    bSendIOContexUsed = false;
                }
            }

            UdpStatistical.AddSendIOCount(bIOSyncCompleted);
            if (bIOSyncCompleted)
            {
                ProcessSend(null, SendArgs);
            }
        }

        private void ProcessReceive(object sender, SocketAsyncEventArgs e)
        {
            if (e.SocketError == SocketError.Success && e.BytesTransferred > 0)
            {
                mClientPeer.mUdpPackageMainThreadMgr.MultiThreadingReceiveNetPackage(e);
            }
            
            StartReceiveEventArg();
        }

        private void ProcessSend(object sender, SocketAsyncEventArgs e)
        {
            if (e.SocketError == SocketError.Success)
            {
                if (Config.bUseSendStream)
                {
                    SendNetStream2();
                }
                else
                {
                    SendNetPackage2();
                }
            }
            else
            {
                bSendIOContexUsed = false;
                DisConnectedWithSocketError(e.SocketError);
            }
        }

        public void SendNetPackage(NetUdpFixedSizePackage mPackage)
        {
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
                mSocket.SendTo(mPackage.buffer, 0, mPackage.Length, SocketFlags.None, remoteEndPoint);
                if (!Config.bUseSendStream)
                {
                    mClientPeer.GetObjectPoolManager().NetUdpFixedSizePackage_Recycle(mPackage);
                }
            }
        }

        private void SendNetPackage2()
        {
            NetUdpFixedSizePackage mPackage = null;
            if (mSendPackageQueue.TryDequeue(out mPackage))
            {
                int nSendBytesCount = 0;
                Buffer.BlockCopy(mPackage.buffer, 0, SendArgs.Buffer, nSendBytesCount, mPackage.Length);
                nSendBytesCount += mPackage.Length;
                mClientPeer.GetObjectPoolManager().NetUdpFixedSizePackage_Recycle(mPackage);

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
                                mClientPeer.GetObjectPoolManager().NetUdpFixedSizePackage_Recycle(mPackage);
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

                SendArgs.SetBuffer(0, nSendBytesCount);
                StartSendEventArg();
            }
            else
            {
                bSendIOContexUsed = false;
            }
        }

        private void SendNetStream2()
        {
            int CurrentSegmentLength = mSendStreamList.CurrentSegmentLength;
            if (CurrentSegmentLength > 0)
            {
                int nSendBytesCount = 0;
                if (Config.bSocketSendMultiPackage)
                {
                    while (CurrentSegmentLength > 0)
                    {
                        if (CurrentSegmentLength + nSendBytesCount <= SendArgs.Buffer.Length)
                        {
                            lock (mSendStreamList)
                            {
                                mSendStreamList.WriteTo(SendArgs.Buffer.AsSpan().Slice(nSendBytesCount));
                            }
                            nSendBytesCount += CurrentSegmentLength;
                            CurrentSegmentLength = mSendStreamList.CurrentSegmentLength;
                        }
                        else
                        {
                            break;
                        }
                    }
                }
                else
                {
                    if (CurrentSegmentLength > 0)
                    {
                        lock (mSendStreamList)
                        {
                            mSendStreamList.WriteTo(SendArgs.Buffer.AsSpan().Slice(nSendBytesCount));
                        }
                        nSendBytesCount += CurrentSegmentLength;
                    }
                }

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
            mClientPeer.SetSocketState(SOCKET_PEER_STATE.DISCONNECTED);
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
            var mSocketPeerState = mClientPeer.GetSocketState();
            if (mSocketPeerState == SOCKET_PEER_STATE.DISCONNECTING)
            {
                mClientPeer.SetSocketState(SOCKET_PEER_STATE.DISCONNECTED);
            }
            else if (mSocketPeerState == SOCKET_PEER_STATE.CONNECTED || mSocketPeerState == SOCKET_PEER_STATE.CONNECTING)
            {
                mClientPeer.SetSocketState(SOCKET_PEER_STATE.RECONNECTING);
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
            if (Config.bUseSendStream)
            {
                lock (mSendStreamList)
                {
                    mSendStreamList.reset();
                }
            }
            else
            {
                NetUdpFixedSizePackage mPackage = null;
                while (mSendPackageQueue.TryDequeue(out mPackage))
                {
                    mClientPeer.GetObjectPoolManager().NetUdpFixedSizePackage_Recycle(mPackage);
                }
            }
        }

        public void Release()
        {
            DisConnectServer();
            CloseSocket();
            NetLog.Log("--------------- Client Release ----------------");
        }
    }
}









