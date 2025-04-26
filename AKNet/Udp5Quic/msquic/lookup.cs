using AKNet.Common;
using System;
using System.Collections.Generic;
using System.Threading;

namespace AKNet.Udp5Quic.Common
{
    internal class QUIC_PARTITIONED_HASHTABLE 
    {
        public readonly ReaderWriterLockSlim RwLock = new ReaderWriterLockSlim();
        public readonly Dictionary<uint, QUIC_CID_HASH_ENTRY> Table = new Dictionary<uint, QUIC_CID_HASH_ENTRY>();
    }

    internal class QUIC_REMOTE_HASH_ENTRY
    {
        public QUIC_CONNECTION Connection;
        public QUIC_ADDR RemoteAddress;
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
        public Dictionary<uint, QUIC_REMOTE_HASH_ENTRY> RemoteHashTable;
        public HASH_Class HASH;
        public QUIC_LOOKUP LookupTable;

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
        static QUIC_CONNECTION QuicLookupFindConnectionByRemoteAddr(QUIC_LOOKUP Lookup, QUIC_ADDR RemoteAddress)
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

            uint Hash = QuicPacketHash(RemoteHashEntry.RemoteAddress, RemoteHashEntry.RemoteCidLength, RemoteHashEntry.RemoteCid);
            Lookup.RemoteHashTable.Remove(Hash);

            Connection.RemoteHashEntry = null;
            CxPlatDispatchRwLockReleaseExclusive(Lookup.RwLock);

            QuicConnRelease(Connection, QUIC_CONNECTION_REF.QUIC_CONN_REF_LOOKUP_TABLE);
        }

        static void QuicLookupRemoveLocalCidInt(QUIC_LOOKUP Lookup, QUIC_CID_HASH_ENTRY SourceCid)
        {
            //NetLog.Assert(SourceCid.CID.IsInLookupTable);
            //NetLog.Assert(Lookup.CidCount != 0);
            //Lookup.CidCount--;

            //if (Lookup.PartitionCount == 0)
            //{
            //    NetLog.Assert(Lookup.SINGLE.Connection == SourceCid.Connection);
            //    if (Lookup.CidCount == 0)
            //    {
            //        Lookup.SINGLE.Connection = null;
            //    }
            //}
            //else
            //{
            //    NetLog.Assert(SourceCid.CID.Length >= MsQuicLib.CidServerIdLength + QUIC_CID_PID_LENGTH);
            //    NetLog.Assert(QUIC_CID_PID_LENGTH == 2, "The code below assumes 2 bytes");
            //    int PartitionIndex;
            //    EndianBitConverter.SetBytes(SourceCid.CID.Data, MsQuicLib.CidServerIdLength, (ushort)PartitionIndex);

            //    PartitionIndex &= MsQuicLib.PartitionMask;
            //    PartitionIndex %= Lookup.PartitionCount;
            //    QUIC_PARTITIONED_HASHTABLE Table = Lookup.HASH.Tables[PartitionIndex];
            //    CxPlatDispatchRwLockAcquireExclusive(Table.RwLock);
            //    CxPlatHashtableRemove(Table.Table, SourceCid.Entry, null);
            //    CxPlatDispatchRwLockReleaseExclusive(Table.RwLock);
            //}
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

        static QUIC_CONNECTION QuicLookupFindConnectionByLocalCid(QUIC_LOOKUP Lookup, QUIC_BUFFER CID)
        {
            //uint Hash = CxPlatHashSimple(CIDLen, CID);

            //CxPlatDispatchRwLockAcquireShared(Lookup.RwLock);
            //QUIC_CONNECTION ExistingConnection = QuicLookupFindConnectionByLocalCidInternal(
            //        Lookup,
            //        CID,
            //        CIDLen,
            //        Hash);

            //if (ExistingConnection != null)
            //{
            //    QuicConnAddRef(ExistingConnection, QUIC_CONNECTION_REF.QUIC_CONN_REF_LOOKUP_RESULT);
            //}

            //CxPlatDispatchRwLockReleaseShared(Lookup.RwLock);
            // return ExistingConnection;
            return null;
        }

        static bool QuicCidMatchConnection(QUIC_CONNECTION Connection, byte[] DestCid, int Length)
        {
            //for (CXPLAT_SLIST_ENTRY Link = Connection.SourceCids.Next; Link != null; Link = Link.Next)
            //{
            //    QUIC_CID_HASH_ENTRY Entry = CXPLAT_CONTAINING_RECORD<QUIC_CID_HASH_ENTRY>(Link);
            //    if (Length == Entry.CID.Length && (Length == 0 || orBufferEqual(DestCid, Entry.CID.Data, Length)))
            //    {
            //        return true;
            //    }
            //}
            return false;
        }

        static QUIC_CONNECTION QuicLookupFindConnectionByLocalCidInternal(QUIC_LOOKUP Lookup, byte[] CID, int CIDLen, uint Hash)
        {
            QUIC_CONNECTION Connection = null;

            //if (Lookup.PartitionCount == 0)
            //{
            //    if (Lookup.SINGLE.Connection != null && QuicCidMatchConnection(Lookup.SINGLE.Connection, CID, CIDLen))
            //    {
            //        Connection = Lookup.SINGLE.Connection;
            //    }
            //}
            //else
            //{
            //    NetLog.Assert(CIDLen >= QUIC_MIN_INITIAL_CONNECTION_ID_LENGTH);
            //    NetLog.Assert(CID != null);

            //    NetLog.Assert(QUIC_CID_PID_LENGTH == 2, "The code below assumes 2 bytes");
            //    int PartitionIndex = (ushort)EndianBitConverter.ToUInt16(CID, MsQuicLib.CidServerIdLength);

            //    PartitionIndex &= MsQuicLib.PartitionMask;
            //    PartitionIndex %= Lookup.PartitionCount;
            //    QUIC_PARTITIONED_HASHTABLE Table = Lookup.HASH.Tables[PartitionIndex];

            //    CxPlatDispatchRwLockAcquireShared(Table.RwLock);
            //    Connection = QuicHashLookupConnection(Table.Table, CID, CIDLen, Hash);
            //    CxPlatDispatchRwLockReleaseShared(Table.RwLock);
            //}
            return Connection;
        }

        static QUIC_CONNECTION QuicHashLookupConnection(CXPLAT_HASHTABLE Table, byte[] DestCid, int Length, uint Hash)
        {
            //CXPLAT_HASHTABLE_LOOKUP_CONTEXT Context;
            //CXPLAT_HASHTABLE_ENTRY TableEntry = CxPlatHashtableLookup(Table, Hash, Context);

            //while (TableEntry != null)
            //{
            //    QUIC_CID_HASH_ENTRY CIDEntry = CXPLAT_CONTAINING_RECORD<QUIC_CID_HASH_ENTRY>(TableEntry);

            //    if (CIDEntry.CID.Length == Length && orBufferEqual(DestCid, CIDEntry.CID.Data, Length))
            //    {
            //        return CIDEntry.Connection;
            //    }

            //    TableEntry = CxPlatHashtableLookupNext(Table, Context);
            //}
            return null;
        }

        static QUIC_CONNECTION QuicLookupFindConnectionByRemoteHash(QUIC_LOOKUP Lookup, QUIC_ADDR RemoteAddress, QUIC_BUFFER RemoteCid)
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

        static QUIC_CONNECTION QuicLookupFindConnectionByRemoteHashInternal(QUIC_LOOKUP Lookup, QUIC_ADDR RemoteAddress,
            int RemoteCidLength, byte[] RemoteCid, uint Hash)
        {
            //CXPLAT_HASHTABLE_LOOKUP_CONTEXT Context = new CXPLAT_HASHTABLE_LOOKUP_CONTEXT();
            //CXPLAT_HASHTABLE_ENTRY TableEntry = CxPlatHashtableLookup(Lookup.RemoteHashTable, Hash, Context);

            //while (TableEntry != null)
            //{
            //    QUIC_REMOTE_HASH_ENTRY Entry = CXPLAT_CONTAINING_RECORD<QUIC_REMOTE_HASH_ENTRY>(TableEntry);

            //    if (RemoteAddress == Entry.RemoteAddress && RemoteCidLength == Entry.RemoteCidLength &&
            //        orBufferEqual(RemoteCid, Entry.RemoteCid, RemoteCidLength))
            //    {
            //        return Entry.Connection;
            //    }

            //    TableEntry = CxPlatHashtableLookupNext(Lookup.RemoteHashTable, Context);
            //}

            return null;
        }

        static void QuicLookupUninitialize(QUIC_LOOKUP Lookup)
        {
            //NetLog.Assert(Lookup.CidCount == 0);
            //if (Lookup.PartitionCount == 0)
            //{
            //    NetLog.Assert(Lookup.SINGLE.Connection == null);
            //}
            //else
            //{
            //    NetLog.Assert(Lookup.HASH.Tables != null);
            //    for (int i = 0; i < Lookup.PartitionCount; i++)
            //    {
            //        QUIC_PARTITIONED_HASHTABLE Table = Lookup.HASH.Tables[i];
            //        NetLog.Assert(Table.Table.NumEntries == 0);
            //        CxPlatHashtableUninitialize(Table.Table);
            //    }
            //}

            //if (Lookup.MaximizePartitioning)
            //{
            //    NetLog.Assert(Lookup.RemoteHashTable.NumEntries == 0);
            //    CxPlatHashtableUninitialize(Lookup.RemoteHashTable);
            //}
        }

        static bool QuicLookupAddRemoteHash(QUIC_LOOKUP Lookup, QUIC_CONNECTION Connection, QUIC_ADDR RemoteAddress, int RemoteCidLength,
                byte[] RemoteCid, ref QUIC_CONNECTION Collision)
        {
            bool Result;
            QUIC_CONNECTION ExistingConnection;
            uint Hash = QuicPacketHash(RemoteAddress, RemoteCidLength, RemoteCid);

            CxPlatDispatchRwLockAcquireExclusive(Lookup.RwLock);
            if (Lookup.MaximizePartitioning)
            {
                ExistingConnection = QuicLookupFindConnectionByRemoteHashInternal(
                        Lookup,
                        RemoteAddress,
                        RemoteCidLength,
                        RemoteCid,
                        Hash);

                if (ExistingConnection == null)
                {
                    Result = QuicLookupInsertRemoteHash(
                            Lookup,
                            Hash,
                            Connection,
                            RemoteAddress,
                            RemoteCidLength,
                            RemoteCid,
                            true);
                    Collision = null;
                }
                else
                {
                    Result = false;
                    Collision = ExistingConnection;
                    QuicConnAddRef(ExistingConnection, QUIC_CONNECTION_REF.QUIC_CONN_REF_LOOKUP_RESULT);
                }
            }
            else
            {
                Result = false;
                Collision = null;
            }

            CxPlatDispatchRwLockReleaseExclusive(Lookup.RwLock);
            return Result;
        }

        static bool QuicLookupInsertRemoteHash(QUIC_LOOKUP Lookup, uint Hash, QUIC_CONNECTION Connection, QUIC_ADDR RemoteAddress, int RemoteCidLength, byte[] RemoteCid, bool UpdateRefCount)
        {
            QUIC_REMOTE_HASH_ENTRY Entry = new QUIC_REMOTE_HASH_ENTRY();
            if (Entry == null)
            {
                return false;
            }

            Entry.Connection = Connection;
            Entry.RemoteAddress = RemoteAddress;
            Entry.RemoteCidLength = RemoteCidLength;
            Array.Copy(RemoteCid, Entry.RemoteCid, RemoteCidLength);

            Lookup.RemoteHashTable[Hash] = Entry;
            Connection.RemoteHashEntry = Entry;
            QuicLibraryOnHandshakeConnectionAdded();

            if (UpdateRefCount)
            {
                QuicConnAddRef(Connection, QUIC_CONNECTION_REF.QUIC_CONN_REF_LOOKUP_TABLE);
            }
            return true;
        }

        static bool QuicLookupAddLocalCid(QUIC_LOOKUP Lookup, QUIC_CID_HASH_ENTRY SourceCid,ref QUIC_CONNECTION Collision)
        {
            bool Result;
            //QUIC_CONNECTION ExistingConnection;
            //uint Hash = CxPlatHashSimple(SourceCid.CID.Length, SourceCid.CID.Data);


            //CxPlatDispatchRwLockAcquireExclusive(Lookup.RwLock);
            //NetLog.Assert(!SourceCid.CID.IsInLookupTable);
            //ExistingConnection = QuicLookupFindConnectionByLocalCidInternal(
            //        Lookup,
            //        SourceCid.CID.Data,
            //        SourceCid.CID.Length,
            //        Hash);

            //if (ExistingConnection == null)
            //{
            //    Result = QuicLookupInsertLocalCid(Lookup, Hash, SourceCid, true);
            //    if (Collision != null)
            //    {
            //        Collision = null;
            //    }
            //}
            //else
            //{
            //    Result = false;
            //    if (Collision != null)
            //    {
            //        Collision = ExistingConnection;
            //        QuicConnAddRef(ExistingConnection,  QUIC_CONNECTION_REF.QUIC_CONN_REF_LOOKUP_RESULT);
            //    }
            //}
            //CxPlatDispatchRwLockReleaseExclusive(Lookup.RwLock);

            return Result;
        }

        static bool QuicLookupInsertLocalCid(QUIC_LOOKUP Lookup, uint Hash, QUIC_CID_HASH_ENTRY SourceCid, bool UpdateRefCount)
        {
            //if (!QuicLookupRebalance(Lookup, SourceCid.Connection))
            //{
            //    return false;
            //}

            //if (Lookup.PartitionCount == 0)
            //{
            //    if (Lookup.SINGLE.Connection == null)
            //    {
            //        Lookup.SINGLE.Connection = SourceCid.Connection;
            //    }

            //}
            //else
            //{
            //    NetLog.Assert(SourceCid.CID.Length >= MsQuicLib.CidServerIdLength + QUIC_CID_PID_LENGTH);
            //    NetLog.Assert(QUIC_CID_PID_LENGTH == 2, "The code below assumes 2 bytes");

            //    int PartitionIndex = EndianBitConverter.ToUInt16(SourceCid.CID.Data, MsQuicLib.CidServerIdLength);

            //    PartitionIndex &= MsQuicLib.PartitionMask;
            //    PartitionIndex %= Lookup.PartitionCount;
            //    QUIC_PARTITIONED_HASHTABLE Table = Lookup.HASH.Tables[PartitionIndex];

            //    CxPlatDispatchRwLockAcquireExclusive(Table.RwLock);
            //    CxPlatHashtableInsert(Table.Table, SourceCid.Entry, Hash, null);
            //    CxPlatDispatchRwLockReleaseExclusive(Table.RwLock);
            //}

            //if (UpdateRefCount)
            //{
            //    Lookup.CidCount++;
            //    QuicConnAddRef(SourceCid.Connection, QUIC_CONNECTION_REF.QUIC_CONN_REF_LOOKUP_TABLE);
            //}

            //SourceCid.CID.IsInLookupTable = true;
            return true;
        }

        static bool QuicLookupRebalance(QUIC_LOOKUP Lookup, QUIC_CONNECTION Connection)
        {
            //int PartitionCount;
            //if (Lookup.MaximizePartitioning)
            //{
            //    PartitionCount = MsQuicLib.PartitionCount;
            //}
            //else if (Lookup.PartitionCount > 0 ||
            //           (Lookup.PartitionCount == 0 &&
            //            Lookup.SINGLE.Connection != null &&
            //            Lookup.SINGLE.Connection != Connection))
            //{
            //    PartitionCount = 1;

            //}
            //else
            //{
            //    PartitionCount = 0;
            //}

            //if (PartitionCount > Lookup.PartitionCount)
            //{
            //    int PreviousPartitionCount = Lookup.PartitionCount;
            //    QUIC_LOOKUP PreviousLookup = Lookup.LookupTable;
            //    Lookup.LookupTable = null;

            //    NetLog.Assert(PartitionCount != 0);
            //    if (!QuicLookupCreateHashTable(Lookup, PartitionCount))
            //    {
            //        Lookup.LookupTable = PreviousLookup;
            //        return false;
            //    }

            //    if (PreviousPartitionCount == 0)
            //    {
            //        if (PreviousLookup != null)
            //        {
            //            CXPLAT_SLIST_ENTRY Entry = ((QUIC_CONNECTION)PreviousLookup).SourceCids.Next;

            //            while (Entry != null)
            //            {
            //                QUIC_CID_HASH_ENTRY CID = CXPLAT_CONTAINING_RECORD<QUIC_CID_HASH_ENTRY>(Entry);
            //                QuicLookupInsertLocalCid(Lookup, CxPlatHashSimple(CID.CID.Length, CID.CID.Data), CID, false);
            //                Entry = Entry.Next;
            //            }
            //        }
            //    }
            //    else
            //    {
            //        QUIC_PARTITIONED_HASHTABLE PreviousTable = PreviousLookup;
            //        for (int i = 0; i < PreviousPartitionCount; i++)
            //        {
            //            CXPLAT_HASHTABLE_ENUMERATOR Enumerator;
            //            CxPlatHashtableEnumerateBegin(PreviousTable[i].Table, Enumerator);
            //            while (true)
            //            {
            //                CXPLAT_HASHTABLE_ENTRY Entry = CxPlatHashtableEnumerateNext(PreviousTable[i].Table, Enumerator);
            //                if (Entry == null)
            //                {
            //                    CxPlatHashtableEnumerateEnd(PreviousTable[i].Table, Enumerator);
            //                    break;
            //                }
            //                CxPlatHashtableRemove(PreviousTable[i].Table, Entry, null);

            //                QUIC_CID_HASH_ENTRY CID = CXPLAT_CONTAINING_RECORD<QUIC_CID_HASH_ENTRY>(Entry);
            //                QuicLookupInsertLocalCid(Lookup, CxPlatHashSimple(CID.CID.Length, CID.CID.Data), CID, false);
            //            }
            //            CxPlatHashtableUninitialize(PreviousTable[i].Table);
            //        }
            //    }
            //}

            return true;
        }

        static bool QuicLookupCreateHashTable(QUIC_LOOKUP Lookup, int PartitionCount)
        {
            NetLog.Assert(Lookup.LookupTable == null);
            NetLog.Assert(PartitionCount > 0);

            //Lookup.HASH.Tables = new QUIC_PARTITIONED_HASHTABLE[PartitionCount];
            //if (Lookup.HASH.Tables != null)
            //{
            //    int Cleanup = 0;
            //    bool Failed = false;
            //    for (int i = 0; i < PartitionCount; i++)
            //    {
            //        if (!CxPlatHashtableInitializeEx(CXPLAT_HASH_MIN_SIZE, ref Lookup.HASH.Tables[i].Table))
            //        {
            //            Cleanup = i;
            //            Failed = true;
            //            break;
            //        }
            //    }

            //    if (Failed)
            //    {
            //        for (int i = 0; i < Cleanup; i++)
            //        {
            //            CxPlatHashtableUninitialize(Lookup.HASH.Tables[i].Table);
            //        }
            //        Lookup.HASH.Tables = null;
            //    }
            //    else
            //    {
            //        Lookup.PartitionCount = PartitionCount;
            //    }
            //}

            return Lookup.HASH.Tables != null;
        }

        static bool QuicLookupMaximizePartitioning(QUIC_LOOKUP Lookup)
        {
            bool Result = true;
            CxPlatDispatchRwLockAcquireExclusive(Lookup.RwLock);
            //if (!Lookup.MaximizePartitioning)
            //{
            //    Result = CxPlatHashtableInitializeEx(CXPLAT_HASH_MIN_SIZE, ref Lookup.RemoteHashTable);
            //    if (Result)
            //    {
            //        Lookup.MaximizePartitioning = true;
            //        Result = QuicLookupRebalance(Lookup, null);
            //        if (!Result)
            //        {
            //            CxPlatHashtableUninitialize(Lookup.RemoteHashTable);
            //            Lookup.MaximizePartitioning = false;
            //        }
            //    }
            //}
            CxPlatDispatchRwLockReleaseExclusive(Lookup.RwLock);
            return Result;
        }

    }
}
