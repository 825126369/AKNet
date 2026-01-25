/************************************Copyright*****************************************
*        ProjectName:AKNet
*        Web:https://github.com/825126369/AKNet
*        Description:C#游戏网络库
*        Author:许珂
*        StartTime:2024/11/01 00:00:00
*        ModifyTime:2025/11/30 19:43:18
*        Copyright:MIT软件许可证
************************************Copyright*****************************************/
using AKNet.Common;
using System.Collections.Generic;
using System.Net;
using System.Threading;

namespace MSQuic1
{
    internal class QUIC_PARTITIONED_HASHTABLE 
    {
        public readonly ReaderWriterLockSlim RwLock = new ReaderWriterLockSlim();
        public readonly Dictionary<QUIC_CID, QUIC_CONNECTION> Table = null;

        public QUIC_PARTITIONED_HASHTABLE()
        {
           Table = new Dictionary<QUIC_CID, QUIC_CONNECTION>(new QUIC_CID_EqualityComparer());
        }
    }

    internal class QUIC_LOOKUP
    {
        public struct SINGLE_DATA
        {
            public QUIC_CONNECTION Connection;
        }

        public struct HASH_DATA
        {
            public QUIC_PARTITIONED_HASHTABLE[] Tables;
        }

        public class LookupTable_DATA
        {
            public SINGLE_DATA SINGLE;
            public HASH_DATA HASH;
            public Dictionary<QUIC_CID, QUIC_CONNECTION> RemoteHashTable = null;

            public LookupTable_DATA()
            {
                RemoteHashTable = new Dictionary<QUIC_CID, QUIC_CONNECTION>(new QUIC_CID_EqualityComparer());
            }
        }

        public bool MaximizePartitioning;
        public uint CidCount;
        public readonly ReaderWriterLockSlim RwLock = new ReaderWriterLockSlim();
        public int PartitionCount;
        public LookupTable_DATA LookupTable = new LookupTable_DATA();
    }

    internal static partial class MSQuicFunc
    {
        public const int CXPLAT_HASH_MIN_SIZE = 128;
        static void QuicLookupInitialize(QUIC_LOOKUP Lookup)
        {
            
        }

        static QUIC_CONNECTION QuicLookupFindConnectionByRemoteHash(QUIC_LOOKUP Lookup, QUIC_CID RemoteCid)
        {
            CxPlatDispatchRwLockAcquireShared(Lookup.RwLock);
            QUIC_CONNECTION ExistingConnection;
            if (Lookup.MaximizePartitioning) //从现在的配置来看，只有是服务器，并且在握手阶段，会走这里
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
            if (Lookup.LookupTable.RemoteHashTable.ContainsKey(RemoteCid))
            {
                return Lookup.LookupTable.RemoteHashTable[RemoteCid];
            }
            return null;
        }

        static QUIC_CONNECTION QuicLookupFindConnectionByRemoteAddr(QUIC_LOOKUP Lookup, IPEndPoint RemoteAddress)
        {
            QUIC_CONNECTION ExistingConnection = null;
            Lookup.RwLock.EnterReadLock();
            if (Lookup.PartitionCount == 0)
            {
                ExistingConnection = Lookup.LookupTable.SINGLE.Connection;
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
            QUIC_CONNECTION Connection = Lookup.LookupTable.RemoteHashTable[RemoteCid];
            NetLog.Assert(Lookup.MaximizePartitioning);

            QuicLibraryOnHandshakeConnectionRemoved();
            CxPlatDispatchRwLockAcquireExclusive(Lookup.RwLock);
            NetLog.Assert(Lookup.LookupTable.RemoteHashTable.ContainsKey(RemoteCid));
            Lookup.LookupTable.RemoteHashTable.Remove(RemoteCid);
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
            for (CXPLAT_LIST_ENTRY Link = Connection.SourceCids.Next; Link != Connection.SourceCids; Link = Link.Next)
            {
                QUIC_CID Entry = CXPLAT_CONTAINING_RECORD<QUIC_CID>(Link);
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
                if (Lookup.LookupTable.SINGLE.Connection != null && QuicCidMatchConnection(Lookup.LookupTable.SINGLE.Connection, CID))
                {
                    Connection = Lookup.LookupTable.SINGLE.Connection;
                }
            }
            else
            {
                NetLog.Assert(CID.Data.Length >= QUIC_MIN_INITIAL_CONNECTION_ID_LENGTH, CID.Data.Length + " | " + QUIC_MIN_INITIAL_CONNECTION_ID_LENGTH);
                NetLog.Assert(QUIC_CID_PID_LENGTH == 2, "The code below assumes 2 bytes");

                int PartitionIndex = (ushort)EndianBitConverter.ToUInt16(CID.GetSpan(), MsQuicLib.CidServerIdLength);
                PartitionIndex &= MsQuicLib.PartitionMask;
                PartitionIndex %= Lookup.PartitionCount;
                QUIC_PARTITIONED_HASHTABLE Table = Lookup.LookupTable.HASH.Tables[PartitionIndex];

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
            Lookup.LookupTable.RemoteHashTable[RemoteCid] = Connection;
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
            if (!QuicLookupRebalance(Lookup, Key.Connection))
            {
                return false;
            }

            QUIC_CONNECTION Connection = Key.Connection;
            if (Lookup.PartitionCount == 0)
            {
                if (Lookup.LookupTable.SINGLE.Connection == null)
                {
                    Lookup.LookupTable.SINGLE.Connection = Connection;
                }
            }
            else
            {
                int PartitionIndex = EndianBitConverter.ToUInt16(Key.GetSpan(), MsQuicLib.CidServerIdLength);
                PartitionIndex &= MsQuicLib.PartitionMask;
                PartitionIndex %= Lookup.PartitionCount;
                QUIC_PARTITIONED_HASHTABLE Table = Lookup.LookupTable.HASH.Tables[PartitionIndex];

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
            NetLog.Assert(PartitionCount > 0);

            Lookup.LookupTable = new QUIC_LOOKUP.LookupTable_DATA();
            Lookup.LookupTable.HASH.Tables = new QUIC_PARTITIONED_HASHTABLE[PartitionCount];
            if (Lookup.LookupTable.HASH.Tables != null)
            {
                for (int i = 0; i < PartitionCount; i++)
                {
                    Lookup.LookupTable.HASH.Tables[i] = new QUIC_PARTITIONED_HASHTABLE();
                }
                Lookup.PartitionCount = PartitionCount;
            }

            return Lookup.LookupTable.HASH.Tables != null;
        }

        static void QuicLookupMaximizePartitioning(QUIC_LOOKUP Lookup)
        {
            CxPlatDispatchRwLockAcquireExclusive(Lookup.RwLock);
            if (!Lookup.MaximizePartitioning)
            {
                Lookup.LookupTable.RemoteHashTable = new Dictionary<QUIC_CID, QUIC_CONNECTION>(CXPLAT_HASH_MIN_SIZE);
                Lookup.MaximizePartitioning = true;
                QuicLookupRebalance(Lookup, null);
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
                    Lookup.LookupTable.SINGLE.Connection = null;
                }
            }
            else
            {
                NetLog.Assert(SourceCid.Data.Length >= MsQuicLib.CidServerIdLength + QUIC_CID_PID_LENGTH);
                NetLog.Assert(QUIC_CID_PID_LENGTH == 2, "The code below assumes 2 bytes");
                int PartitionIndex = EndianBitConverter.ToUInt16(SourceCid.Data.GetSpan(), MsQuicLib.CidServerIdLength);

                PartitionIndex &= MsQuicLib.PartitionMask;
                PartitionIndex %= Lookup.PartitionCount;
                QUIC_PARTITIONED_HASHTABLE Table = Lookup.LookupTable.HASH.Tables[PartitionIndex];

                CxPlatDispatchRwLockAcquireExclusive(Table.RwLock);
                Table.Table.Remove(SourceCid);
                CxPlatDispatchRwLockReleaseExclusive(Table.RwLock);
            }
        }

        static void QuicLookupMoveLocalConnectionIDs(QUIC_LOOKUP LookupSrc, QUIC_LOOKUP LookupDest, QUIC_CONNECTION Connection)
        {
            CXPLAT_LIST_ENTRY Entry = Connection.SourceCids.Next;
            CxPlatDispatchRwLockAcquireExclusive(LookupSrc.RwLock);
            while (Entry != Connection.SourceCids)
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
            while (Entry != Connection.SourceCids)
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

        static bool QuicLookupRebalance(QUIC_LOOKUP Lookup, QUIC_CONNECTION Connection)
        {
            int PartitionCount;
            if (Lookup.MaximizePartitioning)
            {
                PartitionCount = MsQuicLib.PartitionCount;

            }
            else if (Lookup.PartitionCount > 0 ||
                       (Lookup.PartitionCount == 0 &&
                        Lookup.LookupTable.SINGLE.Connection != null &&
                        Lookup.LookupTable.SINGLE.Connection != Connection))
            {
                PartitionCount = 1;
            }
            else
            {
                PartitionCount = 0;
            }

            if (PartitionCount > Lookup.PartitionCount)
            {
                int PreviousPartitionCount = Lookup.PartitionCount;
                
                QUIC_LOOKUP.LookupTable_DATA PreviousLookup = Lookup.LookupTable;
                NetLog.Assert(PartitionCount != 0);
                if (!QuicLookupCreateHashTable(Lookup, PartitionCount))
                {
                    return false;
                }

                if (PreviousPartitionCount == 0)
                {
                    if (PreviousLookup.SINGLE.Connection != null)
                    {
                        CXPLAT_LIST_ENTRY Entry = PreviousLookup.SINGLE.Connection.SourceCids.Next;
                        while (Entry != PreviousLookup.SINGLE.Connection.SourceCids)
                        {
                            QUIC_CID CID = CXPLAT_CONTAINING_RECORD<QUIC_CID>(Entry);
                            QuicLookupInsertLocalCid(Lookup, CID, false);
                            Entry = Entry.Next;
                        }
                    }
                }
                else
                {
                    QUIC_PARTITIONED_HASHTABLE[] PreviousTable = PreviousLookup.HASH.Tables;
                    for (int i = 0; i < PreviousPartitionCount; i++)
                    {
                        foreach (var v in PreviousTable[i].Table)
                        {
                            var Entry = v;
                            PreviousTable[i].Table.Remove(v.Key);
                            QuicLookupInsertLocalCid(Lookup, v.Key, false);
                        }
                    }
                }
            }
            return true;
        }

    }
}
