namespace AKNet.Udp5Quic.Common
{
    internal class CXPLAT_HASHTABLE_ENTRY
    {
        public CXPLAT_LIST_ENTRY Linkage;
        public ulong Signature;
    }

    internal class CXPLAT_HASHTABLE
    {
        public uint Flags;
        public uint TableSize;
        public uint Pivot;
        public uint DivisorMask;
        public uint NumEntries;
        public uint NonEmptyBuckets;
        public uint NumEnumerators;
        void* Directory;
        public CXPLAT_LIST_ENTRY SecondLevelDir; // When TableSize <= HT_SECOND_LEVEL_DIR_MIN_SIZE
        public CXPLAT_LIST_ENTRY FirstLevelDir; // When TableSize > HT_SECOND_LEVEL_DIR_MIN_SIZE
    }

}
