using AKNet.Common;
using System.Threading;

namespace AKNet.Udp5MSQuic.Common
{
    internal static partial class MSQuicFunc
    {
        public static ulong InterlockedIncrement64(ref ulong value)
        {
            long value2 = (long)value;
            value2 = Interlocked.Increment(ref value2);
            value = (ulong)value2;
            return value;
        }

        public static void CxPlatLockAcquire(object Lock)
        {
            Monitor.Enter(Lock);
        }

        public static void CxPlatLockRelease(object Lock)
        {
            Monitor.Enter(Lock);
        }

        static void CxPlatDispatchLockAcquire(object Lock)
        {
            Monitor.Enter(Lock);
        }

        static void CxPlatDispatchLockRelease(object Lock)
        {
            Monitor.Exit(Lock);
        }

        static void CxPlatDispatchRwLockAcquireShared(ReaderWriterLockSlim mLock)
        {
            mLock.EnterReadLock();
        }

        static void CxPlatDispatchRwLockReleaseShared(ReaderWriterLockSlim mLock)
        {
            mLock.ExitReadLock();
        }

        static void CxPlatDispatchRwLockAcquireExclusive(ReaderWriterLockSlim mLock)
        {
            mLock.EnterWriteLock();
        }

        static void CxPlatDispatchRwLockReleaseExclusive(ReaderWriterLockSlim mLock)
        {
            mLock.ExitWriteLock();
        }

        static bool InterlockedFetchAndSetBoolean(ref bool Target)
        {
            return InterlockedEx.Or(ref Target, true);
        }

        static bool InterlockedFetchAndClearBoolean(ref bool Target)
        {
            return InterlockedEx.And(ref Target, false);
        }
    }
}
