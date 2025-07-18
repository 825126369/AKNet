#if TARGET_WINDOWS

namespace AKNet.Platform
{
    using AKNet.Platform.Socket;
    using CXPLAT_CQE = Interop.Kernel32.OVERLAPPED_ENTRY;

    public struct CXPLAT_EVENTQ
    {
        internal IntPtr Queue;
    }

    public unsafe struct CXPLAT_SQE
    {
        internal Overlapped* Overlapped;
        public Action<object> Completion; //这个主要是给外部程序使用的
        public object Contex;
#if DEBUG
        public bool IsQueued;
#endif
    }

    public static unsafe partial class OSPlatformFunc
    {
        static bool CxPlatEventQInitialize(out CXPLAT_EVENTQ queue)
        {
            queue.Queue = Interop.Kernel32.CreateIoCompletionPort(new IntPtr(-1), IntPtr.Zero, 0, 1);
            return queue.Queue != IntPtr.Zero;
        }

        static void CxPlatEventQCleanup(CXPLAT_EVENTQ queue)
        {
            Interop.Kernel32.CloseHandle(queue.Queue);
        }

        static bool CxPlatEventQAssociateHandle(CXPLAT_EVENTQ queue, IntPtr fileHandle)
        {
            return queue.Queue == Interop.Kernel32.CreateIoCompletionPort(fileHandle, queue.Queue, IntPtr.Zero, 0);
        }

        static bool CxPlatEventQEnqueue(CXPLAT_EVENTQ queue, CXPLAT_SQE sqe)
        {
#if DEBUG
            NetLog.Assert(!sqe.IsQueued);
#endif
            CxPlatZeroMemory(sqe.Overlapped, sizeof(Overlapped));
            return Interop.Kernel32.PostQueuedCompletionStatus(queue.Queue, 0, IntPtr.Zero, sqe.Overlapped);
        }

        static bool CxPlatEventQEnqueueEx(CXPLAT_EVENTQ queue, CXPLAT_SQE sqe, int num_bytes)
        {
#if DEBUG
            NetLog.Assert(!sqe.IsQueued);
#endif
            CxPlatZeroMemory(sqe.Overlapped, sizeof(sqe.Overlapped));
            return Interop.Kernel32.PostQueuedCompletionStatus(queue.Queue, (uint)num_bytes, IntPtr.Zero, sqe.Overlapped);
        }

        static int CxPlatEventQDequeue(CXPLAT_EVENTQ queue, CXPLAT_CQE[] events, int count, int wait_time)
        {
            int out_count = 0;
            if (!Interop.Kernel32.GetQueuedCompletionStatusEx(queue.Queue, (CXPLAT_CQE*)&events, count, out out_count, wait_time, false))
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

        static void CxPlatSqeInitializeEx(Action<object> completion, object contex, out CXPLAT_SQE sqe)
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