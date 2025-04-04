using AKNet.Common;
using System.Linq;
using System.Net;
using System.Threading;

namespace AKNet.Udp5Quic.Common
{
    internal class QUIC_REMOTE_HASH_ENTRY
    {
        public CXPLAT_HASHTABLE_ENTRY Entry;
        public QUIC_CONNECTION Connection;
        public IPAddress RemoteAddress;
        public int RemoteCidLength;
        public byte[] RemoteCid;
    }

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

        static void QuicLookupRemoveRemoteHash(QUIC_LOOKUP Lookup, QUIC_REMOTE_HASH_ENTRY RemoteHashEntry)
        {
            QUIC_CONNECTION Connection = RemoteHashEntry.Connection;
            NetLog.Assert(Lookup.MaximizePartitioning);

            QuicLibraryOnHandshakeConnectionRemoved();

            CxPlatDispatchRwLockAcquireExclusive(Lookup.RwLock);
            NetLog.Assert(Connection.RemoteHashEntry != null);
            CxPlatHashtableRemove(Lookup.RemoteHashTable, RemoteHashEntry.Entry, null);
            Connection.RemoteHashEntry = null;
            CxPlatDispatchRwLockReleaseExclusive(Lookup.RwLock);

            CXPLAT_FREE(RemoteHashEntry, QUIC_POOL_REMOTE_HASH);
            QuicConnRelease(Connection, QUIC_CONN_REF_LOOKUP_TABLE);
        }
    }
}
