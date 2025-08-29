using AKNet.Common;
using AKNet.Platform;
using System;
using System.Diagnostics;
using System.Threading;

namespace AKNet.Udp1MSQuic.Common
{
    internal struct CXPLAT_PROCESSOR_GROUP_INFO
    {
        public ulong Mask;  // Bit mask of active processors in the group
        public int Count;  // Count of active processors in the group
        public int Offset; // Base process index offset this group starts at
    }

    internal class CXPLAT_THREAD
    {
        public IntPtr mThreadPtr;
        public LPTHREAD_START_ROUTINE mFunc;
        public CXPLAT_THREAD_CONFIG mConfig;
        public int ThreadFunc(IntPtr parm)
        {
             mConfig.Callback(mConfig.Context);
            return 0;
        }
    }

    internal static unsafe partial class MSQuicFunc
    {
        static QUIC_TRACE_RUNDOWN_CALLBACK QuicTraceRundownCallback;
        static CX_PLATFORM CxPlatform = new CX_PLATFORM();
        static int CxPlatProcessorCount;
        static long CxPlatTotalMemory;

        static void CxPlatSystemLoad()
        {

        }

        public static int CxPlatInitialize()
        {
            int Status = 0;
            bool CryptoInitialized = false;
            bool ProcInfoInitialized = false;

            var memInfo = OSPlatformFunc.GlobalMemoryStatusEx();
            CxPlatTotalMemory = (long)memInfo.ullTotalPageFile;
            CryptoInitialized = true;
        Error:
            return Status;
        }

        static void CxPlatUninitialize()
        {
            NetLog.Assert(CxPlatform.Heap != IntPtr.Zero);
        }

        public static int CxPlatThreadCreate(CXPLAT_THREAD_CONFIG Config, out Thread mThread)
        {
            mThread = new Thread(Config.Callback);

            if (HasFlag(Config.Flags, (ulong)CXPLAT_THREAD_FLAGS.CXPLAT_THREAD_FLAG_SET_IDEAL_PROC))
            {
                Thread.BeginThreadAffinity();
                Thread.EndThreadAffinity();
            }

            if (HasFlag(Config.Flags, (ulong)CXPLAT_THREAD_FLAGS.CXPLAT_THREAD_FLAG_HIGH_PRIORITY))
            {
                mThread.Priority = ThreadPriority.AboveNormal;
            }

            if (Config.Name != null)
            {
                mThread.Name = Config.Name;
            }

            mThread.IsBackground = true;
            mThread.Start(Config.Context);
            return 0;
        }
        
        static void CxPlatThreadDelete(Thread mThread)
        {
            mThread.Abort();
        }

        static void CxPlatThreadWait(Thread mThread)
        {
            mThread.Join();
        }

        //static void SetThreadAffinity(ushort nProcessorId)
        //{
        //    Thread.BeginThreadAffinity();
        //    SetThreadAffinity2(nProcessorId);
        //    Thread.EndThreadAffinity();
        //}

        //static void SetThreadAffinity2(ushort nProcessorId)
        //{
        //    IntPtr mThreadPtr = Interop.Kernel32.GetCurrentThread();
        //    PROCESSOR_NUMBER mData = new PROCESSOR_NUMBER();
        //    mData.Group = 0;
        //    mData.Number = (byte)nProcessorId;
        //    if(!Interop.Kernel32.SetThreadIdealProcessorEx(mThreadPtr, &mData, null))
        //    {
        //        NetLog.LogError("线程 设置 CPU 亲和性 失败");
        //    }
        //}

        static void SetThreadAffinity(ushort nProcessorId)
        {
            Thread.BeginThreadAffinity();

            var curProcess = Process.GetCurrentProcess();
            int threadId = Interop.Kernel32.GetCurrentThreadId();
            ProcessThread currentThread = null;
            foreach(ProcessThread pt in curProcess.Threads)
            {
                if (pt.Id == threadId)
                {
                    currentThread = pt;
                    break;
                }
            }

            // 设置线程亲和性（例如绑定到第0号CPU核心）
            if (currentThread != null)
            {
                currentThread.IdealProcessor = nProcessorId;
                //currentThread.ProcessorAffinity = new IntPtr(1 << nProcessorId); // 二进制 0001 -> CPU 0
            }

            Thread.EndThreadAffinity();
        }
    }
}
