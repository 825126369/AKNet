/************************************Copyright*****************************************
*        ProjectName:AKNet
*        Web:https://github.com/825126369/AKNet
*        Description:C#游戏网络库
*        Author:许珂
*        StartTime:2024/11/01 00:00:00
*        ModifyTime:2025/11/30 19:43:20
*        Copyright:MIT软件许可证
************************************Copyright*****************************************/
#if TARGET_WINDOWS
namespace AKNet.Platform
{
    using AKNet.Common;
    using System.Buffers;
    using System.Runtime.InteropServices;
    using CXPLAT_CQE = OVERLAPPED_ENTRY;

    [StructLayout(LayoutKind.Sequential)]
    public unsafe struct OVERLAPPED_ENTRY
    {
        public ulong lpCompletionKey;
        public OVERLAPPED* lpOverlapped;
        public ulong Internal;
        public int dwNumberOfBytesTransferred;
    }

    public unsafe struct OVERLAPPED
    {
        public ulong Internal;
        public ulong InternalHigh;

        [StructLayout(LayoutKind.Explicit)]
        public struct DUMMYUNIONNAME_DATA1
        {
            public struct DUMMYSTRUCTNAME_DATA2
            {
                public int Offset;
                public int OffsetHigh;
            }
            [FieldOffset(0)] public DUMMYSTRUCTNAME_DATA2 DUMMYUNIONNAME;
            [FieldOffset(0)] public void* Pointer;
        }
        public DUMMYUNIONNAME_DATA1 DUMMYUNIONNAME;
        public IntPtr hEvent;
    }

    public class CXPLAT_EVENTQ : IDisposable
    {
        internal bool _disposed;
        internal IntPtr Queue;
        public readonly Memory<CXPLAT_CQE> events = new CXPLAT_CQE[16];
        internal readonly MemoryHandle eventsMemoryHandle;
        public CXPLAT_EVENTQ()
        {
            _disposed = false;
            eventsMemoryHandle = events.Pin();
        }

        ~CXPLAT_EVENTQ()
        {
            Dispose();
        }

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;
            eventsMemoryHandle.Dispose();
            if (Queue != IntPtr.Zero)
            {
                Interop.Kernel32.CloseHandle(Queue);
                Queue = IntPtr.Zero;
            }
        }
    }

    public unsafe class CXPLAT_SQE:IDisposable
    {
        [StructLayout(LayoutKind.Sequential)]
        public unsafe struct CXPLAT_SQE_Inner
        {
            public const string Overlapped_FieldName = "Overlapped";
            public IntPtr parent;
#if DEBUG
            public bool IsQueued;
#endif
            public OVERLAPPED Overlapped;
        }

        public CXPLAT_SQE_Inner* sqePtr;
        public Action<CXPLAT_CQE> Completion; //这个主要是给外部程序使用的
        public object Contex;
        public CXPLAT_SQE()
        {
            sqePtr = (CXPLAT_SQE_Inner*)OSPlatformFunc.CxPlatAllocAndClear(sizeof(CXPLAT_SQE_Inner));
            GCHandle mGCHandle = GCHandle.Alloc(this, GCHandleType.Normal);
            NetLog.Assert(mGCHandle.IsAllocated);
            sqePtr->parent = GCHandle.ToIntPtr(mGCHandle);
        }

        ~CXPLAT_SQE()
        {
           Dispose();
        }

        public void Dispose()
        {
            if (sqePtr != null)
            {
                if (sqePtr->parent != IntPtr.Zero)
                {
                    GCHandle.FromIntPtr(sqePtr->parent).Free();
                    sqePtr->parent = IntPtr.Zero;
                }

                OSPlatformFunc.CxPlatFree(sqePtr);
                sqePtr = null;
            }
        }
    }

    public static unsafe partial class OSPlatformFunc
    {
        public static bool CxPlatEventQInitialize(CXPLAT_EVENTQ queue)
        {
            queue.Queue = Interop.Kernel32.CreateIoCompletionPort(new IntPtr(-1), IntPtr.Zero, IntPtr.Zero, 1);
            return queue.Queue != IntPtr.Zero;
        }

        public static bool CxPlatEventQAssociateHandle(CXPLAT_EVENTQ queue, IntPtr fileHandle)
        {
            return queue.Queue == Interop.Kernel32.CreateIoCompletionPort(fileHandle, queue.Queue, IntPtr.Zero, 0);
        }

        public static bool CxPlatEventQEnqueue(CXPLAT_EVENTQ queue, CXPLAT_SQE sqe)
        {
#if DEBUG
            NetLog.Assert(!sqe.sqePtr->IsQueued);
            //sqe.sqePtr->IsQueued;
#endif
            CxPlatZeroMemory(&sqe.sqePtr->Overlapped, sizeof(OVERLAPPED));
            return Interop.Kernel32.PostQueuedCompletionStatus(queue.Queue, 0, IntPtr.Zero, &sqe.sqePtr->Overlapped);
        }

        public static bool CxPlatEventQEnqueueEx(CXPLAT_EVENTQ queue, CXPLAT_SQE sqe, int num_bytes)
        {
#if DEBUG
            NetLog.Assert(!sqe.sqePtr->IsQueued);
           // sqe.sqePtr->IsQueued = true;
#endif
            CxPlatZeroMemory(&sqe.sqePtr->Overlapped, sizeof(OVERLAPPED));
            return Interop.Kernel32.PostQueuedCompletionStatus(queue.Queue, (uint)num_bytes, IntPtr.Zero, &sqe.sqePtr->Overlapped);
        }

        public static int CxPlatEventQDequeue(CXPLAT_EVENTQ queue, int wait_time)
        {
            int out_count = 0;
            queue.events.Span.Clear();
            if (!Interop.Kernel32.GetQueuedCompletionStatusEx(queue.Queue, (CXPLAT_CQE*)queue.eventsMemoryHandle.Pointer, queue.events.Length, out out_count, wait_time, false))
            {
                return 0;
            }

            NetLog.Assert(out_count != 0);
            NetLog.Assert(queue.events.Span[0].lpOverlapped != null || out_count == 1);
#if DEBUG
            if (queue.events.Span[0].lpOverlapped != null)
            {
                for (int i = 0; i < out_count; ++i)
                {
                    CXPLAT_SQE.CXPLAT_SQE_Inner* data = CXPLAT_CONTAINING_RECORD<CXPLAT_SQE.CXPLAT_SQE_Inner>(queue.events.Span[i].lpOverlapped, CXPLAT_SQE.CXPLAT_SQE_Inner.Overlapped_FieldName);
                    data->IsQueued = false;
                }
            }
            else
            {
                NetLog.LogError("queue.events[0].lpOverlapped == null");
            }
#endif
            return queue.events.Span[0].lpOverlapped == null ? 0 : out_count;
        }

        public static void CxPlatEventQReturn(CXPLAT_EVENTQ queue, int count)
        {

        }

        public static bool CxPlatSqeInitialize(CXPLAT_EVENTQ queue, Action<CXPLAT_CQE> completion, object contex, CXPLAT_SQE sqe)
        {
            sqe.Contex = contex;
            sqe.Completion = completion;
            CxPlatZeroMemory(&sqe.sqePtr->Overlapped, sizeof(OVERLAPPED));
#if DEBUG
            sqe.sqePtr->IsQueued = false;
#endif
            return true;
        }

        public static void CxPlatSqeInitializeEx(Action<CXPLAT_CQE> completion, object contex, CXPLAT_SQE sqe)
        {
            sqe.Contex = contex;
            sqe.Completion = completion;
            CxPlatZeroMemory(&sqe.sqePtr->Overlapped, sizeof(OVERLAPPED));
#if DEBUG
            sqe.sqePtr->IsQueued = false;
#endif
        }

        public static void CxPlatEventQCleanup(CXPLAT_EVENTQ queue)
        {
            queue.Dispose();
        }

        public static void CxPlatSqeCleanup(CXPLAT_EVENTQ queue, CXPLAT_SQE sqe)
        {
            sqe.Dispose();
        }

        public static CXPLAT_SQE CxPlatCqeGetSqe(CXPLAT_CQE cqe)
        {
            CXPLAT_SQE.CXPLAT_SQE_Inner* data = CXPLAT_CONTAINING_RECORD<CXPLAT_SQE.CXPLAT_SQE_Inner>(cqe.lpOverlapped,
                CXPLAT_SQE.CXPLAT_SQE_Inner.Overlapped_FieldName);
            return GCHandle.FromIntPtr(data->parent).Target as CXPLAT_SQE;
        }
    }
}
#endif