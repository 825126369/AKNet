/************************************Copyright*****************************************
*        ProjectName:AKNet
*        Web:https://github.com/825126369/AKNet
*        Description:C#游戏网络库
*        Author:许珂
*        StartTime:2024/11/01 00:00:00
*        ModifyTime:2025/11/30 19:43:15
*        Copyright:MIT软件许可证
************************************Copyright*****************************************/

#if NET9_0_OR_GREATER
using AKNet.Common;
using AKNet.Quic.Common;
using System;

namespace AKNet.Quic.Server
{
    internal partial class ClientPeer
    {
        public void MultiThreadingReceiveSocketStream(ReadOnlySpan<byte> e)
		{
			lock (lock_mReceiveStreamList_object)
			{
                mReceiveStreamList.WriteFrom(e);
			}
		}

		private bool NetPackageExecute()
		{
			NetStreamPackage mNetPackage = mServerMgr.mNetPackage;
			bool bSuccess = false;
			lock (lock_mReceiveStreamList_object)
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

#endif
