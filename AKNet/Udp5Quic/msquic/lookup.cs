namespace AKNet.Udp5Quic.Common
{
    internal class QUIC_LOOKUP
    {

        public bool MaximizePartitioning;
        public uint CidCount;
        public readonly object RwLock = new object();
        public ushort PartitionCount;

    
        void LookupTable;

        struct SINGLE
        {
            QUIC_CONNECTION Connection;
        }

        struct HASH
        {
            QUIC_PARTITIONED_HASHTABLE* Tables;
        
        }

        CXPLAT_HASHTABLE RemoteHashTable;

    }
}
