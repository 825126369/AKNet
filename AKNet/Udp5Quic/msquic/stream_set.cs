namespace AKNet.Udp5Quic.Common
{
    internal class QUIC_STREAM_TYPE_INFO
    {
        public long MaxTotalStreamCount;
        public long TotalStreamCount;

        public int MaxCurrentStreamCount;
        public int CurrentStreamCount;
    }

    internal class QUIC_STREAM_SET
    {
        public readonly QUIC_STREAM_TYPE_INFO[] Types = new QUIC_STREAM_TYPE_INFO[MSQuicFunc.NUMBER_OF_STREAM_TYPES];
        public CXPLAT_HASHTABLE StreamTable;
        public CXPLAT_LIST_ENTRY WaitingStreams;
        public CXPLAT_LIST_ENTRY ClosedStreams;

#if DEBUG
        public CXPLAT_LIST_ENTRY AllStreams;
        public readonly object AllStreamsLock = new object();
#endif
    }
}
