using System;

namespace XKNetUDP_BROADCAST_COMMON
{
    public class NetUdpFixedSizePackage
	{
		public UInt16 nPackageId;

		public byte[] buffer;
		public int Length;

		public NetUdpFixedSizePackage ()
		{
			nPackageId = 0;

			Length = 0;
			buffer = new byte[Config.nUdpPackageFixedSize];
		}
	}
}

