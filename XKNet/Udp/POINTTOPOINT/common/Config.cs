namespace XKNet.Udp.POINTTOPOINT.Common
{
    internal static class Config
	{
		//Udp Package OrderId
		public const ushort nUdpMinOrderId = 1;
		public const ushort nUdpMaxOrderId = ushort.MaxValue;

		public const int nUdpPackageFixedSize = 1024 * 8;
		public const int nUdpPackageFixedHeadSize = 10;
		public const int nUdpPackageFixedBodySize = nUdpPackageFixedSize - nUdpPackageFixedHeadSize;
		public const int nUdpCombinePackageFixedSize = 4096;
		
		public const double fReceiveHeartBeatTimeOut = 3.5;
		public const double fMySendHeartBeatMaxTime = 1.0;
		public const double fReceiveReConnectMaxTimeOut = 2.0;
    }
}
