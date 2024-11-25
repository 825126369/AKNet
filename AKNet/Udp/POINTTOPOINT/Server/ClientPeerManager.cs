/************************************Copyright*****************************************
*        ProjectName:AKNet
*        Web:https://github.com/825126369/AKNet
*        Description:这是一个面向 .Net Standard 2.1 的游戏网络库
*        Author:阿珂
*        CreateTime:2024/11/23 22:12:37
*        Copyright:MIT软件许可证
************************************Copyright*****************************************/
using AKNet.Common;
using AKNet.Udp.POINTTOPOINT.Common;
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;

namespace AKNet.Udp.POINTTOPOINT.Server
{
    internal class ClientPeerManager
    {
        private readonly Dictionary<string, ClientPeer> mClientDic = new Dictionary<string, ClientPeer>();
        private readonly List<string> mRemovePeerList = new List<string>();
        private readonly Queue<NetUdpFixedSizePackage> mPackageQueue = new Queue<NetUdpFixedSizePackage>();
        private readonly DisConnectSendMgr mDisConnectSendMgr = null;
        private UdpServer mNetServer = null;

        private readonly Dictionary<string, FakeSocket> mAcceptSocketDic = new Dictionary<string, FakeSocket>();
        private readonly FakeSocketPool mFakeSocketPool = null;

        public ClientPeerManager(UdpServer mNetServer)
        {
            this.mNetServer = mNetServer;
            mDisConnectSendMgr = new DisConnectSendMgr(mNetServer);
            mFakeSocketPool = new FakeSocketPool(mNetServer);
        }

        public void MultiThreadingReceiveNetPackage(SocketAsyncEventArgs e)
        {
            if (Config.bSocketSendMultiPackage)
            {
                var mBuff = e.Buffer.AsSpan().Slice(e.Offset, e.BytesTransferred);
                while (true)
                {
                    var mPackage = mNetServer.GetObjectPoolManager().NetUdpFixedSizePackage_Pop();
                    mPackage.remoteEndPoint = e.RemoteEndPoint;
                    bool bSucccess = mNetServer.GetCryptoMgr().Decode(mBuff, mPackage);
                    if (bSucccess)
                    {
                        int nReadBytesCount = mPackage.Length;
                        MultiThreading_HandleSinglePackage(mPackage);
                        if (mBuff.Length > nReadBytesCount)
                        {
                            mBuff = mBuff.Slice(nReadBytesCount);
                        }
                        else
                        {
                            break;
                        }
                    }
                    else
                    {
                        mNetServer.GetObjectPoolManager().NetUdpFixedSizePackage_Recycle(mPackage);
                        NetLog.LogError("解码失败 !!!");
                        break;
                    }
                }
            }
            else
            {
                NetUdpFixedSizePackage mPackage = mNetServer.GetObjectPoolManager().NetUdpFixedSizePackage_Pop();
                Buffer.BlockCopy(e.Buffer, e.Offset, mPackage.buffer, 0, e.BytesTransferred);
                mPackage.Length = e.BytesTransferred;
                mPackage.remoteEndPoint = e.RemoteEndPoint;
                bool bSucccess = mNetServer.GetCryptoMgr().Decode(mPackage);
                if (bSucccess)
                {
                    lock (mPackageQueue)
                    {
                        mPackageQueue.Enqueue(mPackage);
                    }
                }
                else
                {
                    mNetServer.GetObjectPoolManager().NetUdpFixedSizePackage_Recycle(mPackage);
                    NetLog.LogError("解码失败 !!!");
                }
            }
        }

        private void MultiThreading_HandleSinglePackage(NetUdpFixedSizePackage mPackage)
        {
            if (Config.bUseClientPeerManager2)
            {
                MultiThreading_AddClient_And_ReceiveNetPackage(mPackage);
            }
            else
            {
                lock (mPackageQueue)
                {
                    mPackageQueue.Enqueue(mPackage);
                }
            }
        }

        public void Update(double elapsed)
        {
            if (Config.bUseClientPeerManager2) return;

            //网络流量大的时候，会卡在这，一直while循环
            while (NetPackageExecute())
            {

            }

            foreach (var v in mClientDic)
            {
                ClientPeer clientPeer = v.Value;
                clientPeer.Update(elapsed);
                if (clientPeer.GetSocketState() == SOCKET_PEER_STATE.DISCONNECTED)
                {
                    mRemovePeerList.Add(v.Key);
                }
            }

            foreach (var v in mRemovePeerList)
            {
                ClientPeer mClientPeer = mClientDic[v];
                mClientDic.Remove(v);
                PrintRemoveClientMsg(mClientPeer);

                mClientPeer.Reset();
                mNetServer.GetClientPeerPool().recycle(mClientPeer);
            }
            mRemovePeerList.Clear();
        }

        private bool NetPackageExecute()
        {
            NetUdpFixedSizePackage mPackage = null;
            lock (mPackageQueue)
            {
                mPackageQueue.TryDequeue(out mPackage);
            }

            if (mPackage != null)
            {
                UdpStatistical.AddReceivePackageCount();
                AddClient_And_ReceiveNetPackage(mPackage);
                return true;
            }

            return false;
        }
        
        private void AddClient_And_ReceiveNetPackage(NetUdpFixedSizePackage mPackage)
        {
            MainThreadCheck.Check();
            EndPoint endPoint = mPackage.remoteEndPoint;

            ClientPeer mClientPeer = null;
            string nPeerId = endPoint.ToString();
            if (!mClientDic.TryGetValue(nPeerId, out mClientPeer))
            {
                if (mPackage.nPackageId == UdpNetCommand.COMMAND_DISCONNECT)
                {
                    mDisConnectSendMgr.SendInnerNetData(endPoint);
                }
                else if (mPackage.nPackageId == UdpNetCommand.COMMAND_CONNECT)
                {
                    if (mClientDic.Count >= mNetServer.GetConfig().MaxPlayerCount)
                    {
#if DEBUG
                        NetLog.Log($"服务器爆满, 客户端总数: {mClientDic.Count}");
#endif
                    }
                    else
                    {
                        mClientPeer = mNetServer.GetClientPeerPool().Pop();
                        if (mClientPeer != null)
                        {
                            mClientDic.Add(nPeerId, mClientPeer);
                            FakeSocket mSocket = new FakeSocket(mNetServer);
                            mSocket.RemoteEndPoint = endPoint as IPEndPoint;
                            mClientPeer.HandleConnectedSocket(mSocket);
                            PrintAddClientMsg(mClientPeer);
                        }
                    }
                }
            }

            if (mClientPeer != null)
            {
                mClientPeer.mUdpCheckPool.ReceiveNetPackage(mPackage);
            }
            else
            {
                mNetServer.GetObjectPoolManager().NetUdpFixedSizePackage_Recycle(mPackage);
            }
        }

        private void MultiThreading_AddClient_And_ReceiveNetPackage(NetUdpFixedSizePackage mPackage)
        {
            MainThreadCheck.Check();
            EndPoint endPoint = mPackage.remoteEndPoint;

            FakeSocket mFakeSocket = null;
            string nPeerId = endPoint.ToString();
            if (!mAcceptSocketDic.TryGetValue(nPeerId, out mFakeSocket))
            {
                if (mPackage.nPackageId == UdpNetCommand.COMMAND_DISCONNECT)
                {
                    mDisConnectSendMgr.SendInnerNetData(endPoint);
                }
                else if (mPackage.nPackageId == UdpNetCommand.COMMAND_CONNECT)
                {
                    if (mAcceptSocketDic.Count >= mNetServer.GetConfig().MaxPlayerCount)
                    {
#if DEBUG
                        NetLog.Log($"服务器爆满, 客户端总数: {mAcceptSocketDic.Count}");
#endif
                    }
                    else
                    {
                        mFakeSocket = mFakeSocketPool.Pop();
                        mFakeSocket.RemoteEndPoint = endPoint as IPEndPoint;
                        mNetServer.GetClientPeerManager2().MultiThreadingHandleConnectedSocket(mFakeSocket);
                    }
                }
            }

            if (mFakeSocket != null)
            {
                mFakeSocket.WriteFrom(mPackage);
            }
            else
            {
                mNetServer.GetObjectPoolManager().NetUdpFixedSizePackage_Recycle(mPackage);
            }
        }

        private void PrintAddClientMsg(ClientPeer clientPeer)
        {
#if DEBUG
            var mRemoteEndPoint = clientPeer.GetIPEndPoint();
            if (mRemoteEndPoint != null)
            {
                NetLog.Log($"增加客户端: {mRemoteEndPoint}, 客户端总数: {mClientDic.Count}");
            }
            else
            {
                NetLog.Log($"增加客户端, 客户端总数: {mClientDic.Count}");
            }
#endif
        }

        private void PrintRemoveClientMsg(ClientPeer clientPeer)
        {
#if DEBUG
            var mRemoteEndPoint = clientPeer.GetIPEndPoint();
            if (mRemoteEndPoint != null)
            {
                NetLog.Log($"移除客户端: {mRemoteEndPoint}, 客户端总数: {mClientDic.Count}");
            }
            else
            {
                NetLog.Log($"移除客户端, 客户端总数: {mClientDic.Count}");
            }
#endif
        }
    }
}