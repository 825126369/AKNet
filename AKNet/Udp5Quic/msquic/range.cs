using AKNet.Common;
using System;

namespace AKNet.Udp5Quic.Common
{
    internal class QUIC_SUBRANGE
    {
       public ulong Low;
       public ulong Count;
    }

    internal class QUIC_RANGE
    {
        public QUIC_SUBRANGE SubRanges;
        public uint UsedLength;
        public uint AllocLength;
        public uint MaxAllocSize;
        public QUIC_SUBRANGE[] PreAllocSubRanges = new QUIC_SUBRANGE[MSQuicFunc.QUIC_RANGE_INITIAL_SUB_COUNT];
    }

    internal static partial class MSQuicFunc
    {
        static void QuicRangeInitialize(uint MaxAllocSize, QUIC_RANGE Range)
        {
            Range.UsedLength = 0;
            Range.AllocLength = QUIC_RANGE_INITIAL_SUB_COUNT;
            Range.MaxAllocSize = MaxAllocSize;
            NetLog.Assert(sizeof(QUIC_SUBRANGE) * QUIC_RANGE_INITIAL_SUB_COUNT < MaxAllocSize);
            Range.SubRanges = Range.PreAllocSubRanges;
        }

    }
}
