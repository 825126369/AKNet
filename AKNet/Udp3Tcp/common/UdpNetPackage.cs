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

namespace AKNet.Udp3Tcp.Common
{
    internal class NetUdpSendFixedSizePackage : IPoolItemInterface
    {
        public readonly TcpStanardRTOTimer mTcpStanardRTOTimer = new TcpStanardRTOTimer();
        public readonly CheckPackageInfo_TimeOutGenerator mTimeOutGenerator_ReSend = new CheckPackageInfo_TimeOutGenerator();

        public AkCircularBuffer<byte> mBuffer;
        public int nOffset;

        public uint nOrderId;
        public uint nRequestOrderId;
        public int nBodyLength;

        public NetUdpSendFixedSizePackage()
        {
            this.Reset();
        }

        public void Reset()
        {
            this.mBuffer = null;
            this.nRequestOrderId = 0;
            this.nOrderId = 0;
            this.nBodyLength = 0;
            this.nOffset = 0;
            this.mTimeOutGenerator_ReSend.Reset();
        }

        public bool orInnerCommandPackage()
        {
            return UdpNetCommand.orInnerCommand(GetInnerCommandId());
        }

        public byte GetInnerCommandId()
        {
            if (nOrderId < Config.nUdpMinOrderId)
            {
                return (byte)this.nOrderId;
            }
            return 0;
        }

        public void SetInnerCommandId(byte nPackageId)
        {
            this.nOrderId = nPackageId;
        }
    }

    internal class NetUdpReceiveFixedSizePackage : IPoolItemInterface
    {
        public readonly byte[] mBuffer = new byte[Config.nUdpPackageFixedSize];
        public uint nOrderId;
        public uint nRequestOrderId;
        public int nBodyLength = 0;

        public void Reset()
        {
            this.nRequestOrderId = 0;
            this.nOrderId = 0;
        }

        public bool orInnerCommandPackage()
        {
            return UdpNetCommand.orInnerCommand(GetInnerCommandId());
        }

        public byte GetInnerCommandId()
        {
            if (nOrderId < Config.nUdpMinOrderId)
            {
                return (byte)this.nOrderId;
            }
            return 0;
        }

        public void SetInnerCommandId(byte nPackageId)
        {
            this.nOrderId = nPackageId;
        }

        public void CopyFrom(ReadOnlySpan<byte> stream)
        {
            stream.CopyTo(this.mBuffer);
        }

        public ReadOnlySpan<byte> GetTcpBufferSpan()
        {
            return mBuffer.AsSpan().Slice(0, nBodyLength);
        }
    }

}

