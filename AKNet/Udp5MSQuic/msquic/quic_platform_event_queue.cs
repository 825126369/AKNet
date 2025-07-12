using AKNet.Common;
using System;
using System.Threading;

namespace AKNet.Udp5MSQuic.Common
{
    internal class CXPLAT_EVENTQ
    {

    }

    internal class CXPLAT_SQE
    {
        public object contex;
        public SendOrPostCallback Completion;
#if DEBUG
        public bool IsQueued; // Debug flag to catch double queueing.
#endif
    }
    
    internal static partial class MSQuicFunc
    {
        static bool CxPlatEventQInitialize(CXPLAT_EVENTQ queue)
        {
            return true;
        }

        static bool CxPlatSqeInitialize(CXPLAT_EVENTQ queue, SendOrPostCallback completion, object contex, CXPLAT_SQE sqe)
        {
            sqe.contex = contex;
            sqe.Completion = completion;
            return true;
        }

        static object CxPlatCqeGetSqe(object cqe)
        {
            return (cqe as CXPLAT_SQE).contex;
        }

        static void CxPlatEventQEnqueue(CXPLAT_EVENTQ queue,CXPLAT_SQE sqe)
        {
#if DEBUG
            NetLog.Assert(!sqe.IsQueued);
#endif
            var context = SynchronizationContext.Current;
            context.Post(sqe.Completion, sqe.contex);
        }

        static void CxPlatSqeCleanup(CXPLAT_EVENTQ queue, CXPLAT_SQE sqe)
        {
           
        }

        static void CxPlatEventQCleanup(CXPLAT_EVENTQ queue)
        {
            
        }


    }
}
