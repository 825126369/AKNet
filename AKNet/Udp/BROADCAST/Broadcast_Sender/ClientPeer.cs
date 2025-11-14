/************************************Copyright*****************************************
*        ProjectName:AKNet
*        Web:https://github.com/825126369/AKNet
*        Description:C#游戏网络库
*        Author:许珂
*        StartTime:2024/11/01 00:00:00
*        ModifyTime:2025/11/14 8:56:46
*        Copyright:MIT软件许可证
************************************Copyright*****************************************/
using System;
using AKNet.Common;
using AKNet.Udp.BROADCAST.COMMON;


namespace AKNet.Udp.BROADCAST.Sender
{
    public class ClientPeer : SocketUdp_Basic
	{
		private SafeObjectPool<NetUdpFixedSizePackage> mNetPackagePool = null;

		public ClientPeer()
		{
			mNetPackagePool = new SafeObjectPool<NetUdpFixedSizePackage>(2);
		}

		public void SendNetData(UInt16 id, byte[] buffer)
		{
			if (buffer.Length > Config.nUdpPackageFixedBodySize)
			{
				NetLog.LogError("发送 广播信息流 溢出");
			}

			NetUdpFixedSizePackage mPackage = mNetPackagePool.Pop();
			mPackage.nPackageId = id;
			Array.Copy(buffer, 0, mPackage.buffer, Config.nUdpPackageFixedHeadSize, buffer.Length);
			mPackage.Length = buffer.Length + Config.nUdpPackageFixedHeadSize;
			NetPackageEncryption.Encryption(mPackage);
			SendNetStream(mPackage.buffer, 0, mPackage.Length);

			mNetPackagePool.recycle(mPackage);
		}
	}
}
