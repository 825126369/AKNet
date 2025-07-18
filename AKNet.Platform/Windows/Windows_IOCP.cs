#if TARGET_WINDOWS

namespace AKNet.Platform
{
    using static AKNet.Platform.Interop.Kernel32;
    using CXPLAT_CQE = Interop.Kernel32.OVERLAPPED_ENTRY;
    using CXPLAT_EVENTQ = IntPtr;

    public static unsafe partial class OSPlatformFunc
    {
        internal delegate void CXPLAT_EVENT_COMPLETION_HANDLER(CXPLAT_CQE* Cqe);

        struct CXPLAT_SQE
        {
            public IntPtr Overlapped;
            public CXPLAT_EVENT_COMPLETION_HANDLER Completion;
#if DEBUG
            public bool IsQueued;
#endif
        }

        static bool CxPlatEventQInitialize(out CXPLAT_EVENTQ queue)
        {
            return (queue = Interop.Kernel32.CreateIoCompletionPort(new IntPtr(-1), IntPtr.Zero, 0, 1)) != IntPtr.Zero;
        }

        static void CxPlatEventQCleanup(CXPLAT_EVENTQ queue)
        {
            Interop.Kernel32.CloseHandle(queue);
        }

        static bool CxPlatEventQAssociateHandle(CXPLAT_EVENTQ queue, IntPtr fileHandle)
        {
            return queue == Interop.Kernel32.CreateIoCompletionPort(fileHandle, queue, IntPtr.Zero, 0);
        }

        static bool CxPlatEventQEnqueue(CXPLAT_EVENTQ queue, CXPLAT_SQE sqe)
        {
#if DEBUG
            NetLog.Assert(!sqe.IsQueued);
#endif
            CxPlatZeroMemory(sqe.Overlapped, sizeof(sqe.Overlapped));
            return PostQueuedCompletionStatus(queue, 0, IntPtr.Zero, sqe.Overlapped);
        }

        static bool CxPlatEventQEnqueueEx(CXPLAT_EVENTQ queue, CXPLAT_SQE sqe, int num_bytes)
        {
#if DEBUG
            NetLog.Assert(!sqe.IsQueued);
#endif
            CxPlatZeroMemory(sqe.Overlapped, sizeof(sqe.Overlapped));
            return PostQueuedCompletionStatus(queue, (uint)num_bytes, IntPtr.Zero, sqe.Overlapped);
        }

        static int CxPlatEventQDequeue(CXPLAT_EVENTQ queue, CXPLAT_CQE[] events, int count, int wait_time)
        {
            int out_count = 0;
            if (!Interop.Kernel32.GetQueuedCompletionStatusEx(queue, (OVERLAPPED_ENTRY*)&events, count, out out_count, wait_time, false))
            {
                return 0;
            }

            NetLog.Assert(out_count != 0);
            NetLog.Assert(events[0].lpOverlapped != null || out_count == 1);
#if DEBUG
            if (events[0].lpOverlapped != null)
            {
                for (int i = 0; i < out_count; ++i)
                {
                    CXPLAT_CONTAINING_RECORD<CXPLAT_SQE>(events[i].lpOverlapped, "Overlapped")->IsQueued = false;
                }
            }
#endif
            return events[0].lpOverlapped == null ? 0 : out_count;
        }

        static void CxPlatEventQReturn(CXPLAT_EVENTQ* queue, int count)
        {

        }


        static bool CxPlatSqeInitialize(CXPLAT_EVENTQ queue, CXPLAT_EVENT_COMPLETION_HANDLER completion, out CXPLAT_SQE sqe)
        {
            sqe = new CXPLAT_SQE();
            sqe.Completion = completion;
            return true;
        }

        static void CxPlatSqeInitializeEx(CXPLAT_EVENT_COMPLETION_HANDLER completion, out CXPLAT_SQE sqe)
        {
            sqe = new CXPLAT_SQE();
            sqe.Completion = completion;
            CxPlatZeroMemory(sqe.Overlapped, sizeof(sqe.Overlapped));
#if DEBUG
            sqe.IsQueued = false;
#endif
        }

        static void CxPlatSqeCleanup(CXPLAT_EVENTQ queue, CXPLAT_SQE* sqe)
        {

        }

        static CXPLAT_SQE CxPlatCqeGetSqe(CXPLAT_CQE* cqe)
        {
            return *CXPLAT_CONTAINING_RECORD<CXPLAT_SQE>(cqe.lpOverlapped, "Overlapped");
        }
    }
}
#endif