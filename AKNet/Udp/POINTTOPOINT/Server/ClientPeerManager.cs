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
        public readonly ClientPeerPool mClientPeerPool = null;
        private readonly Dictionary<string, ClientPeer> mClientDic = new Dictionary<string, ClientPeer>();
        private readonly List<string> mRemovePeerList = new List<string>();
        private readonly Queue<NetUdpFixedSizePackage> mPackageQueue = new Queue<NetUdpFixedSizePackage>();
        private readonly DisConnectSendMgr mDisConnectSendMgr = null;
        private UdpServer mNetServer = null;

        public ClientPeerManager(UdpServer mNetServer)
        {
            this.mNetServer = mNetServer;
            mClientPeerPool = new ClientPeerPool(mNetServer, 0, mNetServer.GetConfig().MaxPlayerCount);
            mDisConnectSendMgr = new DisConnectSendMgr(mNetServer);
        }

		public void Update(double elapsed)
		{
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

			foreach(var v in mRemovePeerList)
			{
				ClientPeer mClientPeer = mClientDic[v];
				mClientDic.Remove(v);
                PrintRemoveClientMsg(mClientPeer);

                mClientPeer.Reset();
                mClientPeerPool.recycle(mClientPeer);
			}
            mRemovePeerList.Clear();
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

                        lock (mPackageQueue)
                        {
                            mPackageQueue.Enqueue(mPackage);
                        }

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
                        mClientPeer = mClientPeerPool.Pop();
                        if (mClientPeer != null)
                        {
                            mClientDic.Add(nPeerId, mClientPeer);
                            mClientPeer.BindEndPoint(endPoint);
                            mClientPeer.SetName(nPeerId);
                            PrintAddClientMsg(mClientPeer);
                        }
                    }
                }
            }

            if (mClientPeer != null)
            {
                mClientPeer.mMsgReceiveMgr.ReceiveWaitCheckNetPackage(mPackage);
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