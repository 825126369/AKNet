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
}
