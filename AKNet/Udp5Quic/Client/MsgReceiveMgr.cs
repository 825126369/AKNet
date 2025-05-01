/************************************Copyright*****************************************
*        ProjectName:AKNet
*        Web:https://github.com/825126369/AKNet
*        Description:这是一个面向 .Net Standard 2.1 的游戏网络库
*        Author:阿珂
*        ModifyTime:2025/2/27 22:28:11
*        Copyright:MIT软件许可证
************************************Copyright*****************************************/
using System;
using System.Net.Sockets;
using AKNet.Common;
using AKNet.Udp5Quic.Common;

namespace AKNet.Udp5Quic.Client
{
	//和线程打交道
	internal class MsgReceiveMgr
	{
		private readonly AkCircularBuffer mReceiveStreamList = new AkCircularBuffer();
		protected readonly TcpNetPackage mNetPackage = null;
		private ClientPeer mClientPeer;
		public MsgReceiveMgr(ClientPeer mClientPeer)
		{
			this.mClientPeer = mClientPeer;
			mNetPackage = new TcpNetPackage();
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

        public void MultiThreadingReceiveSocketStream(ReadOnlySpan<byte> e)
		{
			lock (mReceiveStreamList)
			{
                mReceiveStreamList.WriteFrom(e);
            }
        }

		private bool NetPackageExecute()
		{
			bool bSuccess = false;

			lock (mReceiveStreamList)
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
