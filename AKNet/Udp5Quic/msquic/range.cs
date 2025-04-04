﻿using System.Drawing;
using System;
using System.Security.Cryptography;
using AKNet.Common;

namespace AKNet.Udp5Quic.Common
{
    internal struct QUIC_SUBRANGE
    {
       public long Low;
       public long Count;
    }

    internal struct QUIC_RANGE_SEARCH_KEY
    {
        public long Low;
        public long High;
    }

    internal class QUIC_RANGE
    {
        public QUIC_SUBRANGE[] SubRanges;
        public int UsedLength;
        public int AllocLength;
        public int MaxAllocSize;
        public QUIC_SUBRANGE[] PreAllocSubRanges = new QUIC_SUBRANGE[MSQuicFunc.QUIC_RANGE_INITIAL_SUB_COUNT];
    }

    internal static partial class MSQuicFunc
    {
        static bool IS_FIND_INDEX(int i)
        {
            return i >= 0;
        }

        static bool IS_INSERT_INDEX(int i)
        {
            return (i < 0);
        }

        static int FIND_INDEX_TO_INSERT_INDEX(int i)
        {
            return -i - 1;
        }

        static int INSERT_INDEX_TO_FIND_INDEX(int i)
        {
            return -(i + 1);
        }

        static int QuicRangeSize(QUIC_RANGE Range)
        {
            return Range.UsedLength;
        }

        static QUIC_SUBRANGE QuicRangeGetSafe(QUIC_RANGE Range, int Index)
        {
            return Index < QuicRangeSize(Range) ? Range.SubRanges[Index] : null;
        }

        static void QuicRangeInitialize(uint MaxAllocSize, QUIC_RANGE Range)
        {
            Range.UsedLength = 0;
            Range.AllocLength = QUIC_RANGE_INITIAL_SUB_COUNT;
            Range.MaxAllocSize = MaxAllocSize;
            Range.SubRanges = Range.PreAllocSubRanges;
        }

        static void QuicRangeUninitialize(QUIC_RANGE Range)
        {
            if (Range.AllocLength != QUIC_RANGE_INITIAL_SUB_COUNT)
            {
                
            }
        }

        static QUIC_SUBRANGE QuicRangeGet(QUIC_RANGE Range, int Index)
        {
            return Range.SubRanges[Index];
        }

        static long QuicRangeGetHigh(QUIC_SUBRANGE Sub)
        {
            return Sub.Low + Sub.Count - 1;
        }

        static long QuicRangeGetMax(QUIC_RANGE Range)
        {
            return QuicRangeGetHigh(QuicRangeGet(Range, Range.UsedLength - 1));
        }

        static bool QuicRangeGetMaxSafe(QUIC_RANGE Range, long Value)
        {
            if (Range.UsedLength > 0)
            {
                Value = QuicRangeGetMax(Range);
                return true;
            }
            return false;
        }

        static bool QuicRangeGrow(QUIC_RANGE Range, int NextIndex)
        {
            if (Range.AllocLength == QUIC_MAX_RANGE_ALLOC_SIZE)
            {
                return false;
            }

            int NewLength = Range.AllocLength << 1; // Grow by a factor of 2.
            QUIC_SUBRANGE[] NewSubRanges = new QUIC_SUBRANGE[NewLength];
            if (NewSubRanges == null)
            {
                return false;
            }

            NetLog.Assert(Range.SubRanges != null);
            if (NextIndex == 0)
            {
                memcpy(
                    NewSubRanges + 1,
                    Range.SubRanges,
                    Range.UsedLength);
            }
            else if (NextIndex == Range.UsedLength)
            {
                memcpy(
                    NewSubRanges,
                    Range->SubRanges,
                    Range->UsedLength * sizeof(QUIC_SUBRANGE));
            }
            else
            {
                memcpy(
                    NewSubRanges,
                    Range->SubRanges,
                    NextIndex * sizeof(QUIC_SUBRANGE));
                memcpy(
                    NewSubRanges + NextIndex + 1,
                    Range->SubRanges + NextIndex,
                    (Range->UsedLength - NextIndex) * sizeof(QUIC_SUBRANGE));
            }

            if (Range->AllocLength != QUIC_RANGE_INITIAL_SUB_COUNT)
            {
                CXPLAT_FREE(Range->SubRanges, QUIC_POOL_RANGE);
            }
            Range->SubRanges = NewSubRanges;
            Range->AllocLength = NewAllocLength;
            Range->UsedLength++; // For the next write index.

            return true;
        }

        static QUIC_SUBRANGE QuicRangeMakeSpace(QUIC_RANGE Range, int Index)
        {
            NetLog.Assert(Index <= Range.UsedLength);

            if (Range.UsedLength == Range.AllocLength)
            {
                if (!QuicRangeGrow(Range, Index))
                {
                    if (Range->MaxAllocSize == QUIC_MAX_RANGE_ALLOC_SIZE ||
                        *Index == 0)
                    {
                        return NULL;
                    }

                    if (*Index > 1)
                    {
                        memmove(
                            Range->SubRanges,
                            Range->SubRanges + 1,
                            (*Index - 1) * sizeof(QUIC_SUBRANGE));
                    }
                    (*Index)--; // Actually going to be inserting 1 before where requested.
                }
            }
            else
            {
                CXPLAT_DBG_ASSERT(Range->SubRanges != 0);
                if (*Index == 0)
                {
                    memmove(
                        Range->SubRanges + 1,
                        Range->SubRanges,
                        Range->UsedLength * sizeof(QUIC_SUBRANGE));
                }
                else if (*Index == Range->UsedLength)
                {
                    //
                    // No need to copy. Appending to the end.
                    //
                }
                else
                {
                    memmove(
                        Range->SubRanges + *Index + 1,
                        Range->SubRanges + *Index,
                        (Range->UsedLength - *Index) * sizeof(QUIC_SUBRANGE));
                }
                Range->UsedLength++; // For the new write.
            }

            return Range->SubRanges + *Index;
        }


        static QUIC_SUBRANGE QuicRangeAddRange(QUIC_RANGE Range, long Low, long Count, ref bool RangeUpdated)
        {
            int i;
            QUIC_SUBRANGE Sub;
            QUIC_RANGE_SEARCH_KEY Key = new QUIC_RANGE_SEARCH_KEY()
            { 
                Low = Low, 
                High = Low + Count - 1 
            };

            RangeUpdated = false;
            int result = QuicRangeSearch(Range, &Key);
            if (result >= 0)
            {
                i = result;
                while ((Sub = QuicRangeGetSafe(Range, i - 1)) != null && QuicRangeCompare(&Key, Sub) == 0)
                {
                    --i;
                }
            }
            else
            {
                i = INSERT_INDEX_TO_FIND_INDEX(result);
            }
            
            if ((Sub = QuicRangeGetSafe(Range, i - 1)) != null && Sub.Low + Sub.Count == Low)
            {
                i--;
            }
            else
            {
                Sub = QuicRangeGetSafe(Range, i);
            }

            if (Sub == null || Sub.Low > Low + Count)
            {
                Sub = QuicRangeMakeSpace(Range, i);
                if (Sub == null)
                {
                    return null;
                }

                Sub.Low = Low;
                Sub.Count = Count;
                RangeUpdated = true;

            }
            else
            {
                if (Sub.Low > Low)
                {
                    RangeUpdated = true;
                    Sub.Count += Sub.Low - Low;
                    Sub.Low = Low;
                }
                if (Sub.Low + Sub.Count < Low + Count)
                {
                    RangeUpdated = true;
                    Sub.Count = Low + Count - Sub.Low;
                }

                int j = i + 1;
                QUIC_SUBRANGE Next;
                while ((Next = QuicRangeGetSafe(Range, j)) != null && Next.Low <= Low + Count)
                {
                    if (Next.Low + Next.Count > Sub.Low + Sub.Count)
                    {
                        Sub.Count = Next.Low + Next.Count - Sub.Low;
                    }
                    j++;
                }

                int RemoveCount = j - (i + 1);
                if (RemoveCount != 0)
                {
                    if (QuicRangeRemoveSubranges(Range, i + 1, RemoveCount))
                    {
                        Sub = QuicRangeGet(Range, i);
                    }
                }
            }

            return Sub;
        }

        static bool QuicRangeRemoveSubranges(QUIC_RANGE Range, int Index, int Count)
        {
            NetLog.Assert(Count > 0);
            NetLog.Assert(Index + Count <= Range.UsedLength);

            if (Index + Count < Range.UsedLength)
            {
                for (int i = 0; i < Range.UsedLength - Index - Count; i++)
                {
                    Range.SubRanges[i + Index] = Range.SubRanges[ i + Index + Count];
                }
            }

            Range.UsedLength -= Count;

            if (Range.AllocLength >= QUIC_RANGE_INITIAL_SUB_COUNT * 2 && Range.UsedLength < Range.AllocLength / 4)
            {
                int NewAllocLength = Range.AllocLength / 2;
                QUIC_SUBRANGE[] NewSubRanges;
                if (NewAllocLength == QUIC_RANGE_INITIAL_SUB_COUNT)
                {
                    NewSubRanges = Range.PreAllocSubRanges;
                }
                else
                {
                    NewSubRanges = new QUIC_SUBRANGE[NewAllocLength];
                    if (NewSubRanges == null)
                    {
                        return false;
                    }
                }

                for (int i = 0; i < Range.UsedLength; i++)
                {
                    NewSubRanges[i].Low = Range.SubRanges[i].Low;
                    NewSubRanges[i].Count = Range.SubRanges[i].Count;
                }

                Range.SubRanges = NewSubRanges;
                Range.AllocLength = NewAllocLength;
                return true;
            }
            return false;
        }

        static int QuicRangeCompare(QUIC_RANGE_SEARCH_KEY Key, QUIC_SUBRANGE Sub)
        {
            if (Key.High < Sub.Low)
            {
                return -1;
            }
            if (QuicRangeGetHigh(Sub) < Key.Low)
            {
                return 1;
            }
            return 0;
        }

        static int QuicRangeSearch(QUIC_RANGE Range, QUIC_RANGE_SEARCH_KEY Key)
        {
            int Num = Range.UsedLength;
            int Lo = 0;
            int Hi = Range.UsedLength - 1;
            int Mid = 0;
            int Result = 0;

            while (Lo <= Hi)
            {
                int Half;
                if ((Half = Num / 2) != 0)
                {
                    Mid = Lo + (BoolOk(Num & 1) ? Half : (Half - 1));
                    if ((Result = QuicRangeCompare(Key, QuicRangeGet(Range, Mid))) == 0)
                    {
                        return (int)Mid;
                    }
                    else if (Result < 0)
                    {
                        Hi = Mid - 1;
                        Num = BoolOk(Num & 1) ? Half : Half - 1;
                    }
                    else
                    {
                        Lo = Mid + 1;
                        Num = Half;
                    }
                }
                else if (BoolOk(Num))
                {
                    if ((Result = QuicRangeCompare(Key, QuicRangeGet(Range, Lo))) == 0)
                    {
                        return (int)Lo;
                    }
                    else if (Result < 0)
                    {
                        return FIND_INDEX_TO_INSERT_INDEX(Lo);
                    }
                    else
                    {
                        return FIND_INDEX_TO_INSERT_INDEX(Lo + 1);
                    }
                }
                else
                {
                    break;
                }
            }

            return Result > 0 ? FIND_INDEX_TO_INSERT_INDEX(Mid + 1) : FIND_INDEX_TO_INSERT_INDEX(Mid);
        }

    }
}
