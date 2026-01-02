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
        public readonly ReSendPackageTimeOut mReSendTimer = new ReSendPackageTimeOut();

        public TcpSlidingWindow mTcpSlidingWindow;
        public readonly SSocketAsyncEventArgs mSendArgs = new SSocketAsyncEventArgs();
        public LogicWorker mLogicWorker;

        public uint nOrderId;
        public uint nRequestOrderId;
        public ushort nBodyLength;
        public ushort nSendCount;

        public NetUdpSendFixedSizePackage()
        {
            mSendArgs.SetBuffer(new byte[Config.nUdpPackageFixedSize], 0, Config.nUdpPackageFixedSize);
            Reset();
        }

        public void Reset()
        {
            nSendCount = 0;
            mTcpSlidingWindow = null;
            mLogicWorker = null;
            nRequestOrderId = 0;
            nOrderId = 0;
            nBodyLength = 0;
            mSendArgs.SetBuffer(0, Config.nUdpPackageFixedSize);
        }

        public void SetLogicWorker(LogicWorker mLogicWorker)
        {
            this.mLogicWorker = mLogicWorker;
            this.mReSendTimer.SetLogicWorker(mLogicWorker);
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
                return (byte)nOrderId;
            }
            return 0;
        }

        public void SetInnerCommandId(byte nPackageId)
        {
            nOrderId = nPackageId;
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
            nRequestOrderId = 0;
            nOrderId = 0;
        }

        public bool orInnerCommandPackage()
        {
            return UdpNetCommand.orInnerCommand(GetInnerCommandId());
        }

        public byte GetInnerCommandId()
        {
            if (nOrderId < Config.nUdpMinOrderId)
            {
                return (byte)nOrderId;
            }
            return 0;
        }

        public void SetInnerCommandId(byte nPackageId)
        {
            nOrderId = nPackageId;
        }

        public void CopyFrom(ReadOnlySpan<byte> stream)
        {
            stream.CopyTo(mBuffer);
        }

        public ReadOnlySpan<byte> GetTcpBufferSpan()
        {
            return mBuffer.AsSpan().Slice(0, nBodyLength);
        }
    }

}

