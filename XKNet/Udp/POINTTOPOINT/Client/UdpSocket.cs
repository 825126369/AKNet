using System;
using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
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
            if (e.SocketError == SocketError.Success && e.BytesTransferred > 0)
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

            }
            else
            {
                DisConnectedWithException(e.SocketError);
            }

            bSendIOContexUsed = false;
        }

        byte[] mSendBuff = new byte[Config.nUdpPackageFixedSize];

        internal void SendNetPackage(NetUdpFixedSizePackage mPackage)
        {
            lock (lock_mSocket_object)
            {
                int nPackageLength = mPackage.Length;
                Array.Copy(mPackage.buffer, 0, mSendBuff, 0, nPackageLength);

                if (mSocket != null)
                {
                    try
                    {
                        NetLog.Assert(mPackage.Length >= Config.nUdpPackageFixedHeadSize, mPackage.Length);
                        int nSendLength = mSocket.SendTo(mSendBuff, 0, nPackageLength, SocketFlags.None, remoteEndPoint);
                        NetLog.Assert(nSendLength == nPackageLength, $"{nSendLength} | {nPackageLength}");
                    }
                    catch (SocketException e)
                    {
                        NetLog.LogError(e.ToString());
                        DisConnectedWithException(e.SocketErrorCode);
                    }
                    catch (Exception e)
                    {
                        NetLog.LogError(e.ToString());
                    }
                }
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









