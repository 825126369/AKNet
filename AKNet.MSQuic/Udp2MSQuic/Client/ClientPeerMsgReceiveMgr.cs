/************************************Copyright*****************************************
*        ProjectName:AKNet
*        Web:https://github.com/825126369/AKNet
*        Description:C#游戏网络库
*        Author:许珂
*        ModifyTime:2025/11/14 8:44:38
*        Copyright:MIT软件许可证
************************************Copyright*****************************************/
using AKNet.Common;
using AKNet.Udp2MSQuic.Common;
using System;

namespace AKNet.Udp2MSQuic.Client
{
	//和线程打交道
	internal class MsgReceiveMgr
	{
		private readonly NetStreamCircularBuffer mReceiveStreamList = new NetStreamCircularBuffer();
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
				mReceiveStreamList.Reset();
			}
		}
	}
}
