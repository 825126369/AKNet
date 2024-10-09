namespace XKNet.Udp.BROADCAST.COMMON
{
    internal class Config
	{
		public const int nUdpPackageFixedSize = 512;
		public const int nUdpPackageFixedHeadSize = 6;
		public const int nUdpPackageFixedBodySize = nUdpPackageFixedSize - nUdpPackageFixedHeadSize;
	}
}
