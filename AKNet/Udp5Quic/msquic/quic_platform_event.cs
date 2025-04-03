using AKNet.Common;
using System.Threading;

namespace AKNet.Udp5Quic.Common
{
    internal class CXPLAT_EVENT
    {
        public readonly object Mutex = new object();
        public int Cond;
        public bool Signaled;
        public bool AutoReset;
    }

    internal static partial class MSQuicFunc
    {
        static void CxPlatEventInitialize(CXPLAT_EVENT Event, bool ManualReset, bool InitialState)
        {
            Event.AutoReset = !ManualReset;
            Event.Signaled = InitialState;
        }

        static void CxPlatEventUninitialize(CXPLAT_EVENT Event)
        {
            CxPlatInternalEventUninitialize(Event);
        }

        static void CxPlatInternalEventUninitialize(CXPLAT_EVENT Event)
        {
            
        }

        static void CxPlatEventWaitForever(CXPLAT_EVENT Event)
        {
            CxPlatInternalEventWaitForever(Event);
        }

        static void CxPlatInternalEventWaitForever(CXPLAT_EVENT Event)
        {
            Monitor.Enter(Event.Mutex);
            while (!Event.Signaled)
            {
                Monitor.Wait(Event.Mutex);
            }
            if (Event.AutoReset)
            {
                Event.Signaled = false;
            }
            Monitor.Exit(Event.Mutex);
        }

        static void CxPlatEventWaitWithTimeout(CXPLAT_EVENT Event, int TimeoutMs)
        {
            Monitor.Enter(Event.Mutex);
            while (!Event.Signaled)
            {
                if(!Monitor.Wait(Event.Mutex, TimeoutMs))
                {
                    goto Exit;
                }
            }

            if (Event.AutoReset)
            {
                Event.Signaled = false;
            }
        Exit:
            Monitor.Exit(Event.Mutex);
        }

    }
}
