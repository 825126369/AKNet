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
using System;
using System.Net.Sockets;

namespace AKNet.Tcp.Client
{
    //和线程打交道
    internal partial class ClientPeer
    {
        private void Update_Receive(double elapsed)
		{
			var mSocketPeerState = GetSocketState();
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
						ReceiveHeartBeat();
					}

					break;
				default:
					break;
			}
		}

        private void MultiThreadingReceiveSocketStream(SocketAsyncEventArgs e)
		{
			lock (mReceiveStreamList)
			{
                mReceiveStreamList.WriteFrom(e.Buffer.AsSpan().Slice(e.Offset, e.Count));
            }
        }

		private bool NetPackageExecute()
		{
			bool bSuccess = false;

			lock (mReceiveStreamList)
			{
				bSuccess = mCryptoMgr.Decode(mReceiveStreamList, mNetPackage);
			}

			if (bSuccess)
			{
				if (TcpNetCommand.orInnerCommand(mNetPackage.nPackageId))
				{

				}
				else
				{
					mPackageManager.NetPackageExecute(this, mNetPackage);
				}
			}

			return bSuccess;
		}
	}
}
