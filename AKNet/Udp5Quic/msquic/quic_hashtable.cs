﻿using System;

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
        public uint TableSize;
        public uint Pivot;
        public uint DivisorMask;
        public uint NumEntries;
        public uint NonEmptyBuckets;
        public uint NumEnumerators;
        void* Directory;
        public CXPLAT_LIST_ENTRY SecondLevelDir;
        public CXPLAT_LIST_ENTRY[] FirstLevelDir;
    }

    internal static partial class MSQuicFunc
    {
        public const uint CXPLAT_HASH_ALLOCATED_HEADER = 0x00000001;

        static uint CxPlatHashSimple(int Length, byte[] Buffer)
        {
            uint Hash = 5387;
            for (int i = 0; i < Length; ++i)
            {
                Hash = ((Hash << 5) - Hash) + Buffer[i];
            }
            return Hash;
        }
    }

}
