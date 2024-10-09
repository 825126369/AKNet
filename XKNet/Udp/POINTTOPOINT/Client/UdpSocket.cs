using System;
using System.Net;
using System.Net.Sockets;
using XKNet.Common;
using XKNet.Udp.Common;

namespace XKNet.Udp.Client
{
    public class SocketUdp : SocketReceivePeer
    {
        protected Socket mSocket = null;
        private SocketAsyncEventArgs ReceiveArgs;
        private EndPoint remoteEndPoint = null;

        protected string ip;
        protected UInt16 port;

        protected CLIENT_SOCKET_PEER_STATE mSocketPeerState;

        private object lock_mSocket_object = new object();

        bool bReceiveIOContexUsed = false;
        bool bSendIOContexUsed = false;

        public SocketUdp()
        {
            mSocketPeerState = CLIENT_SOCKET_PEER_STATE.NONE;
            mSocket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
            mSocket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, 1);

            ReceiveArgs = new SocketAsyncEventArgs();
            ReceiveArgs.SetBuffer(new byte[Config.nUdpPackageFixedSize], 0, Config.nUdpPackageFixedSize);
            ReceiveArgs.Completed += IO_Completed;
            bReceiveIOContexUsed = false;
            bSendIOContexUsed = false;
        }

        public void ConnectServer(string ip, UInt16 ServerPort)
        {
            this.port = ServerPort;
            this.ip = ip;
            remoteEndPoint = new IPEndPoint(IPAddress.Parse(ip), port);
            ReceiveArgs.RemoteEndPoint = remoteEndPoint;

            UDPLikeTCPPeer tcpPeer = this as UDPLikeTCPPeer;
            tcpPeer.SendConnect();

            StartReceiveFromAsync();
        }

        public void DisConnectServer()
        {
            if (mSocketPeerState == CLIENT_SOCKET_PEER_STATE.CONNECTED || mSocketPeerState == CLIENT_SOCKET_PEER_STATE.CONNECTING)
            {
                UDPLikeTCPPeer tcpPeer = this as UDPLikeTCPPeer;
                tcpPeer.SendDisConnect();
            }
        }

        private void StartReceiveFromAsync()
        {
            if (!bReceiveIOContexUsed)
            {
                bReceiveIOContexUsed = true;
                while (!mSocket.ReceiveFromAsync(ReceiveArgs))
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
                    int length = e.BytesTransferred;
                    NetUdpFixedSizePackage mReceiveStream = ObjectPoolManager.Instance.mUdpFixedSizePackagePool.Pop();
                    Array.Copy(e.Buffer, 0, mReceiveStream.buffer, 0, length);
                    mReceiveStream.Length = length;
                    ReceiveNetPackage(mReceiveStream);

                    lock (lock_mSocket_object)
                    {
                        if (mSocket != null)
                        {
                            while (!mSocket.ReceiveFromAsync(e))
                            {
                                ProcessReceive(sender, e);
                            }
                        }
                    }
                }
                else
                {
                    bReceiveIOContexUsed = false;
                    DisConnectedWithNormal();
                }
            }
            else
            {
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
        }

        internal void SendNetPackage(NetUdpFixedSizePackage mPackage)
        {
            lock (lock_mSocket_object)
            {
                if (mSocket != null)
                {
                    try
                    {
                        NetLog.Assert(mPackage.Length >= Config.nUdpPackageFixedHeadSize, mPackage.Length);
                        int nSendLength = mSocket.SendTo(mPackage.buffer, 0, mPackage.Length, SocketFlags.None, remoteEndPoint);
                        NetLog.Assert(nSendLength == mPackage.Length);
                    }
                    catch (SocketException e)
                    {
                        DisConnectedWithException(e.SocketErrorCode);
                    }
                }
            }

            if (!UdpNetCommand.orNeedCheck(mPackage.nPackageId))
            {
                ObjectPoolManager.Instance.mUdpFixedSizePackagePool.recycle(mPackage);
            }
        }

        protected void DisConnectedWithNormal()
        {
            NetLog.Log("客户端 正常 断开服务器 ");
            Reset();
            mSocketPeerState = CLIENT_SOCKET_PEER_STATE.DISCONNECTED;
        }

        private void DisConnectedWithException(SocketError e)
        {
            NetLog.Log("客户端 异常 断开服务器: " + e.ToString());
            Reset();
            if (mSocketPeerState == CLIENT_SOCKET_PEER_STATE.DISCONNECTING)
            {
                mSocketPeerState = CLIENT_SOCKET_PEER_STATE.DISCONNECTED;
            }
            else if (mSocketPeerState == CLIENT_SOCKET_PEER_STATE.CONNECTED)
            {
                mSocketPeerState = CLIENT_SOCKET_PEER_STATE.CONNECTING;
            }
        }

        private void CloseSocket()
        {
            mSocketPeerState = CLIENT_SOCKET_PEER_STATE.DISCONNECTED;

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

        public override void Release()
        {
            DisConnectServer();

            base.Release();

            CloseSocket();

            NetLog.Log("--------------- Client Release ----------------");
        }
    }
}









