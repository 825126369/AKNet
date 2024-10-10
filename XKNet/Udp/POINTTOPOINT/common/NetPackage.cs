using System;
using System.Net;

namespace XKNet.Udp.POINTTOPOINT.Common
{
    public abstract class NetPackage
	{
		public UInt16 nOrderId;
		public UInt16 nGroupCount;
		public UInt16 nPackageId;

		public byte[] buffer;
		public int Length;

		public NetPackage ()
		{
			nOrderId = 0;
			nGroupCount = 0;
			nPackageId = 0;

			buffer = null;
			Length = 0;
		}
	}

	internal class NetUdpFixedSizePackage : NetPackage
	{
		public NetUdpFixedSizePackage ()
		{
			buffer = new byte[Config.nUdpPackageFixedSize];
		}
	}

	internal class NetCombinePackage : NetPackage
	{
		private int nGetCombineCount;
		public NetCombinePackage ()
		{
			base.buffer = new byte[Config.nUdpCombinePackageFixedSize];
		}

		public void Init(NetUdpFixedSizePackage mPackage)
		{
			base.nPackageId = mPackage.nPackageId;
			base.nGroupCount = mPackage.nGroupCount;
			base.nOrderId = mPackage.nOrderId;

			int nSumLength = base.nGroupCount * Config.nUdpPackageFixedBodySize + Config.nUdpPackageFixedHeadSize;
			if (base.buffer.Length < nSumLength) {
				base.buffer = new byte[nSumLength];
			}

			base.Length = Config.nUdpPackageFixedHeadSize;

			nGetCombineCount = 0;
			Add (mPackage);
		}

		public void Add(NetUdpFixedSizePackage mPackage)
		{
			Combine (mPackage);
			nGetCombineCount++;
		}

		public bool CheckCombineFinish ()
		{
			return nGetCombineCount == base.nGroupCount;
		}

		private void Combine (NetUdpFixedSizePackage mPackage)
		{
			int nCopyLength = mPackage.Length - Config.nUdpPackageFixedHeadSize;
			Array.Copy (mPackage.buffer, Config.nUdpPackageFixedHeadSize, base.buffer, base.Length, nCopyLength);
			base.Length += nCopyLength;
		}

	}

	internal class NetEndPointPackage
	{
		public EndPoint mRemoteEndPoint = null;
		public NetUdpFixedSizePackage mPackage = null;
	}

}

