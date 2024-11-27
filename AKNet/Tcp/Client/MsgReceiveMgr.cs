/************************************Copyright*****************************************
*        ProjectName:AKNet
*        Web:https://github.com/825126369/AKNet
*        Description:这是一个面向 .Net Standard 2.1 的游戏网络库
*        Author:阿珂
*        CreateTime:2024/11/23 22:12:36
*        Copyright:MIT软件许可证
************************************Copyright*****************************************/
using System;
using System.Net.Sockets;
using AKNet.Common;
using AKNet.Tcp.Common;

namespace AKNet.Tcp.Client
{
	//和线程打交道
	internal class MsgReceiveMgr
	{
		private readonly AkCircularBuffer<byte> mReceiveStreamList = null;
		protected readonly TcpNetPackage mNetPackage = null;

		private readonly object lock_mReceiveStreamList_object = new object();
		private ClientPeer mClientPeer;
		public MsgReceiveMgr(ClientPeer mClientPeer)
		{
			this.mClientPeer = mClientPeer;
			mNetPackage = new TcpNetPackage();
			mReceiveStreamList = new AkCircularBuffer<byte>(Config.nCircularBufferInitCapacity);
		}

		public void Update(double elapsed)
		{
			var mSocketPeerState = mClientPeer.GetSocketState();
			switch (mSocketPeerState)
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
					//	NetLog.LogWarning("Client 处理逻辑包的数量： " + nPackageCount);
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
                mReceiveStreamList.WriteFrom(e.Buffer, e.Offset, e.BytesTransferred);
            }
        }

		private bool NetPackageExecute()
		{
			bool bSuccess = false;

			lock (lock_mReceiveStreamList_object)
			{
				bSuccess = mClientPeer.mCryptoMgr.Decode(mReceiveStreamList, mNetPackage);
			}

			if (bSuccess)
			{
				if (TcpNetCommand.orInnerCommand(mNetPackage.nPackageId))
				{

				}
				else
				{
					mClientPeer.mPackageManager.NetPackageExecute(this.mClientPeer, mNetPackage);
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
