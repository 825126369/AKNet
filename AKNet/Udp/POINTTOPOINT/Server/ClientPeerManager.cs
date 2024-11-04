/************************************Copyright*****************************************
*        ProjectName:AKNet
*        Web:https://github.com/825126369/AKNet
*        Description:这是一个面向 .Net Standard 2.1 的游戏网络库
*        Author:阿珂
*        CreateTime:2024/11/4 20:04:54
*        Copyright:MIT软件许可证
************************************Copyright*****************************************/
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Net;
using AKNet.Common;
using AKNet.Udp.POINTTOPOINT.Common;

namespace AKNet.Udp.POINTTOPOINT.Server
{
    internal class ClientPeerManager
	{
        public readonly ClientPeerPool mClientPeerPool = null;
        private readonly Dictionary<string, ClientPeer> mClientDic = new Dictionary<string, ClientPeer>();
        private readonly List<string> mRemovePeerList = new List<string>();
        private readonly ConcurrentQueue<NetUdpFixedSizePackage> mPackageQueue = new ConcurrentQueue<NetUdpFixedSizePackage>();
        private UdpServer mNetServer = null;
        private DisConnectSendMgr mDisConnectSendMgr = null;

        public ClientPeerManager(UdpServer mNetServer)
        {
            this.mNetServer = mNetServer;
            mClientPeerPool = new ClientPeerPool(mNetServer, Config.numConnections);
            mDisConnectSendMgr = new DisConnectSendMgr(mNetServer);
        }

		public void Update(double elapsed)
		{
            NetUdpFixedSizePackage mPackage = null;
			while (mPackageQueue.TryDequeue(out mPackage))
			{
                PackageStatistical.AddReceivePackageCount();
                AddClient_And_ReceiveNetPackage(mPackage);
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

        public void MultiThreadingReceiveNetPackage(NetUdpFixedSizePackage mPackage)
        {
            NetLog.Assert(mPackage.remoteEndPoint != null, "endPoint == nul");
            bool bSucccess = NetPackageEncryption.DeEncryption(mPackage);
            if (bSucccess)
            {
                mPackageQueue.Enqueue(mPackage);
            }
            else
            {
                mNetServer.GetObjectPoolManager().NetUdpFixedSizePackage_Recycle(mPackage);
                NetLog.LogError("解码失败 !!!");
            }
        }

        private void AddClient_And_ReceiveNetPackage(NetUdpFixedSizePackage mPackage)
        {
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
                    mClientPeer = mClientPeerPool.Pop();
                    if (mClientPeer != null)
                    {
                        mClientDic.Add(nPeerId, mClientPeer);
                        mClientPeer.BindEndPoint(endPoint);
                        mClientPeer.SetName(nPeerId);
                        PrintAddClientMsg(mClientPeer);
                    }
                    else
                    {
#if DEBUG
                        NetLog.Log($"服务器爆满, 客户端总数: {mClientDic.Count}");
#endif
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