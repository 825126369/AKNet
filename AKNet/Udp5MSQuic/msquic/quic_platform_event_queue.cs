using AKNet.Common;
using System;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;

namespace AKNet.Udp5MSQuic.Common
{
    internal class CXPLAT_EVENTQ
    {
        public IntPtr port = IntPtr.Zero;
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
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                //IntPtr INVALID_HANDLE_VALUE = new IntPtr(-1);
                //IntPtr port = Interop.Kernel32.CreateIoCompletionPort(INVALID_HANDLE_VALUE, IntPtr.Zero, UIntPtr.Zero, 1);
                //queue.port = port;
                return true;
            }

            return false;
        }

        //static unsafe bool CxPlatEventQAssociateHandle(CXPLAT_EVENTQ queue, IntPtr fileHandle)
        //{
        //    return queue.port == Interop.Kernel32.CreateIoCompletionPort(fileHandle, queue.port, UIntPtr.Zero, 0);
        //}

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
            Task.Run(()=>
            {
                sqe.Completion(sqe.contex);
            });
        }

        static void CxPlatSqeCleanup(CXPLAT_EVENTQ queue, CXPLAT_SQE sqe)
        {
           
        }

        static void CxPlatEventQCleanup(CXPLAT_EVENTQ queue)
        {
            
        }


    }
}
