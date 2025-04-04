﻿using AKNet.Common;
using System.Net;
using System.Threading;

namespace AKNet.Udp5Quic.Common
{
    internal class QUIC_PARTITIONED_HASHTABLE 
    {
        public ReaderWriterLockSlim RwLock;
        public CXPLAT_HASHTABLE Table;
    }

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
             public QUIC_PARTITIONED_HASHTABLE[] Tables;
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

            QuicConnRelease(Connection, QUIC_CONNECTION_REF.QUIC_CONN_REF_LOOKUP_TABLE);
        }

        static void QuicLookupRemoveLocalCidInt(QUIC_LOOKUP Lookup, QUIC_CID_HASH_ENTRY SourceCid)
        {
            NetLog.Assert(SourceCid.CID.IsInLookupTable);
            NetLog.Assert(Lookup.CidCount != 0);
            Lookup.CidCount--;

            if (Lookup.PartitionCount == 0)
            {
                NetLog.Assert(Lookup.SINGLE.Connection == SourceCid.Connection);
                if (Lookup.CidCount == 0)
                {
                    Lookup.SINGLE.Connection = null;
                }
            }
            else
            {
                NetLog.Assert(SourceCid.CID.Length >= MsQuicLib.CidServerIdLength + QUIC_CID_PID_LENGTH);
                NetLog.Assert(QUIC_CID_PID_LENGTH == 2, "The code below assumes 2 bytes");
                int PartitionIndex;
                EndianBitConverter.SetBytes(SourceCid.CID.Data, MsQuicLib.CidServerIdLength, (ushort)PartitionIndex);

                PartitionIndex &= MsQuicLib.PartitionMask;
                PartitionIndex %= Lookup.PartitionCount;
                QUIC_PARTITIONED_HASHTABLE Table = Lookup.HASH.Tables[PartitionIndex];
                CxPlatDispatchRwLockAcquireExclusive(Table.RwLock);
                CxPlatHashtableRemove(Table.Table, SourceCid.Entry, null);
                CxPlatDispatchRwLockReleaseExclusive(Table.RwLock);
            }
        }

        static void QuicLookupRemoveLocalCids(QUIC_LOOKUP Lookup, QUIC_CONNECTION Connection)
        {
            int ReleaseRefCount = 0;
            CxPlatDispatchRwLockAcquireExclusive(Lookup.RwLock);
            while (Connection.SourceCids.Next != null)
            {
                QUIC_CID_HASH_ENTRY CID = CXPLAT_CONTAINING_RECORD<QUIC_CID_HASH_ENTRY>(CxPlatListPopEntry(Connection.SourceCids));
                if (CID.CID.IsInLookupTable)
                {
                    QuicLookupRemoveLocalCidInt(Lookup, CID);
                    CID.CID.IsInLookupTable = false;
                    ReleaseRefCount++;
                }
            }
            CxPlatDispatchRwLockReleaseExclusive(Lookup.RwLock);

            for (int i = 0; i < ReleaseRefCount; i++)
            {
                QuicConnRelease(Connection, QUIC_CONNECTION_REF.QUIC_CONN_REF_LOOKUP_TABLE);
            }
        }

        static QUIC_CONNECTION QuicLookupFindConnectionByLocalCid(QUIC_LOOKUP Lookup, byte[] CID, int CIDLen)
        {
            uint Hash = CxPlatHashSimple(CIDLen, CID);

            CxPlatDispatchRwLockAcquireShared(Lookup.RwLock);
            QUIC_CONNECTION ExistingConnection = QuicLookupFindConnectionByLocalCidInternal(
                    Lookup,
                    CID,
                    CIDLen,
                    Hash);

            if (ExistingConnection != null)
            {
                QuicConnAddRef(ExistingConnection, QUIC_CONNECTION_REF.QUIC_CONN_REF_LOOKUP_RESULT);
            }

            CxPlatDispatchRwLockReleaseShared(Lookup.RwLock);
            return ExistingConnection;
        }

        static bool QuicCidMatchConnection(QUIC_CONNECTION Connection, byte[] DestCid, int Length)
        {
            for (CXPLAT_SLIST_ENTRY Link = Connection.SourceCids.Next; Link != null; Link = Link.Next)
            {
                QUIC_CID_HASH_ENTRY Entry = CXPLAT_CONTAINING_RECORD<QUIC_CID_HASH_ENTRY>(Link);
                if (Length == Entry.CID.Length && (Length == 0 || orBufferEqual(DestCid, Entry.CID.Data, Length)))
                {
                    return true;
                }
            }
            return false;
        }

        static QUIC_CONNECTION QuicLookupFindConnectionByLocalCidInternal(QUIC_LOOKUP Lookup, byte[] CID, int CIDLen, uint Hash)
        {
            QUIC_CONNECTION Connection = null;

            if (Lookup.PartitionCount == 0)
            {
                if (Lookup.SINGLE.Connection != null && QuicCidMatchConnection(Lookup.SINGLE.Connection, CID, CIDLen))
                {
                    Connection = Lookup.SINGLE.Connection;
                }
            }
            else
            {
                NetLog.Assert(CIDLen >= QUIC_MIN_INITIAL_CONNECTION_ID_LENGTH);
                NetLog.Assert(CID != null);

                NetLog.Assert(QUIC_CID_PID_LENGTH == 2, "The code below assumes 2 bytes");
                int PartitionIndex = (ushort)EndianBitConverter.ToUInt16(CID, MsQuicLib.CidServerIdLength);

                PartitionIndex &= MsQuicLib.PartitionMask;
                PartitionIndex %= Lookup.PartitionCount;
                QUIC_PARTITIONED_HASHTABLE Table = Lookup.HASH.Tables[PartitionIndex];

                CxPlatDispatchRwLockAcquireShared(Table.RwLock);
                Connection = QuicHashLookupConnection(Table.Table, CID, CIDLen, Hash);
                CxPlatDispatchRwLockReleaseShared(Table.RwLock);
            }
            return Connection;
        }

        static QUIC_CONNECTION QuicHashLookupConnection(CXPLAT_HASHTABLE Table, byte[] DestCid, int Length, uint Hash)
        {
            CXPLAT_HASHTABLE_LOOKUP_CONTEXT Context;
            CXPLAT_HASHTABLE_ENTRY TableEntry = CxPlatHashtableLookup(Table, Hash, Context);

            while (TableEntry != null)
            {
                QUIC_CID_HASH_ENTRY CIDEntry = CXPLAT_CONTAINING_RECORD<QUIC_CID_HASH_ENTRY>(TableEntry);

                if (CIDEntry.CID.Length == Length && orBufferEqual(DestCid, CIDEntry.CID.Data, Length))
                {
                    return CIDEntry.Connection;
                }

                TableEntry = CxPlatHashtableLookupNext(Table, Context);
            }
            return null;
        }

        static QUIC_CONNECTION QuicLookupFindConnectionByRemoteHash(QUIC_LOOKUP Lookup, IPAddress RemoteAddress, int RemoteCidLength, byte[] RemoteCid)
        {
            uint Hash = QuicPacketHash(RemoteAddress, RemoteCidLength, RemoteCid);
            CxPlatDispatchRwLockAcquireShared(Lookup.RwLock);
            QUIC_CONNECTION ExistingConnection;
            if (Lookup.MaximizePartitioning)
            {
                ExistingConnection = QuicLookupFindConnectionByRemoteHashInternal(
                        Lookup,
                        RemoteAddress,
                        RemoteCidLength,
                        RemoteCid,
                        Hash);

                if (ExistingConnection != null)
                {
                    QuicConnAddRef(ExistingConnection,  QUIC_CONNECTION_REF.QUIC_CONN_REF_LOOKUP_RESULT);
                }
            }
            else
            {
                ExistingConnection = null;
            }

            CxPlatDispatchRwLockReleaseShared(Lookup.RwLock);
            return ExistingConnection;
        }

        static QUIC_CONNECTION QuicLookupFindConnectionByRemoteHashInternal(QUIC_LOOKUP Lookup, IPAddress RemoteAddress,
            int RemoteCidLength, byte[] RemoteCid, uint Hash)
        {
            CXPLAT_HASHTABLE_LOOKUP_CONTEXT Context = new CXPLAT_HASHTABLE_LOOKUP_CONTEXT();
            CXPLAT_HASHTABLE_ENTRY TableEntry = CxPlatHashtableLookup(Lookup.RemoteHashTable, Hash, Context);

            while (TableEntry != null)
            {
                QUIC_REMOTE_HASH_ENTRY Entry = CXPLAT_CONTAINING_RECORD<QUIC_REMOTE_HASH_ENTRY>(TableEntry);

                if (RemoteAddress == Entry.RemoteAddress && RemoteCidLength == Entry.RemoteCidLength &&
                    orBufferEqual(RemoteCid, Entry.RemoteCid, RemoteCidLength))
                {
                    return Entry.Connection;
                }

                TableEntry = CxPlatHashtableLookupNext(Lookup.RemoteHashTable, Context);
            }

            return null;
        }

        static void QuicLookupUninitialize(QUIC_LOOKUP Lookup)
        {
            NetLog.Assert(Lookup.CidCount == 0);
            if (Lookup.PartitionCount == 0)
            {
                NetLog.Assert(Lookup.SINGLE.Connection == null);
            }
            else
            {
                NetLog.Assert(Lookup.HASH.Tables != null);
                for (int i = 0; i < Lookup.PartitionCount; i++)
                {
                    QUIC_PARTITIONED_HASHTABLE Table = Lookup.HASH.Tables[i];
                    NetLog.Assert(Table.Table.NumEntries == 0);
                    CxPlatHashtableUninitialize(Table.Table);
                }
            }

            if (Lookup.MaximizePartitioning)
            {
                NetLog.Assert(Lookup.RemoteHashTable.NumEntries == 0);
                CxPlatHashtableUninitialize(Lookup.RemoteHashTable);
            }
        }

    }
}
