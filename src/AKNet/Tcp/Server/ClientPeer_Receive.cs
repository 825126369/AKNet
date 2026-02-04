/************************************Copyright*****************************************
*        ProjectName:AKNet
*        Web:https://github.com/825126369/AKNet
*        Description:C#游戏网络库
*        Author:许珂
*        StartTime:2024/11/01 00:00:00
*        ModifyTime:2026/2/1 20:26:47
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
        public void MultiThreadingReceiveSocketStream(SocketAsyncEventArgs e)
		{
			lock (mReceiveStreamList)
			{
                mReceiveStreamList.WriteFrom(e.MemoryBuffer.Span.Slice(e.Offset, e.BytesTransferred));
            }
		}

		private bool NetPackageExecute()
		{
			NetStreamReceivePackage mNetPackage = mServerMgr.mNetPackage;
			bool bSuccess = false;
			lock (mReceiveStreamList)
			{
				bSuccess = mServerMgr.mCryptoMgr.Decode(mReceiveStreamList, mNetPackage);
			}

			if (bSuccess)
			{
				if (TcpNetCommand.orInnerCommand(mNetPackage.nPackageId))
				{

				}
				else
				{
                    mServerMgr.mPackageManager.NetPackageExecute(this, mNetPackage);
				}
			}

			return bSuccess;
		}
    }
}
