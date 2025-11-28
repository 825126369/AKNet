/************************************Copyright*****************************************
*        ProjectName:AKNet
*        Web:https://github.com/825126369/AKNet
*        Description:C#游戏网络库
*        Author:许珂
*        StartTime:2024/11/01 00:00:00
*        ModifyTime:2025/11/29 4:33:46
*        Copyright:MIT软件许可证
************************************Copyright*****************************************/
using System;
using System.Net.Sockets;
using AKNet.Common;
using AKNet.WebSocket.Common;

namespace AKNet.WebSocket.Server
{
    internal class MsgReceiveMgr
	{
		private readonly NetStreamCircularBuffer mReceiveStreamList = new NetStreamCircularBuffer();
		private readonly object lock_mReceiveStreamList_object = new object();
		private ClientPeer mClientPeer;
		private TcpServer mTcpServer;
        public MsgReceiveMgr(ClientPeer mClientPeer, TcpServer mTcpServer)
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
			NetStreamPackage mNetPackage = mTcpServer.mNetPackage;
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

	}
}
