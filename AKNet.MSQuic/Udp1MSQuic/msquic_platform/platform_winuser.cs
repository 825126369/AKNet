using AKNet.Common;
using AKNet.Platform;
using System;
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
    }
}
