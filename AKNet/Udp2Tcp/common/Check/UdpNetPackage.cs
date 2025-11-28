/************************************Copyright*****************************************
*        ProjectName:AKNet
*        Web:https://github.com/825126369/AKNet
*        Description:C#游戏网络库
*        Author:许珂
*        StartTime:2024/11/01 00:00:00
*        ModifyTime:2025/11/29 4:33:43
*        Copyright:MIT软件许可证
************************************Copyright*****************************************/
using AKNet.Common;
using System;
using System.Collections.Generic;
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
		public LinkedListNode<NetUdpFixedSizePackage> mEntry;
		public readonly TcpStanardRTOTimer mTcpStanardRTOTimer = null;
		public readonly CheckPackageInfo_TimeOutGenerator mTimeOutGenerator_ReSend = null;
		public UInt16 nOrderId;
		public UInt16 nRequestOrderId;
        public int Length;
        public EndPoint remoteEndPoint;

        public readonly byte[] buffer;

		public NetUdpFixedSizePackage()
		{
			mEntry = new LinkedListNode<NetUdpFixedSizePackage>(this);
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

        public void SetRequestOrderId(UInt16 nOrderId)
		{
			this.nRequestOrderId = nOrderId;
		}

		public UInt16 GetRequestOrderId()
		{
			return this.nRequestOrderId;
		}

		public UInt16 GetPackageId()
		{
            return this.nOrderId;
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

