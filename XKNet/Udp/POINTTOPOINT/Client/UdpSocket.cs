using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using XKNet.Common;
using XKNet.Udp.POINTTOPOINT.Common;

namespace XKNet.Udp.POINTTOPOINT.Client
{
    internal class SocketUdp
    {
        protected Socket mSocket = null;
        private SocketAsyncEventArgs ReceiveArgs;
        private SocketAsyncEventArgs SendArgs;
        private EndPoint remoteEndPoint = null;

        internal string ip;
        internal UInt16 port;

        private object lock_mSocket_object = new object();

        bool bReceiveIOContexUsed = false;
        bool bSendIOContexUsed = false;

        ClientPeer mClientPeer;

        public SocketUdp(ClientPeer mClientPeer)
        {
            this.mClientPeer = mClientPeer;
            mClientPeer.SetSocketState(SOCKET_PEER_STATE.NONE);

            mSocket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
            mSocket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, 1);

            ReceiveArgs = new SocketAsyncEventArgs();
            ReceiveArgs.SetBuffer(new byte[Config.nUdpPackageFixedSize], 0, Config.nUdpPackageFixedSize);
            ReceiveArgs.Completed += IO_Completed;

            SendArgs = new SocketAsyncEventArgs();
            SendArgs.SetBuffer(new byte[Config.nUdpPackageFixedSize], 0, Config.nUdpPackageFixedSize);
            SendArgs.Completed += IO_Completed;

            bReceiveIOContexUsed = false;
            bSendIOContexUsed = false;
        }

        public void ConnectServer(string ip, UInt16 nPort)
        {
            this.port = nPort;
            this.ip = ip;
            remoteEndPoint = new IPEndPoint(IPAddress.Parse(ip), port);
            ReceiveArgs.RemoteEndPoint = remoteEndPoint;
            SendArgs.RemoteEndPoint = remoteEndPoint;
            
            mClientPeer.mUDPLikeTCPMgr.SendConnect();
            StartReceiveFromAsync();
        }

        public void ReConnectServer()
        {
            if (mSocket != null && mSocket.Connected)
            {
                mClientPeer.SetSocketState(SOCKET_PEER_STATE.CONNECTED);
            }
            else
            {
                ConnectServer(this.ip, this.port);
            }
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

        private void StartReceiveFromAsync()
        {
            if (!bReceiveIOContexUsed)
            {
                bReceiveIOContexUsed = true;
                if (!mSocket.ReceiveFromAsync(ReceiveArgs))
                {
                    ProcessReceive(null, ReceiveArgs);
                }
            }
        }

        void IO_Completed(object sender, SocketAsyncEventArgs e)
        {
            switch (e.LastOperation)
            {
                case SocketAsyncOperation.ReceiveFrom:
                    ProcessReceive(sender, e);
                    break;
                case SocketAsyncOperation.SendTo:
                    ProcessSend(sender, e);
                    break;
                default:
                    NetLog.Log(e.LastOperation.ToString());
                    break;
            }
        }

        private void ProcessReceive(object sender, SocketAsyncEventArgs e)
        {
            if (e.SocketError == SocketError.Success)
            {
                if (e.BytesTransferred > 0)
                {
                    NetUdpFixedSizePackage mPackage = ObjectPoolManager.Instance.mUdpFixedSizePackagePool.Pop();
                    mPackage.CopyFrom(e);
                    mClientPeer.mMsgReceiveMgr.MultiThreadingReceiveNetPackage(mPackage);

                    lock (lock_mSocket_object)
                    {
                        if (mSocket != null)
                        {
                            if (!mSocket.ReceiveFromAsync(e))
                            {
                                ProcessReceive(sender, e);
                            }
                        }
                    }
                }
                else
                {
                    bReceiveIOContexUsed = false;
                    NetLog.LogError($"{e.SocketError} : {e.BytesTransferred}");
                }
            }
            else
            {
                NetLog.LogError(e.SocketError);
                bReceiveIOContexUsed = false;
                DisConnectedWithException(e.SocketError);
            }
        }

        private void ProcessSend(object sender, SocketAsyncEventArgs e)
        {
            if (e.SocketError == SocketError.Success)
            {
                SendNetPackage2(e);
            }
            else
            {
                bSendIOContexUsed = false;
                DisConnectedWithException(e.SocketError);
            }
        }

        readonly ConcurrentQueue<NetUdpFixedSizePackage> mSendPackageQueue = new ConcurrentQueue<NetUdpFixedSizePackage>();
        readonly object bSendIOContexUsedObj = new object();
        public void SendNetPackage(NetUdpFixedSizePackage mPackage)
        {
            var mPackage2 = ObjectPoolManager.Instance.mUdpFixedSizePackagePool.Pop();
            mPackage2.CopyFrom(mPackage);
            mSendPackageQueue.Enqueue(mPackage2);

            bool bCanGoNext = false;
            lock (bSendIOContexUsedObj)
            {
                bCanGoNext = bSendIOContexUsed == false;
                if (!bSendIOContexUsed)
                {
                    bSendIOContexUsed = true;
                }
            }

            if (bCanGoNext)
            {
                SendNetPackage2(SendArgs);
            }
        }

        private void SendNetPackage2(SocketAsyncEventArgs e)
        {
            NetUdpFixedSizePackage mPackage = null;
            if (mSendPackageQueue.TryDequeue(out mPackage))
            {
                Array.Copy(mPackage.buffer, e.Buffer, mPackage.Length);
                e.SetBuffer(0, mPackage.Length);
                ObjectPoolManager.Instance.mUdpFixedSizePackagePool.recycle(mPackage);

                lock (lock_mSocket_object)
                {
                    if (mSocket != null)
                    {
                        if (!mSocket.SendToAsync(e))
                        {
                            IO_Completed(null, e);
                        }
                    }
                    else
                    {
                        bSendIOContexUsed = false;
                    }
                }
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

        private void DisConnectedWithException(SocketError e)
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

        public void Release()
        {
            DisConnectServer();
            CloseSocket();
            NetLog.Log("--------------- Client Release ----------------");
        }
    }
}









