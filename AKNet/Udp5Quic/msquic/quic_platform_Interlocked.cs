using System.Threading;

namespace AKNet.Udp5Quic.Common
{
    internal static partial class MSQuicFunc
    {
        static void CxPlatDispatchLockAcquire(object Lock)
        {
            Monitor.Enter(Lock);
        }

        static void CxPlatDispatchLockRelease(object Lock)
        {
            Monitor.Exit(Lock);
        }

        static bool InterlockedFetchAndClearBoolean(bool Target)
        {
            byte original;
            byte result;

            do
            {
                original = location;
                result = (byte)(original & mask);
            }
            while (Interlocked.CompareExchange(ref location, result, original) != original);

            return result;
        }

        public static byte InterlockedAnd8(ref byte location, byte mask)
        {
            byte original;
            byte result;

            do
            {
                original = location;
                result = (byte)(original & mask);
            }
            while (Interlocked.CompareExchange<byte>(ref (int)location, result, original) != original);
            return result;
        }
    }
}
