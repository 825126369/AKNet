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
        public byte nPackageId;
        public uint nOrderId;
        public uint nRequestOrderId;

        public void Reset()
        {
            this.nRequestOrderId = 0;
            this.nOrderId = 0;
            mTimeOutGenerator_ReSend.Reset();
        }

        public int Length
        {
            get
            {
                return (int)(this.nRequestOrderId - this.nOrderId);
            }
        }
    }

    internal class NetUdpReceiveFixedSizePackage : IPoolItemInterface
    {
        public readonly byte[] mBuffer = new byte[Config.nUdpPackageFixedSize];

        public byte nPackageId;
        public uint nOrderId;
        public uint nRequestOrderId;

        public void Reset()
        {
            this.nPackageId = 0;
            this.nRequestOrderId = 0;
            this.nOrderId = 0;
        }

        public int Length
        {
            get
            {
                return (int)(this.nRequestOrderId - this.nOrderId);
            }
        }

        public ReadOnlySpan<byte> GetTcpBufferSpan()
        {
            return mBuffer.AsSpan().Slice(0, Length);
        }

    }

}

