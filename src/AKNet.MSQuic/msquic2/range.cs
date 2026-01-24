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
using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Text;
[assembly: InternalsVisibleTo("AKNet.MSTest")]
namespace MSQuic2
{
    internal struct QUIC_SUBRANGE
    {
        public const int sizeof_Length = 16;

        public long Low;
        public long Count;

        public QUIC_SUBRANGE()
        {
            Low = 0;
            Count = 0;
        }

        public bool IsEmpty
        {
            get { return Count == 0; }
        }

        public long High
        {
            get { return Low + Count - 1; }
        }

        public long End
        {
            get { return Low + Count; }
        }

        public static QUIC_SUBRANGE Empty => default;

        public override string ToString()
        {
            return $"Low: {Low}, High: {High}, Count: {Count}";
        }

        public override bool Equals(object? obj)
        {
            throw new NotSupportedException();
        }

        public override int GetHashCode()
        {
            throw new NotSupportedException();
        }

        public static bool operator ==(QUIC_SUBRANGE? left, QUIC_SUBRANGE? right)
        {
            if (left.HasValue && right.HasValue)
            {
                return left.Value.Low == right.Value.Low && left.Value.Count == right.Value.Count;
            }
            else if (left.HasValue)
            {
                return left.Value.IsEmpty;
            }
            else if (right.HasValue)
            {
                return right.Value.IsEmpty;
            }
            else
            {
                throw new NotSupportedException();
            }
        }

        public static bool operator !=(QUIC_SUBRANGE? left, QUIC_SUBRANGE? right)
        {
            return !(left == right);
        }
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
        public int MaxAllocSize;
        public readonly QUIC_SUBRANGE[] PreAllocSubRanges = new QUIC_SUBRANGE[MSQuicFunc.QUIC_RANGE_INITIAL_SUB_COUNT];

        public int AllocLength => SubRanges.Length;

        public override string ToString()
        {
            StringBuilder mBuilder = new StringBuilder();
            mBuilder.AppendLine($"----------------- QUIC_RANGE ------------------");
            mBuilder.AppendLine($"SubRanges UsedLength: {UsedLength} AllocLength:{AllocLength}");
            for (int i = 0; i < UsedLength; i++)
            {
                mBuilder.Append("[");
                mBuilder.Append(SubRanges[i].ToString());
                mBuilder.Append("]  ");
            }
            return mBuilder.ToString();
        }
    }

    internal static partial class MSQuicFunc
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IS_FIND_INDEX(int i)
        {
            return i >= 0;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IS_INSERT_INDEX(int i)
        {
            return (i < 0);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int FIND_INDEX_TO_INSERT_INDEX(int i)
        {
            return -i - 1;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int INSERT_INDEX_TO_FIND_INDEX(int i)
        {
            return (int)(uint)(-(i + 1));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int QuicRangeSize(QUIC_RANGE Range)
        {
            return Range.UsedLength;
        }

        public static QUIC_SUBRANGE QuicRangeGetSafe(QUIC_RANGE Range, int Index)
        {
            return (Index >= 0 && Index < QuicRangeSize(Range) ? Range.SubRanges[Index] : QUIC_SUBRANGE.Empty);
        }

        public static void QuicRangeInitialize(int MaxAllocSize, QUIC_RANGE Range)
        {
            Range.UsedLength = 0;
            Range.MaxAllocSize = MaxAllocSize;
            NetLog.Assert(QUIC_SUBRANGE.sizeof_Length * QUIC_RANGE_INITIAL_SUB_COUNT <= MaxAllocSize);
            Range.SubRanges = Range.PreAllocSubRanges;
        }

        public static void QuicRangeUninitialize(QUIC_RANGE Range)
        {
            if (Range.AllocLength != QUIC_RANGE_INITIAL_SUB_COUNT)
            {
                Range.SubRanges = null;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static QUIC_SUBRANGE QuicRangeGet(QUIC_RANGE Range, int Index)
        {
            return Range.SubRanges[Index];
        }

        public static long QuicRangeGetMax(QUIC_RANGE Range)
        {
            return QuicRangeGet(Range, Range.UsedLength - 1).High;
        }

        public static long QuicRangeGetMin(QUIC_RANGE Range)
        {
            return QuicRangeGet(Range, 0).Low;
        }

        public static void QuicRangeReset(QUIC_RANGE Range)
        {
            Range.UsedLength = 0;
        }

        public static bool QuicRangeGetMaxSafe(QUIC_RANGE Range, out long Value)
        {
            Value = 0;
            if (Range.UsedLength > 0)
            {
                Value = QuicRangeGetMax(Range);
                return true;
            }
            return false;
        }

        public static bool QuicRangeGetMinSafe(QUIC_RANGE Range, out long Value)
        {
            Value = 0;
            if (Range.UsedLength > 0)
            {
                Value = QuicRangeGetMin(Range);
                return true;
            }
            return false;
        }


        //这里为将要插入的元素，腾出位置
        public static bool QuicRangeGrow(QUIC_RANGE Range, int NextIndex)
        {
            if (Range.AllocLength == QUIC_MAX_RANGE_ALLOC_SIZE / QUIC_SUBRANGE.sizeof_Length)
            {
                return false;
            }

            int NewAllocLength = Range.AllocLength * 2;
            int NewAllocSize = NewAllocLength * QUIC_SUBRANGE.sizeof_Length;
            NetLog.Assert(NewAllocSize > QUIC_SUBRANGE.sizeof_Length, "Range alloc arithmetic underflow.");
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
                Range.SubRanges.AsSpan().Slice(0, Range.UsedLength).CopyTo(NewSubRanges.AsSpan().Slice(1));
            }
            else if (NextIndex == Range.UsedLength)
            {
                Range.SubRanges.AsSpan().Slice(0, Range.UsedLength).CopyTo(NewSubRanges);
            }
            else
            {
                Range.SubRanges.AsSpan().Slice(0, NextIndex).CopyTo(NewSubRanges);
                Range.SubRanges.AsSpan().Slice(NextIndex, Range.UsedLength - NextIndex).CopyTo(NewSubRanges.AsSpan().Slice(NextIndex + 1));
            }

            if (Range.AllocLength != QUIC_RANGE_INITIAL_SUB_COUNT)
            {
                Range.SubRanges = null;
            }

            Range.SubRanges = NewSubRanges;
            Range.UsedLength++;
            return true;
        }

        public static bool QuicRangeMakeSpace(QUIC_RANGE Range, ref int Index, out QUIC_SUBRANGE result)
        {
            NetLog.Assert(Index <= Range.UsedLength);
            if (Range.UsedLength == Range.AllocLength)
            {
                if (!QuicRangeGrow(Range, Index))
                {
                    if (Range.MaxAllocSize == QUIC_MAX_RANGE_ALLOC_SIZE || Index == 0)
                    {
                        result = QUIC_SUBRANGE.Empty;
                        return false;
                    }

                    if (Index > 1)
                    {
                        //如果达到最大UsedLength，那就去掉最老的块
                        Range.SubRanges.AsSpan().Slice(1, Index - 1).CopyTo(Range.SubRanges);
                    }

                    Index--;
                }
            }
            else
            {
                NetLog.Assert(Range.SubRanges != null);
                if (Index == 0)
                {
                    Range.SubRanges.AsSpan().Slice(0, Range.UsedLength).CopyTo(Range.SubRanges.AsSpan().Slice(1));
                }
                else if (Index == Range.UsedLength)
                {

                }
                else
                {
                    Range.SubRanges.AsSpan().Slice(Index, Range.UsedLength - Index).CopyTo(Range.SubRanges.AsSpan().Slice(Index + 1));
                }
                Range.UsedLength++;
            }
            
            result = Range.SubRanges[Index];
            return true;
        }

        public static QUIC_SUBRANGE QuicRangeAddRange(QUIC_RANGE Range, long Low, long Count, out bool RangeUpdated)
        {
            CommonFunc.AssertWithException(Low >= 0 && Low <= QUIC_VAR_INT_MAX);
            CommonFunc.AssertWithException(Count >= 1);

            int i;
            QUIC_SUBRANGE Sub;
            QUIC_RANGE_SEARCH_KEY Key = new QUIC_RANGE_SEARCH_KEY()
            {
                Low = Low,
                High = QuicRangeGetHighByLow(Low, Count)
            };

            RangeUpdated = false;
            int result = QuicRangeSearch(Range, Key);
            if (IS_FIND_INDEX(result))
            {
                i = result;
                while ((Sub = QuicRangeGetSafe(Range, i - 1)) != null && QuicRangeCompare(Key, Sub) == 0)
                {
                    --i;
                }
            }
            else
            {
                i = INSERT_INDEX_TO_FIND_INDEX(result);
            }

            Debug.Assert(i >= 0 && i <= Range.AllocLength);
            if ((Sub = QuicRangeGetSafe(Range, i - 1)) != null && Sub.Low + Sub.Count == Low) //可以和前面的合并
            {
                i--; //使用可以合并的索引
            }
            else
            {
                Sub = QuicRangeGetSafe(Range, i);
            }

            if (Sub == null || Sub.Low > Low + Count) //没有合并的可能了
            {
                if (!QuicRangeMakeSpace(Range, ref i, out Sub))
                {
                    return QUIC_SUBRANGE.Empty;
                }

                Sub.Low = Low;
                Sub.Count = Count;
                QuicRangeSet(Range, i, Sub);
                RangeUpdated = true;
            }
            else //找到可以合并的Sub了
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
                    if (Next.High > Sub.High)
                    {
                        Sub.Count = Next.High - Sub.Low + 1;
                    }
                    j++;
                }
                QuicRangeSet(Range, i, Sub);

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

        public static bool QuicRangeRemoveSubranges(QUIC_RANGE Range, int Index, int Count)
        {
            NetLog.Assert(Count > 0);
            NetLog.Assert(Index + Count <= Range.UsedLength);

            if (Index + Count < Range.UsedLength)
            {
                Range.SubRanges.AsSpan().Slice(Index + Count, Range.UsedLength - Index - Count).CopyTo(Range.SubRanges.AsSpan().Slice(Index));
            }
            
            Range.UsedLength -= Count;

            //下面就是当长度远远小于分配的总长度的时候，把分配的内存缩小一半。
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

                Range.SubRanges.AsSpan().Slice(0, Range.UsedLength).CopyTo(NewSubRanges);
                Range.SubRanges = NewSubRanges;
                return true;
            }
            return false;
        }

        public static bool QuicRangeRemoveRange(QUIC_RANGE Range, long Low, long Count)
        {
            int i;
            QUIC_SUBRANGE Sub = QUIC_SUBRANGE.Empty;
            QUIC_SUBRANGE Test;
            for (i = 0; (Test = QuicRangeGetSafe(Range, i)) != null; i++)
            {
                if (Test.Low < Low + Count && Test.Low + Test.Count > Low)
                {
                    Sub = Test;
                    break;
                }
            }

            if (Sub == null)
            {
                return true;
            }

            if (Sub.Low + Sub.Count > Low + Count && Sub.Low < Low)
            {
                QUIC_SUBRANGE NewSub;
                if (!QuicRangeMakeSpace(Range, ref i, out NewSub))
                {
                    return false;
                }
                NewSub = Sub;
                QuicRangeSet(Range, i, NewSub);
                Sub = NewSub;
            }

            if (Sub.Low < Low)
            {
                Sub.Count = Low - Sub.Low;
                QuicRangeSet(Range, i, Sub);
                Sub = QuicRangeGetSafe(Range, ++i);
            }

            int prev = i;
            while (Sub != null && Sub.Low >= Low && Sub.Low + Sub.Count <= Low + Count)
            {
                Sub = QuicRangeGetSafe(Range, ++i);
            }

            if (i > prev)
            {
                QuicRangeRemoveSubranges(Range, prev, i - prev);
                Sub = QuicRangeGetSafe(Range, prev);
            }

            if (Sub != null && Sub.Low < Low + Count && Sub.Low + Sub.Count > Low + Count)
            {
                Sub.Count -= (Low + Count - Sub.Low);
                Sub.Low = Low + Count;
                QuicRangeSet(Range, i, Sub);
            }
            return true;
        }

        public static int QuicRangeCompare(QUIC_RANGE_SEARCH_KEY Key, QUIC_SUBRANGE Sub)
        {
            if (Key.High < Sub.Low)
            {
                return -1;
            }
            if (Sub.High < Key.Low)
            {
                return 1;
            }
            return 0;
        }

#if QUIC_RANGE_USE_BINARY_SEARCH
        public static int QuicRangeSearch(QUIC_RANGE Range, QUIC_RANGE_SEARCH_KEY Key)
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
        public static int QuicRangeSearch(QUIC_RANGE Range, QUIC_RANGE_SEARCH_KEY Key)
        {
            int Result;
            int i;
            for (i = QuicRangeSize(Range); i > 0; i--)
            {
                QUIC_SUBRANGE Sub = QuicRangeGet(Range, i - 1);
                if ((Result = QuicRangeCompare(Key, Sub)) == 0) //有交集
                {
                    return i - 1;
                }
                else if (Result > 0)
                {
                    break;
                }
            }
            return FIND_INDEX_TO_INSERT_INDEX(i);
        }
#endif

        public static void QuicRangeSetMin(QUIC_RANGE Range, long Low)
        {
            CommonFunc.AssertWithException(Low >= 0 && Low <= QUIC_VAR_INT_MAX);

            int i = 0;
            while (i < QuicRangeSize(Range))
            {
                var Sub = QuicRangeGet(Range, i);
                if (Sub.Low >= Low)
                {
                    break;
                }

                if (Sub.High >= Low)
                {
                    Sub.Count -= Low - Sub.Low;
                    Sub.Low = Low;
                    QuicRangeSet(Range, i, Sub);
                    break;
                }
                i++;
            }

            if (i > 0)
            {
                QuicRangeRemoveSubranges(Range, 0, i);
            }
        }

        public static bool QuicRangeAddValue(QUIC_RANGE Range, long Value)
        {
            return QuicRangeAddRange(Range, Value, 1, out _) != null;
        }

        //-----------------------2026-01-15 xuke 加的实用方法---------------------
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void QuicRangeSet(QUIC_RANGE Range, int Index, QUIC_SUBRANGE t)
        {
            Range.SubRanges[Index] = t;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static long QuicRangeGetLowByHigh(long High, long nCount)
        {
            return High + 1 - nCount;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static long QuicRangeGetHighByLow(long Low, long nCount)
        {
            return Low + nCount - 1;
        }
    }
}
