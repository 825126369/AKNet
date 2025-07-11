using System;
using System.Runtime.InteropServices;
using System.Threading;

namespace AKNet.Common
{
    //这个类需要测试，现在不能保证操作原子性的，也就是100个线程都执行 Increment（）操作：不能保证结果等于100
    public unsafe static class InterlockedEx
    {
        //返回值：递增后的值。
        public static ulong Increment(ref ulong location)
        {
            fixed (void* ptr = &location)
            {
                return (ulong)Interlocked.Increment(ref MemoryMarshal.GetReference(new ReadOnlySpan<long>(ptr, 1)));
            }
        }
    }
}
