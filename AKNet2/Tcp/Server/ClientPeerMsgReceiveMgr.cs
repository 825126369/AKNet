/************************************Copyright*****************************************
*        ProjectName:AKNet
*        Web:https://github.com/825126369/AKNet
*        Description:这是一个面向 .Net Standard 2.1 的游戏网络库
*        Author:阿珂
*        ModifyTime:2025/2/27 22:28:11
*        Copyright:MIT软件许可证
************************************Copyright*****************************************/
using AKNet.Common;
using AKNet.Tcp.Common;
using System.Net.Sockets;

namespace AKNet.Tcp.Server
{
    internal class ClientPeerMsgReceiveMgr
	{
		private readonly NetStreamCircularBuffer mReceiveStreamList = new NetStreamCircularBuffer();
		private readonly object lock_mReceiveStreamList_object = new object();
		private ClientPeerPrivate mClientPeer;
		private TcpServer mTcpServer;

        public ClientPeerMsgReceiveMgr(ClientPeerPrivate mClientPeer, TcpServer mTcpServer)
		{
			this.mTcpServer = mTcpServer;
			this.mClientPeer = mClientPeer;
		}

		public void Update(double elapsed)
		{
			switch (mClientPeer.GetSocketState())
			{
				case SOCKET_PEER_STATE.CONNECTED:
					int nPackageCount = 0;

					while (NetPackageExecute())
					{
						nPackageCount++;
					}

					if (nPackageCount > 0)
					{
						mClientPeer.ReceiveHeartBeat();
					}

					//if (nPackageCount > 100)
					//{
					//	NetLog.LogWarning("Server ClientPeer 处理逻辑包的数量： " + nPackageCount);
					//}

					break;
				default:
					break;
			}
		}
		
        public void MultiThreadingReceiveSocketStream(SocketAsyncEventArgs e)
		{
			lock (lock_mReceiveStreamList_object)
			{
                mReceiveStreamList.WriteFrom(e.MemoryBuffer.Span.Slice(e.Offset, e.BytesTransferred));
			}
		}

		private bool NetPackageExecute()
		{
			TcpNetPackage mNetPackage = mTcpServer.mNetPackage;
			bool bSuccess = false;
			lock (lock_mReceiveStreamList_object)
			{
				bSuccess = mTcpServer.mCryptoMgr.Decode(mReceiveStreamList, mNetPackage);
			}

			if (bSuccess)
			{
				if (TcpNetCommand.orInnerCommand(mNetPackage.nPackageId))
				{

				}
				else
				{
					mTcpServer.mPackageManager.NetPackageExecute(this.mClientPeer, mNetPackage);
				}
			}

			return bSuccess;
		}

		public void Reset()
		{
			lock (mReceiveStreamList)
			{
				mReceiveStreamList.Reset();
			}
		}

		public void Release()
		{
            mReceiveStreamList.Dispose();
        }

	}
}
