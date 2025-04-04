using System.Linq;
using System.Net;
using System.Threading;

namespace AKNet.Udp5Quic.Common
{
    internal class QUIC_LOOKUP
    {
        public bool MaximizePartitioning;
        public uint CidCount;
        public readonly ReaderWriterLockSlim RwLock = new ReaderWriterLockSlim();
        public ushort PartitionCount;
        public SINGLE_Class SINGLE;
        public CXPLAT_HASHTABLE RemoteHashTable;
        public HASH_Class HASH;

        void LookupTable;
        public class SINGLE_Class
        {
             public QUIC_CONNECTION Connection;
        }
        public class HASH_Class
        {
             public QUIC_PARTITIONED_HASHTABLE Tables;
        }
    }

    internal static partial class MSQuicFunc
    {
        static QUIC_CONNECTION QuicLookupFindConnectionByRemoteAddr(QUIC_LOOKUP Lookup, IPAddress RemoteAddress)
        {
            QUIC_CONNECTION ExistingConnection = null;
            Lookup.RwLock.EnterReadLock();
            if (Lookup.PartitionCount == 0)
            {
                ExistingConnection = Lookup.SINGLE.Connection;
            }
            else
            {

            }

            if (ExistingConnection != null)
            {
                QuicConnAddRef(ExistingConnection, QUIC_CONNECTION_REF.QUIC_CONN_REF_LOOKUP_RESULT);
            }
            Lookup.RwLock.ExitReadLock();
            return ExistingConnection;
        }
    }
}
