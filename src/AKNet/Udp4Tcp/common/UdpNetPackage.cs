/************************************Copyright*****************************************
*        ProjectName:AKNet
*        Web:https://github.com/825126369/AKNet
*        Description:C#游戏网络库
*        Author:许珂
*        StartTime:2024/11/01 00:00:00
*        ModifyTime:2025/11/30 19:43:16
*        Copyright:MIT软件许可证
************************************Copyright*****************************************/
using AKNet.Common;
using System;

namespace AKNet.Udp4Tcp.Common
{
    internal class NetUdpSendFixedSizePackage : IPoolItemInterface
    {
        public readonly TcpStanardRTOTimer mTcpStanardRTOTimer = new TcpStanardRTOTimer();
        public TcpSlidingWindow mTcpSlidingWindow;

        public uint nOrderId;
        public uint nRequestOrderId;
        public ushort nBodyLength;
        public ushort nSendCount;
        public long nLastSendTime = 0;
        public long nInternalTime;

        public NetUdpSendFixedSizePackage()
        {
            this.Reset();
        }

        public void Reset()
        {
            this.nSendCount = 0;
            this.mTcpSlidingWindow = null;
            this.nRequestOrderId = 0;
            this.nOrderId = 0;
            this.nBodyLength = 0;
        }

        public bool orTimeOut()
        {
            return UdpStaticCommon.GetNowTime() - nLastSendTime >= this.nInternalTime;
        }

        public void SetInternalTime(long InternalTime)
        {
            this.nInternalTime = InternalTime;
        }

        public TcpSlidingWindow WindowBuff { 
            get {
                return mTcpSlidingWindow; 
            } 
        }

        public int WindowOffset { 
            get {
                if (mTcpSlidingWindow != null)
                {
                    return mTcpSlidingWindow.GetWindowOffset(nOrderId);
                }
                return 0;
            } 
        }
        public int WindowLength { 
            get { 
                return nBodyLength; 
            } 
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
        public ushort nBodyLength = 0;

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

