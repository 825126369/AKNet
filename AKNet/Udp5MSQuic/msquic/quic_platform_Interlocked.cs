using AKNet.Common;
using System.Threading;

namespace AKNet.Udp5MSQuic.Common
{
    internal static partial class MSQuicFunc
    {
        public static void CxPlatLockAcquire(object Lock)
        {
            Monitor.Enter(Lock);
        }

        public static void CxPlatLockRelease(object Lock)
        {
            Monitor.Exit(Lock);
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

        //原子地获取一个布尔值，并将其设置为 false。
        static int InterlockedFetchAndClearBoolean(ref int Target)
        {
            return Interlocked.CompareExchange(ref Target, 0, Target);
        }

        static int InterlockedFetchAndSetBoolean(ref int Target)
        {
            return Interlocked.CompareExchange(ref Target, 1, Target);
        }
    }
}
