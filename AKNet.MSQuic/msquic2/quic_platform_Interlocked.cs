/************************************Copyright*****************************************
*        ProjectName:AKNet
*        Web:https://github.com/825126369/AKNet
*        Description:C#游戏网络库
*        Author:许珂
*        StartTime:2024/11/01 00:00:00
*        ModifyTime:2025/11/14 8:56:48
*        Copyright:MIT软件许可证
************************************Copyright*****************************************/
using AKNet.Common;
using System.Runtime.CompilerServices;
using System.Threading;

namespace MSQuic2
{
    internal static partial class MSQuicFunc
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void CxPlatLockAcquire(object Lock)
        {
            Monitor.Enter(Lock);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void CxPlatLockRelease(object Lock)
        {
            Monitor.Exit(Lock);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static void CxPlatDispatchLockAcquire(object Lock)
        {
            Monitor.Enter(Lock);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static void CxPlatDispatchLockRelease(object Lock)
        {
            Monitor.Exit(Lock);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static void CxPlatDispatchRwLockAcquireShared(ReaderWriterLockSlim mLock)
        {
            mLock.EnterReadLock();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static void CxPlatDispatchRwLockReleaseShared(ReaderWriterLockSlim mLock)
        {
            mLock.ExitReadLock();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static void CxPlatDispatchRwLockAcquireExclusive(ReaderWriterLockSlim mLock)
        {
            mLock.EnterWriteLock();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static void CxPlatDispatchRwLockReleaseExclusive(ReaderWriterLockSlim mLock)
        {
            mLock.ExitWriteLock();
        }

        //原子地获取一个布尔值，并将其设置为 false。
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static int InterlockedFetchAndClearBoolean(ref int Target)
        {
            return InterlockedEx.And(ref Target, 0);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static int InterlockedFetchAndSetBoolean(ref int Target)
        {
            return InterlockedEx.Or(ref Target, 1);
        }
    }
}
