using System;
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
        static public int CxPlatCurThreadID()
        {
            return Thread.CurrentThread.ManagedThreadId;
        }

        static void CxPlatEventInitialize(CXPLAT_EVENT Event, bool ManualReset, bool InitialState)
        {
            Event.AutoReset = !ManualReset;
            Event.Signaled = InitialState;
        }

        static void CxPlatEventUninitialize(CXPLAT_EVENT Event)
        {
            CxPlatInternalEventUninitialize(Event);
        }

        static void CxPlatEventWaitForever(CXPLAT_EVENT Event)
        {
            CxPlatInternalEventWaitForever(Event);
        }

        static void CxPlatInternalEventUninitialize(CXPLAT_EVENT Event)
        {
            
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

        static void CxPlatRefIncrement(ref long RefCount)
        {
            Interlocked.Increment(ref RefCount);
        }

        static bool CxPlatRefDecrement(ref long RefCount)
        {
            long NewValue = Interlocked.Decrement(ref RefCount);
            if (NewValue > 0)
            {
                return false;
            }
            else if (NewValue == 0)
            {
                Thread.MemoryBarrier();
                return true;
            }
            return false;
        }

        static bool CxPlatEventQEnqueue(int queue, CXPLAT_SQE sqe)
        {
            return eventfd_write(sqe.fd, 1) == 0;
        }

    }
}
