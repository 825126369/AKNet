using AKNet.Common;
using System;
using System.Threading;

namespace AKNet.Udp5Quic.Common
{
    internal class CXPLAT_THREAD_CONFIG
    {
        public uint Flags;
        public int IdealProcessor;
        public string Name;
        public Action<QUIC_WORKER> Callback;
        public QUIC_WORKER Context;
    }

    internal class CXPLAT_SQE
    {
        public OVERLAPPED Overlapped;
        public CXPLAT_EVENT_COMPLETION_HANDLER Completion;
        public bool IsQueued;
    }

    internal static partial class MSQuicFunc
    {
        static void CxPlatRefInitialize(ref long RefCount)
        {
            RefCount = 1;
        }

        static void CxPlatRundownInitialize(CXPLAT_RUNDOWN_REF Rundown)
        {
            CxPlatRefInitialize(ref Rundown.RefCount);
            Rundown.RundownComplete = null;
            NetLog.Assert((Rundown).RundownComplete != null);
        }

        static int CxPlatProcCurrentNumber()
        {
            return Thread.CurrentThread.ManagedThreadId;
        }
    }
}
