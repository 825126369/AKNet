using AKNet.Common;
using System;
using System.Threading.Tasks;

namespace AKNet.Udp5Quic.Common
{
    internal static partial class MSQuicFunc
    {
        public const int CXPLAT_HASH_RESERVED_SIGNATURE = 0;
        public const int CXPLAT_HASH_ALT_SIGNATURE = CXPLAT_HASH_RESERVED_SIGNATURE + 1;
        public const int HT_FIRST_LEVEL_DIR_SIZE  = 16;
        public const int HT_SECOND_LEVEL_DIR_SHIFT = 7;
        public const int HT_SECOND_LEVEL_DIR_MIN_SIZE = 1 << HT_SECOND_LEVEL_DIR_SHIFT;
        public const int MAX_HASH_TABLE_SIZE = ((1 << HT_FIRST_LEVEL_DIR_SIZE) - 1) * HT_SECOND_LEVEL_DIR_MIN_SIZE;
        public const int BASE_HASH_TABLE_SIZE = HT_SECOND_LEVEL_DIR_MIN_SIZE;
        public const int CXPLAT_HASHTABLE_MAX_RESIZE_ATTEMPTS = 1;
        public const int CXPLAT_HASHTABLE_MAX_CHAIN_LENGTH = 4;
        public const int CXPLAT_HASHTABLE_MAX_EMPTY_BUCKET_PERCENTAGE = 25;

        static uint CxPlatGetBucketIndex(CXPLAT_HASHTABLE HashTable, ulong Signature)
        {
            uint BucketIndex = (uint)(Signature) & HashTable.DivisorMask;
            if (BucketIndex < HashTable.Pivot)
            {
                BucketIndex = ((uint)Signature) & ((HashTable.DivisorMask << 1) | 1);
            }
            return BucketIndex;
        }

        static byte CxPlatBitScanReverse(uint Index, uint Mask)
        {
            int ii = 0;
            if (Mask == 0 || Index == 0)
            {
                return 0;
            }

            for (ii = sizeof(uint) * 8; ii >= 0; --ii)
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

        static void CxPlatComputeDirIndices(uint BucketIndex, ref uint FirstLevelIndex, ref uint SecondLevelIndex)
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
    }
}
