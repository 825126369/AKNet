using AKNet.Common;
using AKNet.Udp5Quic.Common;
using System;
using System.Collections;

namespace AKNet.Udp5Quic.Common
{
    internal class CXPLAT_HASHTABLE_ENTRY
    {
        public CXPLAT_LIST_ENTRY Linkage;
        public ulong Signature;
    }

    internal class CXPLAT_HASHTABLE_LOOKUP_CONTEXT
    {
        public CXPLAT_LIST_ENTRY ChainHead;
        public CXPLAT_LIST_ENTRY PrevLinkage;
        public ulong Signature;
    }

    internal class CXPLAT_HASHTABLE_ENUMERATOR
    {
        public CXPLAT_HASHTABLE_ENTRY HashEntry;
        public CXPLAT_LIST_ENTRY CurEntry;
        public CXPLAT_LIST_ENTRY ChainHead;
        public uint BucketIndex;
    }

    internal class CXPLAT_HASHTABLE
    {
        public uint Flags;
        public int TableSize;
        public uint Pivot;
        public int DivisorMask;
        public uint NumEntries;
        public uint NonEmptyBuckets;
        public uint NumEnumerators;
        void* Directory;
        public CXPLAT_LIST_ENTRY[] SecondLevelDir;
        public CXPLAT_LIST_ENTRY[] FirstLevelDir;
    }

    internal static partial class MSQuicFunc
    {
        public const uint CXPLAT_HASH_ALLOCATED_HEADER = 0x00000001;
        public const uint CXPLAT_HASH_MIN_SIZE = 128;
        public const int CXPLAT_HASH_RESERVED_SIGNATURE = 0;
        public const int CXPLAT_HASH_ALT_SIGNATURE = CXPLAT_HASH_RESERVED_SIGNATURE + 1;
        public const int HT_FIRST_LEVEL_DIR_SIZE = 16;
        public const int HT_SECOND_LEVEL_DIR_SHIFT = 7;
        public const int HT_SECOND_LEVEL_DIR_MIN_SIZE = 1 << HT_SECOND_LEVEL_DIR_SHIFT;
        public const int MAX_HASH_TABLE_SIZE = ((1 << HT_FIRST_LEVEL_DIR_SIZE) - 1) * HT_SECOND_LEVEL_DIR_MIN_SIZE;
        public const int BASE_HASH_TABLE_SIZE = HT_SECOND_LEVEL_DIR_MIN_SIZE;
        public const int CXPLAT_HASHTABLE_MAX_RESIZE_ATTEMPTS = 1;
        public const int CXPLAT_HASHTABLE_MAX_CHAIN_LENGTH = 4;
        public const int CXPLAT_HASHTABLE_MAX_EMPTY_BUCKET_PERCENTAGE = 25;


        static bool CxPlatHashtableInitializeEx(int InitialSize, ref CXPLAT_HASHTABLE HashTable)
        {
            return CxPlatHashtableInitialize(InitialSize, ref HashTable);
        }

        static bool CxPlatHashtableInitialize(int InitialSize, ref CXPLAT_HASHTABLE HashTable)
        {
            if (!IS_POWER_OF_TWO(InitialSize) || (InitialSize > MAX_HASH_TABLE_SIZE) || (InitialSize < BASE_HASH_TABLE_SIZE))
            {
                return false;
            }

            uint LocalFlags = 0;
            CXPLAT_HASHTABLE Table;
            if (HashTable == null)
            {
                Table = new CXPLAT_HASHTABLE();
                if (Table == null)
                {
                    return false;
                }

                LocalFlags = CXPLAT_HASH_ALLOCATED_HEADER;
            }
            else
            {
                Table = HashTable;
            }

            Table.Flags = LocalFlags;
            Table.TableSize = InitialSize;
            Table.DivisorMask = Table.TableSize - 1;
            Table.Pivot = 0;

            if (Table.TableSize <= HT_SECOND_LEVEL_DIR_MIN_SIZE)
            {
                Table.SecondLevelDir = new CXPLAT_LIST_ENTRY[CxPlatComputeSecondLevelDirSize(0)];
                if (Table.SecondLevelDir == null)
                {
                    CxPlatHashtableUninitialize(Table);
                    return false;
                }

                CxPlatInitializeSecondLevelDir(Table.SecondLevelDir, Table.TableSize);
            }
            else
            {
                int FirstLevelIndex = 0, SecondLevelIndex = 0;
                CxPlatComputeDirIndices(Table.TableSize - 1, ref FirstLevelIndex, ref SecondLevelIndex);

                Table.FirstLevelDir = new CXPLAT_LIST_ENTRY[HT_FIRST_LEVEL_DIR_SIZE];
                if (Table.FirstLevelDir == null)
                {
                    CxPlatHashtableUninitialize(Table);
                    return false;
                }

                for (int i = 0; i <= FirstLevelIndex; i++)
                {
                    Table.FirstLevelDir[i] = new CXPLAT_LIST_ENTRY[CxPlatComputeSecondLevelDirSize(i)];
                    CXPLAT_ALLOC_NONPAGED(
                            CxPlatComputeSecondLevelDirSize(i) * sizeof(CXPLAT_LIST_ENTRY),
                            QUIC_POOL_HASHTABLE_MEMBER);
                    if (Table.FirstLevelDir[i] == null)
                    {
                        CxPlatHashtableUninitialize(Table);
                        return false;
                    }

                    CxPlatInitializeSecondLevelDir(
                        Table.FirstLevelDir[i],
                        (i < FirstLevelIndex)
                            ? CxPlatComputeSecondLevelDirSize(i)
                            : (SecondLevelIndex + 1));
                }
            }

            HashTable = Table;
            return true;
        }

        static void CxPlatInitializeSecondLevelDir(CXPLAT_LIST_ENTRY[] SecondLevelDir, int NumberOfBucketsToInitialize)
        {
            for (int i = 0; i < NumberOfBucketsToInitialize; i += 1)
            {
                CxPlatListInitializeHead(SecondLevelDir[i]);
            }
        }

        static uint CxPlatComputeSecondLevelDirSize(int FirstLevelIndex)
        {
            return (uint)(1 << (int)(FirstLevelIndex + HT_SECOND_LEVEL_DIR_SHIFT));
        }

        static uint CxPlatHashSimple(int Length, byte[] Buffer)
        {
            uint Hash = 5387;
            for (int i = 0; i < Length; ++i)
            {
                Hash = ((Hash << 5) - Hash) + Buffer[i];
            }
            return Hash;
        }

        static uint CxPlatGetBucketIndex(CXPLAT_HASHTABLE HashTable, ulong Signature)
        {
            uint BucketIndex = (uint)(Signature) & HashTable.DivisorMask;
            if (BucketIndex < HashTable.Pivot)
            {
                BucketIndex = ((uint)Signature) & ((HashTable.DivisorMask << 1) | 1);
            }
            return BucketIndex;
        }

        static byte CxPlatBitScanReverse(ref uint Index, ref uint Mask)
        {
            int ii = 0;
            if (Mask == 0 || Index == 0)
            {
                return 0;
            }

            for (ii = 32; ii >= 0; --ii)
            {
                uint TempMask = (uint)(1 << ii);

                if ((Mask & TempMask) != 0)
                {
                    Index = ii;
                    break;
                }
            }
            return (ii >= 0 ? (byte)1 : (byte)0);
        }

        static void CxPlatComputeDirIndices(int BucketIndex, ref uint FirstLevelIndex, ref uint SecondLevelIndex)
        {
            NetLog.Assert(BucketIndex < MAX_HASH_TABLE_SIZE);

            uint AbsoluteIndex = BucketIndex + HT_SECOND_LEVEL_DIR_MIN_SIZE;
            NetLog.Assert(AbsoluteIndex != 0);
            CxPlatBitScanReverse(FirstLevelIndex, AbsoluteIndex);
            SecondLevelIndex = (uint)(AbsoluteIndex ^ (1 << (int)FirstLevelIndex));
            FirstLevelIndex -= HT_SECOND_LEVEL_DIR_SHIFT;
            NetLog.Assert(FirstLevelIndex < HT_FIRST_LEVEL_DIR_SIZE);
        }

        static CXPLAT_LIST_ENTRY CxPlatGetChainHead(CXPLAT_HASHTABLE HashTable, uint BucketIndex)
        {
            uint SecondLevelIndex;
            CXPLAT_LIST_ENTRY SecondLevelDir;
            NetLog.Assert(BucketIndex < HashTable.TableSize);

            if (HashTable.TableSize <= HT_SECOND_LEVEL_DIR_MIN_SIZE)
            {
                SecondLevelDir = HashTable.SecondLevelDir;
                SecondLevelIndex = BucketIndex;
            }
            else
            {
                uint FirstLevelIndex = 0;
                CxPlatComputeDirIndices(BucketIndex, FirstLevelIndex, SecondLevelIndex);
                SecondLevelDir = *(HashTable.FirstLevelDir + FirstLevelIndex);
            }

            NetLog.Assert(SecondLevelDir != null);
            return SecondLevelDir + SecondLevelIndex;
        }

        static void CxPlatPopulateContext(CXPLAT_HASHTABLE HashTable, CXPLAT_HASHTABLE_LOOKUP_CONTEXT Context, ulong Signature)
        {
            uint BucketIndex = CxPlatGetBucketIndex(HashTable, Signature);

            CXPLAT_LIST_ENTRY BucketPtr = CxPlatGetChainHead(HashTable, BucketIndex);
            NetLog.Assert(null != BucketPtr);

            CXPLAT_LIST_ENTRY CurEntry = BucketPtr;
            while (CurEntry.Flink != BucketPtr)
            {
                CXPLAT_LIST_ENTRY NextEntry = CurEntry.Flink;
                CXPLAT_HASHTABLE_ENTRY NextHashEntry = CxPlatFlinkToHashEntry(NextEntry.Flink);

                if ((CXPLAT_HASH_RESERVED_SIGNATURE == NextHashEntry.Signature) || NextHashEntry.Signature < Signature)
                {

                    CurEntry = NextEntry;
                    continue;
                }

                break;
            }

            Context.ChainHead = BucketPtr;
            Context.PrevLinkage = CurEntry;
            Context.Signature = Signature;
        }

        static void CxPlatHashtableEnumerateBegin(CXPLAT_HASHTABLE HashTable, CXPLAT_HASHTABLE_ENUMERATOR Enumerator)
        {
            NetLog.Assert(Enumerator != null);

            CXPLAT_HASHTABLE_LOOKUP_CONTEXT LocalContext;
            CxPlatPopulateContext(HashTable, LocalContext, 0);
            HashTable.NumEnumerators++;

            if (CxPlatListIsEmpty(LocalContext.ChainHead))
            {
                HashTable.NonEmptyBuckets++;
            }

            CxPlatListInsertHead(LocalContext.ChainHead, Enumerator.HashEntry.Linkage);
            Enumerator.BucketIndex = 0;
            Enumerator.ChainHead = LocalContext.ChainHead;
            Enumerator.HashEntry.Signature = CXPLAT_HASH_RESERVED_SIGNATURE;
        }

        static CXPLAT_HASHTABLE_ENTRY CxPlatHashtableEnumerateNext(CXPLAT_HASHTABLE HashTable, CXPLAT_HASHTABLE_ENUMERATOR Enumerator)
        {
            NetLog.Assert(Enumerator != null);
            NetLog.Assert(Enumerator.ChainHead != null);
            NetLog.Assert(CXPLAT_HASH_RESERVED_SIGNATURE == Enumerator.HashEntry.Signature);

            for (uint i = Enumerator.BucketIndex; i < HashTable.TableSize; i++)
            {
                CXPLAT_LIST_ENTRY CurEntry;
                CXPLAT_LIST_ENTRY ChainHead;
                if (i == Enumerator.BucketIndex)
                {
                    CurEntry = Enumerator.HashEntry.Linkage;
                    ChainHead = Enumerator.ChainHead;
                }
                else
                {
                    ChainHead = CxPlatGetChainHead(HashTable, i);
                    CurEntry = ChainHead;
                }

                while (CurEntry.Flink != ChainHead)
                {

                    CXPLAT_LIST_ENTRY NextEntry = CurEntry.Flink;
                    CXPLAT_HASHTABLE_ENTRY NextHashEntry = CxPlatFlinkToHashEntry(NextEntry.Flink);
                    if (CXPLAT_HASH_RESERVED_SIGNATURE != NextHashEntry.Signature)
                    {
                        CxPlatListEntryRemove(Enumerator.HashEntry.Linkage);
                        NetLog.Assert(Enumerator.ChainHead != null);

                        if (Enumerator.ChainHead != ChainHead)
                        {
                            if (CxPlatListIsEmpty(Enumerator.ChainHead))
                            {
                                HashTable.NonEmptyBuckets--;
                            }

                            if (CxPlatListIsEmpty(ChainHead))
                            {
                                HashTable.NonEmptyBuckets++;
                            }
                        }

                        Enumerator.BucketIndex = i;
                        Enumerator.ChainHead = ChainHead;

                        CxPlatListInsertHead(NextEntry, Enumerator.HashEntry.Linkage);
                        return NextHashEntry;
                    }

                    CurEntry = NextEntry;
                }
            }

            return null;
        }

        static void CxPlatHashtableEnumerateEnd(CXPLAT_HASHTABLE HashTable, CXPLAT_HASHTABLE_ENUMERATOR Enumerator)
        {
            NetLog.Assert(Enumerator != null);
            NetLog.Assert(HashTable.NumEnumerators > 0);
            HashTable.NumEnumerators--;
            if (!CxPlatListIsEmpty(Enumerator.HashEntry.Linkage))
            {
                NetLog.Assert(Enumerator.ChainHead != null);

                CxPlatListEntryRemove(Enumerator.HashEntry.Linkage);

                if (CxPlatListIsEmpty(Enumerator.ChainHead))
                {
                    NetLog.Assert(HashTable.NonEmptyBuckets > 0);
                    HashTable.NonEmptyBuckets--;
                }
            }
            Enumerator.ChainHead = false;
        }

        static void CxPlatHashtableRemove(CXPLAT_HASHTABLE HashTable, CXPLAT_HASHTABLE_ENTRY Entry, CXPLAT_HASHTABLE_LOOKUP_CONTEXT Context)
        {
            ulong Signature = Entry.Signature;
            NetLog.Assert(HashTable.NumEntries > 0);
            HashTable.NumEntries--;

            if (Entry.Linkage.Flink == Entry.Linkage.Blink)
            {
                NetLog.Assert(HashTable.NonEmptyBuckets > 0);
                HashTable.NonEmptyBuckets--;
            }

            CxPlatListEntryRemove(Entry.Linkage);
            if (Context != null)
            {
                if (Context.ChainHead == null)
                {
                    CxPlatPopulateContext(HashTable, Context, Signature);
                }
                else
                {
                    NetLog.Assert(Signature == Context.Signature);
                }
            }
        }

        static CXPLAT_HASHTABLE_ENTRY CxPlatHashtableLookup(CXPLAT_HASHTABLE HashTable, ulong Signature, CXPLAT_HASHTABLE_LOOKUP_CONTEXT Context)
        {
            if (Signature == CXPLAT_HASH_RESERVED_SIGNATURE)
            {
                Signature = CXPLAT_HASH_ALT_SIGNATURE;
            }

            CXPLAT_HASHTABLE_LOOKUP_CONTEXT LocalContext = new CXPLAT_HASHTABLE_LOOKUP_CONTEXT();
            CXPLAT_HASHTABLE_LOOKUP_CONTEXT ContextPtr = (Context != null) ? Context : LocalContext;
            CxPlatPopulateContext(HashTable, ContextPtr, Signature);

            CXPLAT_LIST_ENTRY CurEntry = ContextPtr.PrevLinkage.Flink;
            if (ContextPtr.ChainHead == CurEntry)
            {
                return null;
            }

            CXPLAT_HASHTABLE_ENTRY CurHashEntry = CxPlatFlinkToHashEntry(CurEntry.Flink);
            NetLog.Assert(CXPLAT_HASH_RESERVED_SIGNATURE != CurHashEntry.Signature);
            if (CurHashEntry.Signature == Signature)
            {
                return CurHashEntry;
            }
            return null;
        }

        static CXPLAT_HASHTABLE_ENTRY CxPlatFlinkToHashEntry(CXPLAT_LIST_ENTRY FlinkPtr)
        {
            return CXPLAT_CONTAINING_RECORD<CXPLAT_HASHTABLE_ENTRY>(FlinkPtr);
        }

        static CXPLAT_HASHTABLE_ENTRY CxPlatHashtableLookupNext(CXPLAT_HASHTABLE HashTable, CXPLAT_HASHTABLE_LOOKUP_CONTEXT Context)
        {
            NetLog.Assert(null != Context);
            NetLog.Assert(null != Context.ChainHead);
            NetLog.Assert(Context.PrevLinkage.Flink != Context.ChainHead);

            CXPLAT_LIST_ENTRY CurEntry = Context.PrevLinkage.Flink;
            NetLog.Assert(CurEntry != Context.ChainHead);
            NetLog.Assert(CXPLAT_HASH_RESERVED_SIGNATURE != (CxPlatFlinkToHashEntry(CurEntry.Flink).Signature));

            if (CurEntry.Flink == Context.ChainHead)
            {
                return null;
            }

            CXPLAT_LIST_ENTRY NextEntry;
            CXPLAT_HASHTABLE_ENTRY NextHashEntry;
            if (HashTable.NumEnumerators == 0)
            {
                NextEntry = CurEntry.Flink;
                NextHashEntry = CxPlatFlinkToHashEntry(NextEntry.Flink);
            }
            else
            {
                NetLog.Assert(CurEntry.Flink != Context.ChainHead);
                NextHashEntry = null;
                while (CurEntry.Flink != Context.ChainHead)
                {
                    NextEntry = CurEntry.Flink;
                    NextHashEntry = CxPlatFlinkToHashEntry(NextEntry.Flink);

                    if (CXPLAT_HASH_RESERVED_SIGNATURE != NextHashEntry.Signature)
                    {
                        break;
                    }

                    CurEntry = NextEntry;
                }
            }

            NetLog.Assert(NextHashEntry != null);
            if (NextHashEntry.Signature == Context.Signature)
            {
                Context.PrevLinkage = CurEntry;
                return NextHashEntry;
            }

            return null;
        }

        static void CxPlatHashtableInsert(CXPLAT_HASHTABLE HashTable, CXPLAT_HASHTABLE_ENTRY Entry,
            ulong Signature, CXPLAT_HASHTABLE_LOOKUP_CONTEXT Context)
        {
            CXPLAT_HASHTABLE_LOOKUP_CONTEXT LocalContext;
            CXPLAT_HASHTABLE_LOOKUP_CONTEXT ContextPtr = null;

            if (Signature == CXPLAT_HASH_RESERVED_SIGNATURE)
            {
                Signature = CXPLAT_HASH_ALT_SIGNATURE;
            }

            Entry.Signature = Signature;
            HashTable.NumEntries++;

            if (Context == null)
            {
                CxPlatPopulateContext(HashTable, LocalContext, Signature);
                ContextPtr = LocalContext;
            }
            else
            {

                if (Context.ChainHead == null)
                {
                    CxPlatPopulateContext(HashTable, Context, Signature);
                }

                NetLog.Assert(Signature == Context.Signature);
                ContextPtr = Context;
            }

            NetLog.Assert(ContextPtr.ChainHead != null);

            if (CxPlatListIsEmpty(ContextPtr.ChainHead))
            {
                HashTable.NonEmptyBuckets++;
            }

            CxPlatListInsertHead(ContextPtr.PrevLinkage, Entry.Linkage);
            if (HashTable.NumEntries > CXPLAT_HASHTABLE_MAX_CHAIN_LENGTH * HashTable.NonEmptyBuckets)
            {
                int RestructAttempts = CXPLAT_HASHTABLE_MAX_RESIZE_ATTEMPTS;
                do
                {
                    if (!CxPlatHashTableExpand(HashTable))
                    {
                        break;
                    }

                    RestructAttempts--;

                } while ((RestructAttempts > 0) &&
                    (HashTable.NumEntries > CXPLAT_HASHTABLE_MAX_CHAIN_LENGTH * HashTable.NonEmptyBuckets));
            }
        }

        static bool CxPlatHashTableExpand(CXPLAT_HASHTABLE HashTable)
        {
            if (HashTable.TableSize == MAX_HASH_TABLE_SIZE)
            {
                return false;
            }

            if (HashTable.NumEnumerators > 0)
            {
                return false;
            }

            NetLog.Assert(HashTable.TableSize < MAX_HASH_TABLE_SIZE);
            int FirstLevelIndex = 0, SecondLevelIndex;

            CxPlatComputeDirIndices(HashTable.TableSize, FirstLevelIndex, SecondLevelIndex);

            CXPLAT_LIST_ENTRY SecondLevelDir;
            CXPLAT_LIST_ENTRY FirstLevelDir;
            if (HT_SECOND_LEVEL_DIR_MIN_SIZE == HashTable.TableSize)
            {
                SecondLevelDir = HashTable.SecondLevelDir;
                FirstLevelDir = CXPLAT_ALLOC_NONPAGED(
                        sizeof(CXPLAT_LIST_ENTRY) * HT_FIRST_LEVEL_DIR_SIZE,
                        QUIC_POOL_HASHTABLE_MEMBER);

                if (FirstLevelDir == null)
                {
                    return false;
                }

                FirstLevelDir[0] = SecondLevelDir;
                HashTable.FirstLevelDir = FirstLevelDir;
            }

            NetLog.Assert(HashTable.FirstLevelDir != null);
            FirstLevelDir = HashTable.FirstLevelDir;
            SecondLevelDir = FirstLevelDir[FirstLevelIndex];

            if (SecondLevelDir == null)
            {
                SecondLevelDir = CXPLAT_ALLOC_NONPAGED(
                        CxPlatComputeSecondLevelDirSize(FirstLevelIndex) * sizeof(CXPLAT_LIST_ENTRY),
                        QUIC_POOL_HASHTABLE_MEMBER);
                if (null == SecondLevelDir)
                {
                    if (HT_SECOND_LEVEL_DIR_MIN_SIZE == HashTable.TableSize)
                    {
                        NetLog.Assert(FirstLevelIndex == 1);
                        HashTable.SecondLevelDir = FirstLevelDir[0];
                        CXPLAT_FREE(FirstLevelDir, QUIC_POOL_HASHTABLE_MEMBER);
                    }

                    return false;
                }

                FirstLevelDir[FirstLevelIndex] = SecondLevelDir;
            }

            HashTable.TableSize++;
            CXPLAT_LIST_ENTRY ChainToBeSplit = CxPlatGetChainHead(HashTable, HashTable.Pivot);
            HashTable.Pivot++;

            CXPLAT_LIST_ENTRY NewChain = SecondLevelDir[SecondLevelIndex];
            CxPlatListInitializeHead(NewChain);

            if (!CxPlatListIsEmpty(ChainToBeSplit))
            {
                CXPLAT_LIST_ENTRY CurEntry = ChainToBeSplit;
                while (CurEntry.Flink != ChainToBeSplit)
                {
                    CXPLAT_LIST_ENTRY NextEntry = CurEntry.Flink;
                    CXPLAT_HASHTABLE_ENTRY NextHashEntry = CxPlatFlinkToHashEntry(NextEntry.Flink);

                    uint BucketIndex = (NextHashEntry.Signature) & ((HashTable.DivisorMask << 1) | 1);

                    NetLog.Assert((BucketIndex == (HashTable.Pivot - 1)) || (BucketIndex == (HashTable.TableSize - 1)));
                    if (BucketIndex == (HashTable.TableSize - 1))
                    {
                        CxPlatListEntryRemove(NextEntry);
                        CxPlatListInsertTail(NewChain, NextEntry);
                        continue;
                    }
                    CurEntry = NextEntry;
                }

                if (!CxPlatListIsEmpty(NewChain))
                {
                    HashTable.NonEmptyBuckets++;
                }

                if (CxPlatListIsEmpty(ChainToBeSplit))
                {
                    NetLog.Assert(HashTable.NonEmptyBuckets > 0);
                    HashTable.NonEmptyBuckets--;
                }
            }

            if (HashTable.Pivot == (HashTable.DivisorMask + 1))
            {
                HashTable.DivisorMask = (HashTable.DivisorMask << 1) | 1;
                HashTable.Pivot = 0;
                NetLog.Assert(0 == (HashTable.TableSize & (HashTable.TableSize - 1)));
            }

            return true;
        }

        static void CxPlatHashtableUninitialize(CXPLAT_HASHTABLE HashTable)
        {
            NetLog.Assert(HashTable.NumEnumerators == 0);
            NetLog.Assert(HashTable.NumEntries == 0);

            if (HashTable.TableSize <= HT_SECOND_LEVEL_DIR_MIN_SIZE)
            {
                if (HashTable.SecondLevelDir != null)
                {
                    HashTable.SecondLevelDir = null;
                }
            }
            else
            {
                if (HashTable.FirstLevelDir != null)
                {
                    uint FirstLevelIndex;
                    for (FirstLevelIndex = 0; FirstLevelIndex < HT_FIRST_LEVEL_DIR_SIZE; FirstLevelIndex++)
                    {
                        CXPLAT_LIST_ENTRY SecondLevelDir = HashTable.FirstLevelDir[FirstLevelIndex];
                        if (null == SecondLevelDir)
                        {
                            break;
                        }
                    }
                    HashTable.FirstLevelDir = null;
                }
            }
        }


    }
}
