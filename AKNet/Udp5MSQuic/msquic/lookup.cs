using AKNet.Common;
using System.Collections.Generic;
using System.Threading;

namespace AKNet.Udp5MSQuic.Common
{
    internal class QUIC_PARTITIONED_HASHTABLE 
    {
        public readonly ReaderWriterLockSlim RwLock = new ReaderWriterLockSlim();
        public readonly Dictionary<QUIC_CID, QUIC_CONNECTION> Table = null;

        public QUIC_PARTITIONED_HASHTABLE()
        {
           Table = new Dictionary<QUIC_CID, QUIC_CONNECTION>();
        }
    }

    internal class QUIC_LOOKUP
    {
        public bool MaximizePartitioning;
        public uint CidCount;
        public readonly ReaderWriterLockSlim RwLock = new ReaderWriterLockSlim();
        public int PartitionCount;
        public readonly SINGLE_DATA SINGLE = new SINGLE_DATA();
        public Dictionary<QUIC_CID, QUIC_CONNECTION> RemoteHashTable;
        public readonly HASH_DATA HASH = new HASH_DATA();
        public QUIC_LOOKUP LookupTable;

        public class SINGLE_DATA
        {
             public QUIC_CONNECTION Connection;
        }

        public class HASH_DATA
        {
             public QUIC_PARTITIONED_HASHTABLE[] Tables;
        }
    }

    internal static partial class MSQuicFunc
    {
        static void QuicLookupInitialize(QUIC_LOOKUP Lookup)
        {
            
        }

        static QUIC_CONNECTION QuicLookupFindConnectionByRemoteHash(QUIC_LOOKUP Lookup, QUIC_CID RemoteCid)
        {
            CxPlatDispatchRwLockAcquireShared(Lookup.RwLock);
            QUIC_CONNECTION ExistingConnection;
            if (Lookup.MaximizePartitioning)
            {
                ExistingConnection = QuicLookupFindConnectionByRemoteHashInternal(Lookup, RemoteCid);
                if (ExistingConnection != null)
                {
                    QuicConnAddRef(ExistingConnection, QUIC_CONNECTION_REF.QUIC_CONN_REF_LOOKUP_RESULT);
                }
            }
            else
            {
                ExistingConnection = null;
            }

            CxPlatDispatchRwLockReleaseShared(Lookup.RwLock);
            return ExistingConnection;
        }

        static QUIC_CONNECTION QuicLookupFindConnectionByRemoteHashInternal(QUIC_LOOKUP Lookup, QUIC_CID RemoteCid)
        {
            if (Lookup.RemoteHashTable.ContainsKey(RemoteCid))
            {
                return Lookup.RemoteHashTable[RemoteCid];
            }
            return null;
        }

        static QUIC_CONNECTION QuicLookupFindConnectionByRemoteAddr(QUIC_LOOKUP Lookup, QUIC_ADDR RemoteAddress)
        {
            QUIC_CONNECTION ExistingConnection = null;
            Lookup.RwLock.EnterReadLock();
            if (Lookup.PartitionCount == 0)
            {
                ExistingConnection = Lookup.SINGLE.Connection;
            }

            if (ExistingConnection != null)
            {
                QuicConnAddRef(ExistingConnection, QUIC_CONNECTION_REF.QUIC_CONN_REF_LOOKUP_RESULT);
            }
            Lookup.RwLock.ExitReadLock();
            return ExistingConnection;
        }

        static void QuicLookupRemoveRemoteHash(QUIC_LOOKUP Lookup, QUIC_CID RemoteCid)
        {
            QUIC_CONNECTION Connection = Lookup.RemoteHashTable[RemoteCid];
            NetLog.Assert(Lookup.MaximizePartitioning);

            QuicLibraryOnHandshakeConnectionRemoved();
            CxPlatDispatchRwLockAcquireExclusive(Lookup.RwLock);
            NetLog.Assert(Lookup.RemoteHashTable.ContainsKey(RemoteCid));
            Lookup.RemoteHashTable.Remove(RemoteCid);
            CxPlatDispatchRwLockReleaseExclusive(Lookup.RwLock);

            QuicConnRelease(Connection, QUIC_CONNECTION_REF.QUIC_CONN_REF_LOOKUP_TABLE);
        }

        static void QuicLookupRemoveLocalCids(QUIC_LOOKUP Lookup, QUIC_CONNECTION Connection)
        {
            int ReleaseRefCount = 0;
            CxPlatDispatchRwLockAcquireExclusive(Lookup.RwLock);
            while (!CxPlatListIsEmpty(Connection.SourceCids.Next))
            {
                QUIC_CID CID = CXPLAT_CONTAINING_RECORD<QUIC_CID>(CxPlatListRemoveHead(Connection.SourceCids));
                if (CID.IsInLookupTable)
                {
                    QuicLookupRemoveLocalCidInt(Lookup, CID);
                    CID.IsInLookupTable = false;
                    ReleaseRefCount++;
                }
            }
            CxPlatDispatchRwLockReleaseExclusive(Lookup.RwLock);

            for (int i = 0; i < ReleaseRefCount; i++)
            {
                QuicConnRelease(Connection, QUIC_CONNECTION_REF.QUIC_CONN_REF_LOOKUP_TABLE);
            }
        }

        static QUIC_CONNECTION QuicLookupFindConnectionByLocalCid(QUIC_LOOKUP Lookup, QUIC_CID CID)
        {
            CxPlatDispatchRwLockAcquireShared(Lookup.RwLock);
            QUIC_CONNECTION ExistingConnection = QuicLookupFindConnectionByLocalCidInternal(Lookup, CID);
            if (ExistingConnection != null)
            {
                QuicConnAddRef(ExistingConnection, QUIC_CONNECTION_REF.QUIC_CONN_REF_LOOKUP_RESULT);
            }
            CxPlatDispatchRwLockReleaseShared(Lookup.RwLock);
            return ExistingConnection;
        }

        static bool QuicCidMatchConnection(QUIC_CONNECTION Connection, QUIC_CID DestCid)
        {
            for (CXPLAT_LIST_ENTRY Link = Connection.SourceCids.Next; !CxPlatListIsEmpty(Link); Link = Link.Next)
            {
                QUIC_CID Entry = CXPLAT_CONTAINING_RECORD<QUIC_CID>(Link);

                NetLog.Log("QuicCidMatchConnection: " + Entry.ToString() + " : " + DestCid.ToString());
                if (orBufferEqual(DestCid.GetSpan(), Entry.GetSpan()))
                {
                    return true;
                }
            }
            return false;
        }

        static QUIC_CONNECTION QuicLookupFindConnectionByLocalCidInternal(QUIC_LOOKUP Lookup, QUIC_CID CID)
        {
            QUIC_CONNECTION Connection = null;

            if (Lookup.PartitionCount == 0)
            {
                if (Lookup.SINGLE.Connection != null && QuicCidMatchConnection(Lookup.SINGLE.Connection, CID))
                {
                    Connection = Lookup.SINGLE.Connection;
                }
            }
            else
            {
                NetLog.Assert(QUIC_CID_PID_LENGTH == 2, "The code below assumes 2 bytes");
                int PartitionIndex = (ushort)EndianBitConverter.ToUInt16(CID.GetSpan(), MsQuicLib.CidServerIdLength);

                PartitionIndex &= MsQuicLib.PartitionMask;
                PartitionIndex %= Lookup.PartitionCount;
                QUIC_PARTITIONED_HASHTABLE Table = Lookup.HASH.Tables[PartitionIndex];

                CxPlatDispatchRwLockAcquireShared(Table.RwLock);
                Connection = QuicHashLookupConnection(Table.Table, CID);
                CxPlatDispatchRwLockReleaseShared(Table.RwLock);
            }
            return Connection;
        }

        static QUIC_CONNECTION QuicHashLookupConnection(Dictionary<QUIC_CID, QUIC_CONNECTION> Table, QUIC_CID DestCid)
        {
            if (Table.ContainsKey(DestCid))
            {
                return Table[DestCid];
            }
            return null;
        }

        static void QuicLookupUninitialize(QUIC_LOOKUP Lookup)
        {
            NetLog.Assert(Lookup.CidCount == 0);
        }

        static bool QuicLookupAddRemoteHash(QUIC_LOOKUP Lookup, QUIC_CONNECTION Connection, QUIC_CID RemoteCid, ref QUIC_CONNECTION Collision)
        {
            bool Result;
            QUIC_CONNECTION ExistingConnection;

            CxPlatDispatchRwLockAcquireExclusive(Lookup.RwLock);
            if (Lookup.MaximizePartitioning)
            {
                ExistingConnection = QuicLookupFindConnectionByRemoteHashInternal(Lookup, RemoteCid);
                if (ExistingConnection == null)
                {
                    Result = QuicLookupInsertRemoteHash(Lookup, Connection, RemoteCid,true);
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

        static bool QuicLookupInsertRemoteHash(QUIC_LOOKUP Lookup, QUIC_CONNECTION Connection, QUIC_CID RemoteCid, bool UpdateRefCount)
        {
            Lookup.RemoteHashTable[RemoteCid] = Connection;
            QuicLibraryOnHandshakeConnectionAdded();

            if (UpdateRefCount)
            {
                QuicConnAddRef(Connection, QUIC_CONNECTION_REF.QUIC_CONN_REF_LOOKUP_TABLE);
            }
            return true;
        }

        static bool QuicLookupAddLocalCid(QUIC_LOOKUP Lookup, QUIC_CID SourceCid, out QUIC_CONNECTION Connection)
        {
            bool Result = false;
            Connection = null;
            QUIC_CONNECTION ExistingConnection;
            
            CxPlatDispatchRwLockAcquireExclusive(Lookup.RwLock);
            NetLog.Assert(!SourceCid.IsInLookupTable);
            ExistingConnection = QuicLookupFindConnectionByLocalCidInternal(Lookup, SourceCid);

            if (ExistingConnection == null)
            {
                Result = QuicLookupInsertLocalCid(Lookup, SourceCid, true);
                if (Connection != null)
                {
                    Connection = null;
                }
            }
            else
            {
                Result = false;
                if (Connection != null)
                {
                    Connection = ExistingConnection;
                    QuicConnAddRef(ExistingConnection, QUIC_CONNECTION_REF.QUIC_CONN_REF_LOOKUP_RESULT);
                }
            }
            CxPlatDispatchRwLockReleaseExclusive(Lookup.RwLock);
            return Result;
        }

        static bool QuicLookupInsertLocalCid(QUIC_LOOKUP Lookup, QUIC_CID Key, bool UpdateRefCount)
        {
            QUIC_CONNECTION Connection = Key.Connection;
            if (Lookup.PartitionCount == 0)
            {
                if (Lookup.SINGLE.Connection == null)
                {
                    Lookup.SINGLE.Connection = Connection;
                }
            }
            else
            {
                int PartitionIndex = EndianBitConverter.ToUInt16(Key.GetSpan(), MsQuicLib.CidServerIdLength);
                PartitionIndex &= MsQuicLib.PartitionMask;
                PartitionIndex %= Lookup.PartitionCount;
                QUIC_PARTITIONED_HASHTABLE Table = Lookup.HASH.Tables[PartitionIndex];

                CxPlatDispatchRwLockAcquireExclusive(Table.RwLock);
                Table.Table.Add(Key, Connection);
                CxPlatDispatchRwLockReleaseExclusive(Table.RwLock);
            }

            if (UpdateRefCount)
            {
                Lookup.CidCount++;
                QuicConnAddRef(Connection, QUIC_CONNECTION_REF.QUIC_CONN_REF_LOOKUP_TABLE);
            }
            return true;
        }

        static bool QuicLookupCreateHashTable(QUIC_LOOKUP Lookup, int PartitionCount)
        {
            NetLog.Assert(Lookup.LookupTable == null);
            NetLog.Assert(PartitionCount > 0);

            Lookup.HASH.Tables = new QUIC_PARTITIONED_HASHTABLE[PartitionCount];
            if (Lookup.HASH.Tables != null)
            {
                for (int i = 0; i < PartitionCount; i++)
                {
                    Lookup.HASH.Tables[i] = new QUIC_PARTITIONED_HASHTABLE();
                }
                Lookup.PartitionCount = PartitionCount;
            }

            return Lookup.HASH.Tables != null;
        }

        static void QuicLookupMaximizePartitioning(QUIC_LOOKUP Lookup)
        {
            CxPlatDispatchRwLockAcquireExclusive(Lookup.RwLock);
            if (!Lookup.MaximizePartitioning)
            {
                Lookup.RemoteHashTable = new Dictionary<QUIC_CID, QUIC_CONNECTION>();
                Lookup.MaximizePartitioning = true;
            }
            CxPlatDispatchRwLockReleaseExclusive(Lookup.RwLock);
        }

        static void QuicLookupRemoveLocalCid(QUIC_LOOKUP Lookup, QUIC_CID SourceCid)
        {
            CxPlatDispatchRwLockAcquireExclusive(Lookup.RwLock);
            QuicLookupRemoveLocalCidInt(Lookup, SourceCid);
            SourceCid.IsInLookupTable = false;
            CxPlatDispatchRwLockReleaseExclusive(Lookup.RwLock);
            QuicConnRelease(SourceCid.Connection, QUIC_CONNECTION_REF.QUIC_CONN_REF_LOOKUP_TABLE);
        }

        static void QuicLookupRemoveLocalCidInt(QUIC_LOOKUP Lookup, QUIC_CID SourceCid)
        {
            NetLog.Assert(SourceCid.IsInLookupTable);
            NetLog.Assert(Lookup.CidCount != 0);
            Lookup.CidCount--;

            if (Lookup.PartitionCount == 0)
            {
                if (Lookup.CidCount == 0)
                {
                    Lookup.SINGLE.Connection = null;
                }
            }
            else
            {
                NetLog.Assert(SourceCid.Data.Length >= MsQuicLib.CidServerIdLength + QUIC_CID_PID_LENGTH);
                NetLog.Assert(QUIC_CID_PID_LENGTH == 2, "The code below assumes 2 bytes");
                int PartitionIndex = EndianBitConverter.ToUInt16(SourceCid.Data.GetSpan(), MsQuicLib.CidServerIdLength);

                PartitionIndex &= MsQuicLib.PartitionMask;
                PartitionIndex %= Lookup.PartitionCount;
                QUIC_PARTITIONED_HASHTABLE Table = Lookup.HASH.Tables[PartitionIndex];

                CxPlatDispatchRwLockAcquireExclusive(Table.RwLock);
                Table.Table.Remove(SourceCid);
                CxPlatDispatchRwLockReleaseExclusive(Table.RwLock);
            }
        }

        static void QuicLookupMoveLocalConnectionIDs(QUIC_LOOKUP LookupSrc, QUIC_LOOKUP LookupDest, QUIC_CONNECTION Connection)
        {
            CXPLAT_LIST_ENTRY Entry = Connection.SourceCids.Next;
            CxPlatDispatchRwLockAcquireExclusive(LookupSrc.RwLock);
            while (Entry != null)
            {
                QUIC_CID CID = CXPLAT_CONTAINING_RECORD<QUIC_CID>(Entry);
                if (CID.IsInLookupTable)
                {
                    QuicLookupRemoveLocalCidInt(LookupSrc, CID);
                    QuicConnRelease(Connection, QUIC_CONNECTION_REF.QUIC_CONN_REF_LOOKUP_TABLE);
                }
                Entry = Entry.Next;
            }
            CxPlatDispatchRwLockReleaseExclusive(LookupSrc.RwLock);

            CxPlatDispatchRwLockAcquireExclusive(LookupDest.RwLock);
            Entry = Connection.SourceCids.Next;
            while (Entry != null)
            {
                QUIC_CID CID = CXPLAT_CONTAINING_RECORD<QUIC_CID>(Entry);
                if (CID.IsInLookupTable)
                {
                    bool Result = QuicLookupInsertLocalCid(LookupDest, CID, true);
                    NetLog.Assert(Result);
                }
                Entry = Entry.Next;
            }
            CxPlatDispatchRwLockReleaseExclusive(LookupDest.RwLock);
        }

    }
}
