/************************************Copyright*****************************************
*        ProjectName:AKNet
*        Web:https://github.com/825126369/AKNet
*        Description:C#游戏网络库
*        Author:许珂
*        StartTime:2024/11/01 00:00:00
*        ModifyTime:2026/2/1 20:26:53
*        Copyright:MIT软件许可证
************************************Copyright*****************************************/
using AKNet.Common;
using System;
using System.Collections.Generic;

namespace AKNet.Udp5Tcp.Common
{
    internal class NetStreamSendPackage : IPoolItemInterface
    {
        private readonly LinkedListNode<NetStreamSendPackage> mEntry;
        public readonly Memory<byte> mBuffer = new byte[Config.nUdpPackageFixedSize];
        public int nLength;
        public NetStreamSendPackage()
        {
            mEntry = new LinkedListNode<NetStreamSendPackage>(this);
        }

        public LinkedListNode<NetStreamSendPackage> GetEntry()
        {
            return mEntry;
        }

        public void Create(ReadOnlySpan<byte> other)
        {
            other.CopyTo(mBuffer.Span);
            nLength = other.Length;
        }

        public void Reset()
        {
            nLength = 0;
        }

        public ReadOnlySpan<byte> GetCanReadSpan()
        {
            return mBuffer.Span.Slice(0, nLength);
        }
    }

    internal class NetUdpSendFixedSizePackage : IPoolItemInterface
    {
        public readonly TcpStanardRTOTimer mTcpStanardRTOTimer = new TcpStanardRTOTimer();
        public readonly ReSendPackageTimeOut mReSendTimer = new ReSendPackageTimeOut();

        public TcpSlidingWindow mTcpSlidingWindow;
        public LogicWorker mLogicWorker;

        public uint nOrderId;
        public uint nRequestOrderId;
        public ushort nBodyLength;
        public ushort nSendCount;

        public NetUdpSendFixedSizePackage()
        {
            Reset();
        }

        public void Reset()
        {
            nSendCount = 0;
            mTcpSlidingWindow = null;
            nRequestOrderId = 0;
            nOrderId = 0;
            nBodyLength = 0;
            SetLogicWorker(null);
        }

        public void SetLogicWorker(LogicWorker mLogicWorker)
        {
            this.mLogicWorker = mLogicWorker;
            this.mReSendTimer.SetLogicWorker(mLogicWorker);
            this.mTcpStanardRTOTimer.SetLogicWorker(mLogicWorker);
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
        public readonly byte[] mBuffer = new byte[Config.nUdpPackageFixedBodySize];
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

