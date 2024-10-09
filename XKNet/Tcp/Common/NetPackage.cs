using System;

namespace XKNetTcpCommon
{
	public class NetPackage
	{
		public ushort nPackageId = 0;
		public ArraySegment<byte> mBufferSegment;
	}
}

