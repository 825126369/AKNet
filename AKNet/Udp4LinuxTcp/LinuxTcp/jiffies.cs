namespace AKNet.LinuxTcp
{
    internal static partial class LinuxTcpFunc
    {
        public static bool time_after(long a, long b)
        {
            return (long)((b) - (a)) < 0;
        }

        public static bool time_before(long a, long b)
        {
            return time_after(b, a);
        }

        public static bool time_after_eq(long a, long b)
        {
            return (long)((a) - (b)) >= 0;
        }

        public static bool time_before_eq(long a, long b)
        {
            return time_after_eq(b, a);
        }
    }
}
