using System;
using System.Net;
using System.Net.Sockets;
using XKNet.Common;

namespace XKNet.Udp.POINTTOPOINT.Common
{
	internal class UdpNetPackage : NetPackage
	{
		internal UInt16 nOrderId;
		internal UInt16 nGroupCount;
		public byte[] buffer;
		public int Length;

		public UdpNetPackage()
		{
			nOrderId = 0;
			nGroupCount = 0;
			nPackageId = 0;

			buffer = null;
			Length = 0;
		}

		public override ReadOnlySpan<byte> GetMsgSpin()
		{
			return new ReadOnlySpan<byte>(buffer, Config.nUdpPackageFixedHeadSize, Length - Config.nUdpPackageFixedHeadSize);
		}
	}

	internal class NetUdpFixedSizePackage : UdpNetPackage
	{
		public NetUdpFixedSizePackage()
		{
			buffer = new byte[Config.nUdpPackageFixedSize];
		}

		public void Reset()
		{
			this.nOrderId = 0;
			this.nPackageId = 0;
			this.nGroupCount = 0;
			this.Length = 0;
		}

		public void CopyFrom(NetUdpFixedSizePackage other)
		{
			this.nOrderId = other.nOrderId;
			this.nPackageId = other.nPackageId;
			this.nGroupCount = other.nGroupCount;
			this.Length = other.Length;
			Array.Copy(other.buffer, 0, this.buffer, 0, this.Length);
		}

		public void CopyFrom(SocketAsyncEventArgs e)
		{
			this.Length = e.BytesTransferred;
			Array.Copy(e.Buffer, 0, this.buffer, 0, e.BytesTransferred);
		}

		public void CopyFromMsgStream(ReadOnlySpan<byte> stream, int nBeginIndex, int nCount)
		{
			this.Length = Config.nUdpPackageFixedHeadSize + nCount;
			for (int i = 0; i < nCount; i++)
			{
				this.buffer[Config.nUdpPackageFixedHeadSize + i] = stream[nBeginIndex + i];
			}
		}

		public void CopyFromMsgStream(ReadOnlySpan<byte> stream)
		{
			this.Length = Config.nUdpPackageFixedHeadSize + stream.Length;
			for (int i = 0; i < stream.Length; i++)
			{
				this.buffer[Config.nUdpPackageFixedHeadSize + i] = stream[i];
			}
		}
	}

	internal class NetCombinePackage : UdpNetPackage
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
			if (base.buffer.Length < nSumLength)
			{
				base.buffer = new byte[nSumLength];
			}

			base.Length = Config.nUdpPackageFixedHeadSize;

			nGetCombineCount = 0;
			Add(mPackage);
		}

		private int AddOrderId(int nOrderId, int nAddCount)
		{
			nOrderId += nAddCount;
			if (nOrderId > Config.nUdpMaxOrderId)
			{
				nOrderId -= Config.nUdpMaxOrderId;
			}

			if (nOrderId > Config.nUdpMaxOrderId)
			{
				NetLog.Assert(false, "nOrderId 用尽了！！！！， 合包内数量太多！！！" + nGroupCount);
			}

			return nOrderId;
		}

        public bool Add(NetUdpFixedSizePackage mPackage)
		{
            if (AddOrderId(base.nOrderId, nGetCombineCount) != mPackage.nOrderId)
			{
				return false;
			}
			
			Combine(mPackage);
			nGetCombineCount++;
			return true;
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

