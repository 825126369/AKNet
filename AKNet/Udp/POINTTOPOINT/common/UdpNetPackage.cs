/************************************Copyright*****************************************
*        ProjectName:AKNet
*        Web:https://github.com/825126369/AKNet
*        Description:这是一个面向 .Net Standard 2.1 的游戏网络库
*        Author:阿珂
*        CreateTime:2024/11/17 12:39:36
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
        internal UInt16 nRequestOrderId;
        public byte[] buffer;
		public int Length;
		public EndPoint remoteEndPoint;

		public UdpNetPackage()
		{
			nRequestOrderId = 0;

			nOrderId = 0;
			nGroupCount = 0;
			nPackageId = 0;

			buffer = null;
			Length = 0;
		}

		public void SetRequestOrderId(UInt16 nOrderId)
		{
			this.nRequestOrderId = nOrderId;
		}

        public UInt16 GetRequestOrderId()
        {
            return this.nRequestOrderId;
        }

        public void SetPackageCheckSureOrderId(UInt16 nOrderId)
        {
            this.nOrderId = nOrderId;
        }

        public UInt16 GetPackageCheckSureOrderId()
        {
            return this.nOrderId;
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
			return new ReadOnlySpan<byte>(buffer, 0, Length);
		}
	}

	internal class NetUdpFixedSizePackage : UdpNetPackage, IPoolItemInterface
	{
		public readonly TcpStanardRTOTimer mTcpStanardRTOTimer = null;
        public NetUdpFixedSizePackage()
		{
			buffer = new byte[Config.nUdpPackageFixedSize];

			if(Config.bUdpCheck)
			{
                mTcpStanardRTOTimer = new TcpStanardRTOTimer();
            }
		}

		public void Reset()
		{
            this.nRequestOrderId = 0;
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

		public void CopyFrom(ReadOnlySpan<byte> stream, int nBeginIndex, int nCount)
		{
			this.Length = nCount;
			for (int i = 0; i < stream.Length; i++)
			{
				this.buffer[i] = stream[i];
			}
		}
	}

	internal class NetCombinePackage : UdpNetPackage, IPoolItemInterface
	{
		private int nGetCombineCount;
		public NetCombinePackage()
		{
			Reset();
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

        public bool CheckReset()
        {
			return nOrderId == 0 || nGetCombineCount == 0;
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
			this.nRequestOrderId = 0;
			this.nPackageId = 0;
			this.nGroupCount = 0;
			this.nOrderId = 0;
			this.Length = 0;
			this.nGetCombineCount = 0;
			this.remoteEndPoint = null;
			NetLog.Assert(CheckCombineFinish());
		}
	}
}

