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
using AKNet.Tcp.Common;

namespace AKNet.Tcp.Server
{
    internal partial class ClientPeer
	{
		public void UpdateReceive(double elapsed)
		{
			switch (GetSocketState())
			{
				case SOCKET_PEER_STATE.CONNECTED:
					int nPackageCount = 0;

					while (NetPackageExecute())
					{
						nPackageCount++;
					}

					if (nPackageCount > 0)
					{
						ReceiveHeartBeat();
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
                mReceiveStreamList.WriteFrom(e.Buffer.AsSpan().Slice(e.Offset, e.BytesTransferred));
			}
		}

		private bool NetPackageExecute()
		{
			TcpNetPackage mNetPackage = this.mNetServer.mNetPackage;
			bool bSuccess = false;
			lock (lock_mReceiveStreamList_object)
			{
				bSuccess = this.mNetServer.mCryptoMgr.Decode(mReceiveStreamList, mNetPackage);
			}

			if (bSuccess)
			{
				if (TcpNetCommand.orInnerCommand(mNetPackage.nPackageId))
				{

				}
				else
				{
                    this.mNetServer.mPackageManager.NetPackageExecute(this, mNetPackage);
				}
			}

			return bSuccess;
		}

	}
}
