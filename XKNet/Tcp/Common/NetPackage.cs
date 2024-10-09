using System;

namespace XKNet.Tcp.Common
{
	public class NetPackage
	{
		public ushort nPackageId = 0;
		public ArraySegment<byte> mBufferSegment;
	}
}

