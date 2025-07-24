using AKNet.Common;
using System.Threading;

namespace AKNet.Udp1MSQuic.Common
{
    internal static partial class MSQuicFunc
    {
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

        static void CxPlatEventUninitialize(EventWaitHandle Event)
        {
            Event.Close();
        }

        static void CxPlatEventSet(EventWaitHandle Event)
        {
            Event.Set();
        }

        static void CxPlatEventReset(EventWaitHandle Event)
        {
            Event.Reset();
        }

        static void CxPlatEventWaitForever(EventWaitHandle Event)
        {
            Event.WaitOne();
        }

        static bool CxPlatEventWaitWithTimeout(EventWaitHandle Event, int TimeoutMs)
        {
            NetLog.Assert(TimeoutMs != int.MaxValue);
            return Event.WaitOne(TimeoutMs);
        }

    }
}
