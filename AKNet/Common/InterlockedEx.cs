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

        public static bool Exchange(ref bool location1, bool value)
        {
            long l1 = (long)(location1 ? 1 : 0);
            long l2 = (long)(value ? 1 : 0);
            l2 = Interlocked.Exchange(ref l1, l2);
            location1 = l2 == 1;
            return location1;
        }

        public static bool And(ref bool location, bool value)
        {
            int value2 = value ? 1 : 0;
            fixed (void* ptr = &location)
            {
                int oldValue = And(ref MemoryMarshal.GetReference(new ReadOnlySpan<int>(ptr, 1)), value2);
                return oldValue > 0 ? true: false;
            }
        }

        //DotNet 9.0 拷贝过来的
        public static int And(ref int location1, int value)
        {
            int current = location1;
            while (true)
            {
                int newValue = current & value;
                int oldValue = Interlocked.CompareExchange(ref location1, newValue, current);
                if (oldValue == current)
                {
                    return oldValue;
                }
                current = oldValue;
            }
        }

        //DotNet 9.0 拷贝过来的
        public static int Or(ref int location1, int value)
        {
            int current = location1;
            while (true)
            {
                int newValue = current | value;
                int oldValue = Interlocked.CompareExchange(ref location1, newValue, current);
                if (oldValue == current)
                {
                    return oldValue;
                }
                current = oldValue;
            }
        }

        //DotNet 9.0 拷贝过来的
        public static long Or(ref long location1, long value)
        {
            long current = location1;
            while (true)
            {
                long newValue = current | value;
                long oldValue = Interlocked.CompareExchange(ref location1, newValue, current);
                if (oldValue == current)
                {
                    return oldValue;
                }
                current = oldValue;
            }
        }

        public static ulong Or(ref ulong location1, ulong value)
        {
            fixed (void* ptr = &location1)
            {
                return (ulong)InterlockedEx.Or(ref MemoryMarshal.GetReference(new ReadOnlySpan<long>(ptr, 1)), (long)value);
            }
        }

    }
}
