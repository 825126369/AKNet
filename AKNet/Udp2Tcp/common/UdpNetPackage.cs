/************************************Copyright*****************************************
*        ProjectName:AKNet
*        Web:https://github.com/825126369/AKNet
*        Description:这是一个面向 .Net Standard 2.1 的游戏网络库
*        Author:阿珂
*        CreateTime:2024/11/28 7:14:06
*        Copyright:MIT软件许可证
************************************Copyright*****************************************/
using AKNet.Common;
using System;
using System.Net;

namespace AKNet.Udp2Tcp.Common
{
	internal class InnectCommandPeekPackage : IPoolItemInterface
	{
        public UInt16 mPackageId;
		public ushort Length;

        public void Reset()
		{
			this.mPackageId = 0;
			this.Length = 0;
        }
	}

	internal class NetUdpFixedSizePackage : IPoolItemInterface
	{
		public readonly TcpStanardRTOTimer mTcpStanardRTOTimer = null;
		public readonly CheckPackageInfo_TimeOutGenerator mTimeOutGenerator_ReSend = null;
		public UInt16 nOrderId;
		public UInt16 nRequestOrderId;
        public UInt16 nSureOrderId;
        public int Length;
        public EndPoint remoteEndPoint;

        public readonly byte[] buffer;

		public NetUdpFixedSizePackage()
		{
			buffer = new byte[Config.nUdpPackageFixedSize];
            mTcpStanardRTOTimer = new TcpStanardRTOTimer();
            mTimeOutGenerator_ReSend = new CheckPackageInfo_TimeOutGenerator();
        }

		public void Reset()
		{
			this.nSureOrderId = 0;
            this.nRequestOrderId = 0;
			this.nOrderId = 0;
			this.Length = 0;
			this.remoteEndPoint = null;

			if (Config.bUdpCheck)
			{
				mTimeOutGenerator_ReSend.Reset();
			}
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

		public UInt16 GetPackageId()
		{
			if (this.nOrderId < Config.nUdpMinOrderId)
			{
				return this.nOrderId;
			}
			return 0;
		}

        public void SetPackageId(ushort nPackageId)
        {
			this.nOrderId = nPackageId;
        }

		public void CopyFrom(ReadOnlySpan<byte> stream)
		{
			this.Length = Config.nUdpPackageFixedHeadSize + stream.Length;
			if (stream.Length > 0)
			{
				stream.CopyTo(this.buffer.AsSpan().Slice(Config.nUdpPackageFixedHeadSize));
			}
		}

		public void CopyFrom(AkCircularBuffer<byte> mAkCircularBuffer)
		{
			int nReadLength = mAkCircularBuffer.WriteToMax(0, buffer.AsSpan().Slice(Config.nUdpPackageFixedHeadSize));
			this.Length = Config.nUdpPackageFixedHeadSize + nReadLength;
		}

		public ReadOnlySpan<byte> GetBufferSpan()
		{
			return buffer.AsSpan().Slice(0, Length);
		}

        public ReadOnlySpan<byte> GetTcpBufferSpan()
        {
            return buffer.AsSpan().Slice(Config.nUdpPackageFixedHeadSize, Length - Config.nUdpPackageFixedHeadSize);
        }
    }

}

