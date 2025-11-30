/************************************Copyright*****************************************
*        ProjectName:AKNet
*        Web:https://github.com/825126369/AKNet
*        Description:C#游戏网络库
*        Author:许珂
*        StartTime:2024/11/01 00:00:00
*        ModifyTime:2025/11/30 19:43:17
*        Copyright:MIT软件许可证
************************************Copyright*****************************************/
using AKNet.Common;
using System;
using System.Collections.Generic;
using System.Net;

namespace AKNet.Udp.POINTTOPOINT.Common
{
    internal abstract class UdpNetPackage:NetPackage
	{
        public ushort nPackageId = 0;
        public UInt16 nOrderId;
        public UInt16 nGroupCount;
        public UInt16 nRequestOrderId;
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
		
        public ReadOnlySpan<byte> GetData()
		{
			return new ReadOnlySpan<byte>(buffer, Config.nUdpPackageFixedHeadSize, Length - Config.nUdpPackageFixedHeadSize);
		}

        public ushort GetPackageId()
        {
			return nPackageId;
        }

        void NetPackage.SetData(Memory<byte> mData)
        {
            throw new NotImplementedException();
        }
    }

	internal class InnectCommandPeekPackage : UdpNetPackage, IPoolItemInterface
	{
		public void Reset()
		{
			this.nRequestOrderId = 0;
			this.nOrderId = 0;
			this.nPackageId = 0;
			this.nGroupCount = 0;
			this.Length = 0;
			this.remoteEndPoint = null;
            this.buffer = null;
        }
	}


    internal class NetUdpFixedSizePackage : UdpNetPackage, IPoolItemInterface
	{
		public readonly LinkedListNode<NetUdpFixedSizePackage> mEntry;
		public readonly TcpStanardRTOTimer mTcpStanardRTOTimer = null;
		public readonly CheckPackageInfo_TimeOutGenerator mTimeOutGenerator_ReSend = null;
		public NetUdpFixedSizePackage()
		{
			mEntry = new LinkedListNode<NetUdpFixedSizePackage>(this);
            buffer = new byte[Config.nUdpPackageFixedSize];

			if (Config.bUdpCheck)
			{
				mTcpStanardRTOTimer = new TcpStanardRTOTimer();
				mTimeOutGenerator_ReSend = new CheckPackageInfo_TimeOutGenerator();
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
			if (Config.bUdpCheck)
			{
				mTimeOutGenerator_ReSend.Reset();
			}
		}

		public void CopyFrom(NetUdpFixedSizePackage other)
		{
			this.nOrderId = other.nOrderId;
			this.nPackageId = other.nPackageId;
			this.nGroupCount = other.nGroupCount;
			this.Length = other.Length;
			this.nRequestOrderId = other.nRequestOrderId;

            Buffer.BlockCopy(other.buffer, 0, this.buffer, 0, this.Length);
		}

		public void CopyFrom(ReadOnlySpan<byte> stream)
		{
            this.Length = Config.nUdpPackageFixedHeadSize + stream.Length;
            if (stream.Length > 0)
			{
				stream.CopyTo(this.buffer.AsSpan().Slice(Config.nUdpPackageFixedHeadSize));
			}
		}

		public ReadOnlySpan<byte> GetBufferSpan()
		{
			return buffer.AsSpan().Slice(0, Length);
        }
	}

	internal class NetCombinePackage : UdpNetPackage, IPoolItemInterface
	{
		private int nGetCombineCount;
		public NetCombinePackage()
		{
			Reset();
			this.buffer = new byte[Config.nUdpPackageFixedSize * 2];
		}

		public void Init(NetUdpFixedSizePackage mPackage)
		{
			this.nPackageId = mPackage.nPackageId;
			this.nGroupCount = mPackage.nGroupCount;
			this.nOrderId = mPackage.nOrderId;
			this.Length = Config.nUdpPackageFixedHeadSize;

			int nSumLength = this.nGroupCount * Config.nUdpPackageFixedBodySize + Config.nUdpPackageFixedHeadSize;
            BufferTool.EnSureBufferOk_Power2(ref this.buffer, nSumLength);
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

