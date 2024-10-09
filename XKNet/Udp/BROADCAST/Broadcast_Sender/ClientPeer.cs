using System;
using XKNetCommon;
using XKNetUDP_BROADCAST_COMMON;


namespace XKNetUDP_BROADCAST_Sender
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
