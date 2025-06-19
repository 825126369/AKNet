using System;
using System.Runtime.InteropServices;
using System.Threading;

namespace AKNet.Common
{
    internal unsafe static class InterlockedEx
    {
        public static ulong Increment(ref ulong location)
        {
            fixed (void* ptr = &location)
            {
                return (ulong)Interlocked.Increment(ref MemoryMarshal.GetReference<long>(new ReadOnlySpan<long>(ptr, 1)));
            }
        }

        public static ulong Add(ref ulong location, int count)
        {
            long l2 = (long)location;
            l2 = Interlocked.Add(ref l2, count);
            location = (ulong)l2;
            return location;
        }

        public static bool Read(ref bool value)
        {
            long l2 = (value ? 1 : 0);
            l2 = Interlocked.Read(ref l2);
            value = l2 == 1;
            return value;
        }

        public static bool Exchange(ref bool location1, bool value)
        {
            long l1 = (long)(location1 ? 1 : 0);
            long l2 = (long)(value ? 1 : 0);
            l2 = Interlocked.Exchange(ref l1, l2);
            location1 = l2 == 1;
            return location1;
        }

        public static bool Or(ref bool location1, bool value)
        {
            int t1 = location1 ? 1 : 0;
            int t2 = value ? 1 : 0;
            int t3 = Or(ref t1, t2);
            location1 = t3 == 1;
            return location1;
        }

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

        public static bool And(ref bool location1, bool value)
        {
            int t1 = location1 ? 1 : 0;
            int t2 = value ? 1 : 0;
            int t3 = And(ref t1, t2);
            return t3 == 1;
        }

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

        public static long And(ref long location1, long value)
        {
            long current = location1;
            while (true)
            {
                long newValue = current & value;
                long oldValue = Interlocked.CompareExchange(ref location1, newValue, current);
                if (oldValue == current)
                {
                    return oldValue;
                }
                current = oldValue;
            }
        }

    }
}
