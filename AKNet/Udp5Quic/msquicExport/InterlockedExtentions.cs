using System.Threading;

namespace AKNet.Udp5Quic.Common
{
    public static class InterlockedEx
    {
        public static ulong Increment(ref ulong location)
        {
            long l2 = (long)location;
            l2 = Interlocked.Increment(ref l2);
            return (ulong)l2;
        }

        public static ulong Add(ref ulong location, int count)
        {
            long l2 = (long)location;
            l2 = Interlocked.Add(ref l2, count);
            return (ulong)l2;
        }

        public static bool Read(ref bool value)
        {
            long l2 = (long)(value ? 1 : 0);
            l2= Interlocked.Read(ref l2);
            return l2 == 1;
        }

        public static bool Or(ref bool location1, bool value)
        {
            int t1 = location1 ? 1 : 0;
            int t2 = value ? 1 : 0;
            int t3 = Or(ref t1, t2);
            return t3 == 1;
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
    }
}
