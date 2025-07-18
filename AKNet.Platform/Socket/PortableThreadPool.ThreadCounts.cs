using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;

namespace AKNet.Platform.Socket
{
    internal sealed partial class PortableThreadPool
    {
        private struct ThreadCounts : IEquatable<ThreadCounts>
        {
            private const byte NumProcessingWorkShift = 0;
            private const byte NumExistingThreadsShift = 16;
            private const byte NumThreadsGoalShift = 32;
            private ulong _data;
            private ThreadCounts(ulong data) => _data = data;

            private short GetInt16Value(byte shift) => (short)(_data >> shift);
            private void SetInt16Value(short value, byte shift) =>
                _data = (_data & ~((ulong)ushort.MaxValue << shift)) | ((ulong)(ushort)value << shift);
            
            public short NumProcessingWork
            {
                get
                {
                    short value = GetInt16Value(NumProcessingWorkShift);
                    Debug.Assert(value >= 0);
                    return value;
                }
                set
                {
                    Debug.Assert(value >= 0);
                    SetInt16Value(Math.Max((short)0, value), NumProcessingWorkShift);
                }
            }
            
            public short NumExistingThreads
            {
                get
                {
                    short value = GetInt16Value(NumExistingThreadsShift);
                    Debug.Assert(value >= 0);
                    return value;
                }
                set
                {
                    Debug.Assert(value >= 0);
                    SetInt16Value(Math.Max((short)0, value), NumExistingThreadsShift);
                }
            }
            
            public short NumThreadsGoal
            {
                get
                {
                    short value = GetInt16Value(NumThreadsGoalShift);
                    Debug.Assert(value > 0);
                    return value;
                }
                set
                {
                    Debug.Assert(value > 0);
                    SetInt16Value(Math.Max((short)1, value), NumThreadsGoalShift);
                }
            }

            public ThreadCounts InterlockedSetNumThreadsGoal(short value)
            {
                ThreadPoolInstance._threadAdjustmentLock.VerifyIsLocked();

                ThreadCounts counts = this;
                while (true)
                {
                    ThreadCounts newCounts = counts;
                    newCounts.NumThreadsGoal = value;

                    ThreadCounts countsBeforeUpdate = InterlockedCompareExchange(newCounts, counts);
                    if (countsBeforeUpdate == counts)
                    {
                        return newCounts;
                    }

                    counts = countsBeforeUpdate;
                }
            }

            public ThreadCounts VolatileRead() => new ThreadCounts(Volatile.Read(ref _data));

            public ThreadCounts InterlockedCompareExchange(ThreadCounts newCounts, ThreadCounts oldCounts)
            {
#if DEBUG
                if (newCounts.NumThreadsGoal != oldCounts.NumThreadsGoal)
                {
                    ThreadPoolInstance._threadAdjustmentLock.VerifyIsLocked();
                }
#endif
                return new ThreadCounts(
                    (ulong)Interlocked.CompareExchange(
                        ref MemoryMarshal.GetReference<long>(MemoryMarshal.Cast<ulong, long>(MemoryMarshal.CreateSpan(ref _data, 1))), 
                    (long)newCounts._data, (long)oldCounts._data));
            }

            public static bool operator ==(ThreadCounts lhs, ThreadCounts rhs) => lhs._data == rhs._data;
            public static bool operator !=(ThreadCounts lhs, ThreadCounts rhs) => lhs._data != rhs._data;

            public override bool Equals([NotNullWhen(true)] object? obj) => obj is ThreadCounts other && Equals(other);
            public bool Equals(ThreadCounts other) => _data == other._data;
            public override int GetHashCode() => (int)_data + (int)(_data >> 32);
        }
    }
}
