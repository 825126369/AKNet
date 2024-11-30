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

namespace AKNet.Udp3Tcp.Common
{
	internal class InnectCommandPeekPackage : IPoolItemInterface
	{
        public UInt16 mPackageId;
		public int Length;

        public void Reset()
		{
			this.mPackageId = 0;
			this.Length = 0;
        }
	}

	internal class NetUdpFixedSizePackage : IPoolItemInterface
	{
        public readonly byte[] buffer;
        public readonly TcpStanardRTOTimer mTcpStanardRTOTimer = null;
		public readonly CheckPackageInfo_TimeOutGenerator mTimeOutGenerator_ReSend = null;
        public EndPoint remoteEndPoint;

        public uint nOrderId;
		public uint nRequestOrderId;
        public int Length;

		public NetUdpFixedSizePackage()
		{
			buffer = new byte[Config.nUdpPackageFixedSize];
            mTcpStanardRTOTimer = new TcpStanardRTOTimer();
            mTimeOutGenerator_ReSend = new CheckPackageInfo_TimeOutGenerator();
        }

		public void Reset()
		{
            this.nRequestOrderId = 0;
			this.nOrderId = 0;
			this.Length = 0;
			this.remoteEndPoint = null;

			if (Config.bUdpCheck)
			{
				mTimeOutGenerator_ReSend.Reset();
			}
		}

		public void SetRequestOrderId(uint nOrderId)
		{
			this.nRequestOrderId = nOrderId;
		}

		public uint GetRequestOrderId()
		{
			return this.nRequestOrderId;
		}

		public ushort GetPackageId()
		{
            return (ushort)this.nOrderId;
        }

        public void SetPackageId(ushort nPackageId)
        {
			this.nOrderId = nPackageId;
        }

		public void CopyFrom(ReadOnlySpan<byte> stream)
		{
			this.Length = (Config.nUdpPackageFixedHeadSize + stream.Length);
			if (stream.Length > 0)
			{
				stream.CopyTo(this.buffer.AsSpan().Slice(Config.nUdpPackageFixedHeadSize));
			}
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

