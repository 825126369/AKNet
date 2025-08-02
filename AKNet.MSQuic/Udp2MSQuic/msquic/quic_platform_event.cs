using AKNet.Common;
using System.Runtime.CompilerServices;
using System.Threading;

namespace AKNet.Udp2MSQuic.Common
{
    internal static partial class MSQuicFunc
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static void CxPlatEventInitialize(out EventWaitHandle Event, bool ManualReset, bool InitialState)
        {
            if (ManualReset)
            {
                Event = new ManualResetEvent(InitialState);
            }
            else
            {
                Event = new AutoResetEvent(InitialState);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static void CxPlatEventUninitialize(EventWaitHandle Event)
        {
            Event.Close();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static void CxPlatEventSet(EventWaitHandle Event)
        {
            Event.Set();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static void CxPlatEventReset(EventWaitHandle Event)
        {
            Event.Reset();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static void CxPlatEventWaitForever(EventWaitHandle Event)
        {
            Event.WaitOne();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static bool CxPlatEventWaitWithTimeout(EventWaitHandle Event, int TimeoutMs)
        {
            NetLog.Assert(TimeoutMs != int.MaxValue);
            return Event.WaitOne(TimeoutMs);
        }

    }
}
