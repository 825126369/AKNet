using XKNet.Common;

namespace XKNet.Udp.POINTTOPOINT.Common
{
	internal static class Config
	{
		//Udp Package OrderId
		public const ushort nUdpMinOrderId = 1;
		public const ushort nUdpMaxOrderId = ushort.MaxValue;

		public const int nUdpPackageFixedSize = 1024;
		public const int nUdpPackageFixedHeadSize = 12;
		public const int nUdpPackageFixedBodySize = nUdpPackageFixedSize - nUdpPackageFixedHeadSize;
		public const int nUdpCombinePackageInitSize = 1024 * 8; //合并包是可变的
		public const int nMsgPackageBufferMaxLength = 1024 * 8 - nUdpPackageFixedHeadSize;

		public const double fReceiveHeartBeatTimeOut = 5.0;
		public const double fMySendHeartBeatMaxTime = 2.0;

		//Server
		public const int numConnections = 10000;
	}
}
