using AKNet.Common;
using System;
using System.Runtime.CompilerServices;

namespace AKNet.Udp5MSQuic.Common
{
    internal struct QUIC_SUBRANGE
    {
        public ulong Low;
        public int Count;

        public bool IsEmpty
        {
            get
            {
                return Low == 0 && Count == 0;
            }
        }

        public static QUIC_SUBRANGE Empty => default;
    }

    internal struct QUIC_RANGE_SEARCH_KEY
    {
        public ulong Low;
        public ulong High;
    }

    internal class QUIC_RANGE
    {
        public QUIC_SUBRANGE[] SubRanges;
        public int UsedLength;
        public int AllocLength;
        public int MaxAllocSize;
        public readonly QUIC_SUBRANGE[] PreAllocSubRanges = new QUIC_SUBRANGE[MSQuicFunc.QUIC_RANGE_INITIAL_SUB_COUNT];
    }

    internal static partial class MSQuicFunc
    {
        public const int sizeof_QUIC_SUBRANGE = 16;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static bool IS_FIND_INDEX(int i)
        {
            return i >= 0;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static bool IS_INSERT_INDEX(int i)
        {
            return (i < 0);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static int FIND_INDEX_TO_INSERT_INDEX(int i)
        {
            return -(i + 1);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static int INSERT_INDEX_TO_FIND_INDEX(int i)
        {
            return -(i + 1);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static int QuicRangeSize(QUIC_RANGE Range)
        {
            return Range.UsedLength;
        }

        static QUIC_SUBRANGE QuicRangeGetSafe(QUIC_RANGE Range, int Index)
        {
            return Index < QuicRangeSize(Range) ? Range.SubRanges[Index] : QUIC_SUBRANGE.Empty;
        }

        static void QuicRangeInitialize(int MaxAllocSize, QUIC_RANGE Range)
        {
            Range.UsedLength = 0;
            Range.AllocLength = QUIC_RANGE_INITIAL_SUB_COUNT;
            Range.MaxAllocSize = MaxAllocSize;
            NetLog.Assert(sizeof_QUIC_SUBRANGE * QUIC_RANGE_INITIAL_SUB_COUNT <= MaxAllocSize);
            Range.SubRanges = Range.PreAllocSubRanges;
        }

        static void QuicRangeUninitialize(QUIC_RANGE Range)
        {
           
        }

        static QUIC_SUBRANGE QuicRangeGet(QUIC_RANGE Range, int Index)
        {
            return Range.SubRanges[Index];
        }

        static ulong QuicRangeGetHigh(QUIC_SUBRANGE Sub)
        {
            return Sub.Low + (ulong)(Sub.Count - 1);
        }

        static ulong QuicRangeGetMax(QUIC_RANGE Range)
        {
            return QuicRangeGetHigh(QuicRangeGet(Range, Range.UsedLength - 1));
        }

        static void QuicRangeReset(QUIC_RANGE Range)
        {
            Range.UsedLength = 0;
        }

        static bool QuicRangeGetMaxSafe(QUIC_RANGE Range, ref ulong Value)
        {
            if (Range.UsedLength > 0)
            {
                Value = QuicRangeGetMax(Range);
                return true;
            }
            return false;
        }

        //这里为将要插入的元素，腾出位置
        static bool QuicRangeGrow(QUIC_RANGE Range, int NextIndex)
        {
            if (Range.AllocLength == QUIC_MAX_RANGE_ALLOC_SIZE)
            {
                return false;
            }

            int NewAllocLength = Range.AllocLength * 2;
            int NewAllocSize = NewAllocLength * sizeof_QUIC_SUBRANGE;
            NetLog.Assert(NewAllocSize > sizeof_QUIC_SUBRANGE, "Range alloc arithmetic underflow.");
            if (NewAllocSize > Range.MaxAllocSize)
            {
                return false;
            }

            QUIC_SUBRANGE[] NewSubRanges = new QUIC_SUBRANGE[NewAllocLength];
            if (NewSubRanges == null)
            {
                return false;
            }

            NetLog.Assert(Range.SubRanges != null);
            if (NextIndex == 0)
            {
                for (int i = 0; i < Range.UsedLength; i++)
                {
                    NewSubRanges[i + 1] = Range.SubRanges[i];
                }
            }
            else if (NextIndex == Range.UsedLength)
            {
                for (int i = 0; i < Range.UsedLength; i++)
                {
                    NewSubRanges[i] = Range.SubRanges[i];
                }
            }
            else
            {
                for (int i = 0; i < NextIndex; i++)
                {
                    NewSubRanges[i] = Range.SubRanges[i];
                }

                for (int i = 0; i < Range.UsedLength - NextIndex; i++)
                {
                    NewSubRanges[i + NextIndex + 1] = Range.SubRanges[i + NextIndex];
                }
            }

            Range.SubRanges = NewSubRanges;
            Range.AllocLength = NewAllocLength;
            Range.UsedLength++;
            return true;
        }

        static QUIC_SUBRANGE QuicRangeMakeSpace(QUIC_RANGE Range, int Index)
        {
            NetLog.Assert(Index <= Range.UsedLength);
            if (Range.UsedLength == Range.AllocLength)
            {
                if (!QuicRangeGrow(Range, Index))
                {
                    if (Range.MaxAllocSize == QUIC_MAX_RANGE_ALLOC_SIZE || Index == 0)
                    {
                        return QUIC_SUBRANGE.Empty;
                    }

                    if (Index > 1)
                    {
                        for (int i = 0; i < Index - 1; i++)
                        {
                            Range.SubRanges[i] = Range.SubRanges[i + 1];
                            Range.SubRanges[i + 1] = QUIC_SUBRANGE.Empty;
                        }
                    }

                    Index--;
                }
            }
            else
            {
                NetLog.Assert(Range.SubRanges != null);
                if (Index == 0)
                {
                    for (int i = 0; i < Range.UsedLength; i++)
                    {
                        Range.SubRanges[i + 1] = Range.SubRanges[i];
                        Range.SubRanges[i] = QUIC_SUBRANGE.Empty;
                    }
                }
                else if (Index == Range.UsedLength)
                {

                }
                else
                {
                    for (int i = 0; i < Range.UsedLength - Index; i++)
                    {
                        Range.SubRanges[i + Index + 1] = Range.SubRanges[i + Index];
                        Range.SubRanges[i + Index] = QUIC_SUBRANGE.Empty;
                    }
                }
                Range.UsedLength++;
            }

            return Range.SubRanges[Index];
        }

        static QUIC_SUBRANGE QuicRangeAddRange(QUIC_RANGE Range, ulong Low, int Count, ref bool RangeUpdated)
        {
            int i;
            QUIC_SUBRANGE Sub;
            QUIC_RANGE_SEARCH_KEY Key = new QUIC_RANGE_SEARCH_KEY()
            {
                Low = Low,
                High = Low + (ulong)(Count - 1)
            };

            RangeUpdated = false;
            int result = QuicRangeSearch(Range, Key);
            if (IS_FIND_INDEX(result))
            {
                i = result;
                while (!(Sub = QuicRangeGetSafe(Range, i - 1)).IsEmpty && QuicRangeCompare(Key, Sub) == 0)
                {
                    --i;
                }
            }
            else
            {
                i = INSERT_INDEX_TO_FIND_INDEX(result);
            }

            if (!(Sub = QuicRangeGetSafe(Range, i - 1)).IsEmpty && Sub.Low + (ulong)Sub.Count == Low)
            {
                i--;
            }
            else
            {
                Sub = QuicRangeGetSafe(Range, i);
            }

            if (Sub.IsEmpty || Sub.Low > Low + (ulong)Count)
            {
                Sub = QuicRangeMakeSpace(Range, i);
                if (Sub.IsEmpty)
                {
                    return QUIC_SUBRANGE.Empty;
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
                    Sub.Count += (int)(Sub.Low - Low);
                    Sub.Low = Low;
                }
                if (Sub.Low + (ulong)Sub.Count < Low + (ulong)Count)
                {
                    RangeUpdated = true;
                    Sub.Count = (int)(Low + (ulong)Count - Sub.Low);
                }

                int j = i + 1;
                QUIC_SUBRANGE Next;
                while (!(Next = QuicRangeGetSafe(Range, j)).IsEmpty && Next.Low <= Low + (ulong)Count)
                {
                    if (Next.Low + (ulong)Next.Count > Sub.Low + (ulong)Sub.Count)
                    {
                        Sub.Count = (int)(Next.Low + (ulong)Next.Count - Sub.Low);
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
            if (Key.Low > QuicRangeGetHigh(Sub))
            {
                return 1;
            }
            return 0;
        }

#if QUIC_RANGE_USE_BINARY_SEARCH
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
                    Result = QuicRangeCompare(Key, QuicRangeGet(Range, Mid));
                    if (Result == 0)
                    {
                        return Mid;
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
                    Result = QuicRangeCompare(Key, QuicRangeGet(Range, Lo));
                    if (Result == 0)
                    {
                        return Lo;
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
#else
        static int QuicRangeSearch(QUIC_RANGE Range, QUIC_RANGE_SEARCH_KEY Key)
        {
            int Result; int i;
            for (i = QuicRangeSize(Range) - 1; i >= 0; i--)
            {
                QUIC_SUBRANGE Sub = QuicRangeGet(Range, i);
                if ((Result = QuicRangeCompare(Key, Sub)) == 0)
                {
                    return i;
                }
                else if (Result > 0)
                {
                    break;
                }
            }
            return FIND_INDEX_TO_INSERT_INDEX(i + 1);
        }
#endif

        static void QuicRangeSetMin(QUIC_RANGE Range, ulong Low)
        {
            int i = 0;
            QUIC_SUBRANGE Sub = QUIC_SUBRANGE.Empty;
            while (i < QuicRangeSize(Range))
            {
                Sub = QuicRangeGet(Range, i);
                if (Sub.Low >= Low)
                {
                    break;
                }

                if (QuicRangeGetHigh(Sub) >= Low)
                {
                    Sub.Count -= (int)(Low - Sub.Low);
                    Sub.Low = Low;
                    break;
                }
                i++;
            }

            if (i > 0)
            {
                QuicRangeRemoveSubranges(Range, 0, i);
            }
        }

        static bool QuicRangeAddValue(QUIC_RANGE Range, ulong Value)
        {
            bool DontCare = false;
            return !QuicRangeAddRange(Range, Value, 1, ref DontCare).IsEmpty;
        }

    }
}
