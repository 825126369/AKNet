namespace XKNetUDP_BROADCAST_COMMON
{
    public class Config
	{
		public const int nUdpPackageFixedSize = 512;
		public const int nUdpPackageFixedHeadSize = 6;
		public const int nUdpPackageFixedBodySize = nUdpPackageFixedSize - nUdpPackageFixedHeadSize;
	}
}
