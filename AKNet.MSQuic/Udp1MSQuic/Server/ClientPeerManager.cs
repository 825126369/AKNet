/************************************Copyright*****************************************
*        ProjectName:AKNet
*        Web:https://github.com/825126369/AKNet
*        Description:C#游戏网络库
*        Author:许珂
*        StartTime:2024/11/01 00:00:00
*        ModifyTime:2025/11/14 8:56:49
*        Copyright:MIT软件许可证
************************************Copyright*****************************************/
using AKNet.Common;
using AKNet.Udp1MSQuic.Common;
using System.Collections.Generic;

namespace AKNet.Udp1MSQuic.Server
{
    internal class ClientPeerManager
	{
		private readonly List<ClientPeer> mClientList = new List<ClientPeer>(0);
		private readonly Queue<QuicConnection> mConnectSocketQueue = new Queue<QuicConnection>();
		private QuicServer mNetServer;

		public ClientPeerManager(QuicServer mNetServer)
		{
			this.mNetServer = mNetServer;
		}

		public void Update(double elapsed)
		{
			while (CreateClientPeer())
			{
				
			}

			for (int i = mClientList.Count - 1; i >= 0; i--)
			{
				var mClientPeer = mClientList[i];
				if (mClientPeer.GetSocketState() == SOCKET_PEER_STATE.CONNECTED)
				{
					mClientPeer.Update(elapsed);
				}
				else
				{
					mClientList.RemoveAt(i);
                    PrintRemoveClientMsg(mClientPeer);
                    mClientPeer.Reset();

                }
			}
		}

		public bool MultiThreadingHandleConnectedSocket(QuicConnection connection)
		{
			int nNowConnectCount = mClientList.Count + mConnectSocketQueue.Count;
			if (nNowConnectCount >= mNetServer.mConfig.MaxPlayerCount)
			{
#if DEBUG
				NetLog.Log($"服务器爆满, 客户端总数: {nNowConnectCount}");
#endif
				return false;
			}
			else
			{
				lock (mConnectSocketQueue)
				{
					mConnectSocketQueue.Enqueue(connection);
				}
				return true;
			}
		}

		private bool CreateClientPeer()
		{
			QuicConnection connection = null;
            lock (mConnectSocketQueue)
			{
				mConnectSocketQueue.TryDequeue(out connection);
			}

			if (connection != null)
			{
				var clientPeer = new ClientPeer(mNetServer);
				clientPeer.HandleConnectedSocket(connection);
				mClientList.Add(clientPeer);
                PrintAddClientMsg(clientPeer);
				return true;
			}
			return false;
		}

        private void PrintAddClientMsg(ClientPeer clientPeer)
		{
#if DEBUG
            var mRemoteEndPoint = clientPeer.GetIPEndPoint();
			if (mRemoteEndPoint != null)
			{
				NetLog.Log($"增加客户端: {mRemoteEndPoint}, 客户端总数: {mClientList.Count}");
			}
			else
			{
                NetLog.Log($"增加客户端, 客户端总数: {mClientList.Count}");
            }
#endif
        }

        private void PrintRemoveClientMsg(ClientPeer clientPeer)
		{
#if DEBUG
			var mRemoteEndPoint = clientPeer.GetIPEndPoint();
			if (mRemoteEndPoint != null)
			{
				NetLog.Log($"移除客户端: {mRemoteEndPoint}, 客户端总数: {mClientList.Count}");
			}
			else
			{
                NetLog.Log($"移除客户端, 客户端总数: {mClientList.Count}");
            }
#endif
		}
	}

}