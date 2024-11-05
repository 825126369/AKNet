/************************************Copyright*****************************************
*        ProjectName:AKNet
*        Web:https://github.com/825126369/AKNet
*        Description:这是一个面向 .Net Standard 2.1 的游戏网络库
*        Author:阿珂
*        CreateTime:2024/11/4 20:04:54
*        Copyright:MIT软件许可证
************************************Copyright*****************************************/
using System;
using AKNet.Common;
using AKNet.Tcp.Common;

namespace AKNet.Tcp.Server
{
    internal class MsgReceiveMgr
	{
		private readonly CircularBuffer<byte> mReceiveStreamList = null;
		private readonly object lock_mReceiveStreamList_object = new object();
		private ClientPeer mClientPeer;
		private TcpServer mTcpServer;
        public MsgReceiveMgr(ClientPeer mClientPeer, TcpServer mTcpServer)
		{
			this.mTcpServer = mTcpServer;
			this.mClientPeer = mClientPeer;
            mReceiveStreamList = new CircularBuffer<byte>(Config.nSendReceiveCacheBufferInitLength);
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

					if (nPackageCount > 50)
					{
						NetLog.LogWarning("Client 处理逻辑包的数量： " + nPackageCount);
					}

					break;
				default:
					break;
			}
		}
		
        public void ReceiveSocketStream(ReadOnlySpan<byte> readOnlySpan)
		{
			lock (lock_mReceiveStreamList_object)
			{
                mReceiveStreamList.WriteFrom(readOnlySpan);
			}
		}

		private bool NetPackageExecute()
		{
			TcpNetPackage mNetPackage = mTcpServer.mNetPackage;
			bool bSuccess = false;
			lock (lock_mReceiveStreamList_object)
			{
				bSuccess = NetPackageEncryption.DeEncryption(mReceiveStreamList, mNetPackage);
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
				mReceiveStreamList.reset();
			}
		}

	}
}
