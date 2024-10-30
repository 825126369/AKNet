/************************************Copyright*****************************************
*        ProjectName:AKNet
*        Web:https://github.com/825126369/AKNet
*        Description:AKNet 网络库, 兼容 C#8.0 和 .Net Standard 2.1
*        Author:阿珂
*        CreateTime:2024/10/30 21:55:41
*        Copyright:MIT软件许可证
************************************Copyright*****************************************/
using System;
using System.Net;
using System.Net.Sockets;
using AKNet.Common;

namespace AKNet.Udp.POINTTOPOINT.Common
{
	internal abstract class UdpNetPackage : NetPackage
	{
		internal UInt16 nOrderId;
		internal UInt16 nGroupCount;
        internal UInt16 nSureOrderId;
        public byte[] buffer;
		public int Length;
		public EndPoint remoteEndPoint;

		public UdpNetPackage()
		{
			nSureOrderId = 0;
			nOrderId = 0;
			nGroupCount = 0;
			nPackageId = 0;

			buffer = null;
			Length = 0;
		}

		public override ReadOnlySpan<byte> GetBuffBody()
		{
			return new ReadOnlySpan<byte>(buffer, Config.nUdpPackageFixedHeadSize, Length - Config.nUdpPackageFixedHeadSize);
		}

		public override ReadOnlySpan<byte> GetBuffHead()
		{
			return new ReadOnlySpan<byte>(buffer, 0, Config.nUdpPackageFixedHeadSize);
		}

		public override ReadOnlySpan<byte> GetBuff()
		{
			return new ReadOnlySpan<byte>(buffer);
		}
	}

	internal class NetUdpFixedSizePackage : UdpNetPackage, IPoolItemInterface
	{
		public NetUdpFixedSizePackage()
		{
			buffer = new byte[Config.nUdpPackageFixedSize];
		}

		public void Reset()
		{
            this.nSureOrderId = 0;
            this.nOrderId = 0;
			this.nPackageId = 0;
			this.nGroupCount = 0;
			this.Length = 0;
			this.remoteEndPoint = null;
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
			Array.Copy(e.Buffer, e.Offset, this.buffer, 0, e.BytesTransferred);
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

	internal class NetCombinePackage : UdpNetPackage, IPoolItemInterface
	{
		private int nGetCombineCount;
		public NetCombinePackage()
		{
			this.buffer = new byte[Config.nUdpCombinePackageInitSize];
		}

		public void Init(NetUdpFixedSizePackage mPackage)
		{
			this.nPackageId = mPackage.nPackageId;
			this.nGroupCount = mPackage.nGroupCount;
			this.nOrderId = mPackage.nOrderId;
			this.Length = Config.nUdpPackageFixedHeadSize;

			int nSumLength = this.nGroupCount * Config.nUdpPackageFixedBodySize + Config.nUdpPackageFixedHeadSize;
			if (this.buffer.Length < nSumLength)
			{
				this.buffer = new byte[nSumLength];
				NetLog.LogWarning("NetCombinePackage buffer Length: " + this.buffer.Length);
			}

			nGetCombineCount = 0;
			Add(mPackage);
		}

		public bool Add(NetUdpFixedSizePackage mPackage)
		{
			if (OrderIdHelper.AddOrderId(base.nOrderId, (ushort)nGetCombineCount) == mPackage.nOrderId)
			{
				Combine(mPackage);
				nGetCombineCount++;
				if (nGetCombineCount >= ushort.MaxValue)
				{
					throw new Exception();
				}
				return true;
			}
			return false;
		}

		public bool CheckCombineFinish()
		{
			return nGetCombineCount == base.nGroupCount;
		}

		private void Combine(NetUdpFixedSizePackage mPackage)
		{
			int nCopyLength = mPackage.Length - Config.nUdpPackageFixedHeadSize;
			Array.Copy(mPackage.buffer, Config.nUdpPackageFixedHeadSize, base.buffer, base.Length, nCopyLength);
			base.Length += nCopyLength;
		}

		public void Reset()
		{
			this.nSureOrderId = 0;
			this.nPackageId = 0;
			this.nGroupCount = 0;
			this.nOrderId = 0;
			this.Length = 0;
			this.nGetCombineCount = 0;
			this.remoteEndPoint = null;
		}
	}
}

