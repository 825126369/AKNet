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
    }
}
