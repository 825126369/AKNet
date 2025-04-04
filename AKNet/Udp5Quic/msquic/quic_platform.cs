﻿using AKNet.Common;
using System;
using System.Collections.Generic;
using System.Threading;

namespace AKNet.Udp5Quic.Common
{
    internal enum CXPLAT_THREAD_FLAGS
    {
        CXPLAT_THREAD_FLAG_NONE = 0x0000,
        CXPLAT_THREAD_FLAG_SET_IDEAL_PROC = 0x0001,
        CXPLAT_THREAD_FLAG_SET_AFFINITIZE = 0x0002,
        CXPLAT_THREAD_FLAG_HIGH_PRIORITY = 0x0004
    }

    internal class CXPLAT_EXECUTION_CONTEXT
    {
        public CXPLAT_SLIST_ENTRY Entry;
        public QUIC_WORKER Context;
        public CXPLAT_WORKER CxPlatContext;
        public Func<QUIC_WORKER, CXPLAT_EXECUTION_STATE, bool> Callback;
        public long NextTimeUs;
        public bool Ready;
    }

    internal class CXPLAT_RUNDOWN_REF
    {
        public long RefCount;
        public Action RundownComplete;
    }

    internal class CXPLAT_WORKER_POOL
    {
        public readonly List<CXPLAT_WORKER> Workers = new List<CXPLAT_WORKER>();
        public readonly object WorkerLock = new object();
        public CXPLAT_RUNDOWN_REF Rundown;
    }

    internal class CXPLAT_POOL_EX
    {
        public CXPLAT_POOL Base;
        public CXPLAT_LIST_ENTRY Link;
    }

    internal class CXPLAT_EXECUTION_STATE
    {
        public long TimeNow;               // in microseconds
        public long LastWorkTime;          // in microseconds
        public long LastPoolProcessTime;   // in microseconds
        public long WaitTime;
        public int NoWorkCount;
        public int ThreadID;
    }

    internal class CXPLAT_THREAD_CONFIG
    {
        public uint Flags;
        public int IdealProcessor;
        public string Name;
        public ParameterizedThreadStart Callback;
        public QUIC_WORKER Context;
    }

    internal class CXPLAT_PROCESSOR_INFO
    {
        public int Index;
        public int Group;
    }

    internal static partial class MSQuicFunc
    {
        static int CxPlatCurThreadID()
        {
            return Thread.CurrentThread.ManagedThreadId;
        }

        static int CxPlatProcCount()
        {
            return Environment.ProcessorCount;
        }

        static bool CxPlatRundownAcquire(CXPLAT_RUNDOWN_REF Rundown)
        {
            Interlocked.Increment(ref Rundown.RefCount);
            return true;
        }

        static void CxPlatRundownRelease(CXPLAT_RUNDOWN_REF Rundown)
        {
            if (CxPlatRefDecrement(ref Rundown.RefCount))
            {
                Rundown.RundownComplete();
            }
        }

        static QUIC_STREAM CXPLAT_CONTAINING_RECORD(CXPLAT_HASHTABLE_ENTRY mEntry)
        {
            return ((CXPLAT_HASHTABLE_ENTRY_QUIC_STREAM)(mEntry)).mContain;
        }

        static QUIC_STREAM CXPLAT_CONTAINING_RECORD(CXPLAT_LIST_ENTRY mEntry)
        {
            return ((CXPLAT_LIST_ENTRY_QUIC_STREAM)(mEntry)).mContain;
        }

        static long CxPlatTimeDiff(long T1, long T2)
        {
            return T2 - T1;
        }

        static long CxPlatTime()
        {
            return mStopwatch.ElapsedMilliseconds;
        }

        static long CxPlatTimeDiff64(long T1, long T2)
        {
            return T2 - T1;
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

        static bool CxPlatEventQEnqueue(int queue, int sqe)
        {
            return eventfd_write(sqe.fd, 1) == 0;
        }

        static ulong CxPlatThreadCreate(CXPLAT_THREAD_CONFIG Config, Thread mThread)
        {
            mThread = new Thread(Config.Callback);

            NetLog.Assert(Config.IdealProcessor < CxPlatProcCount());
            mThread.Name = Config.Name;
            if (BoolOk(Config.Flags & (int)CXPLAT_THREAD_FLAGS.CXPLAT_THREAD_FLAG_HIGH_PRIORITY))
            {
                mThread.Priority = ThreadPriority.Highest;
            }

            mThread.Start(Config.Context);// 这里创建完成后，会立刻运行线程
            return QUIC_STATUS_SUCCESS;
        }

        static ulong CxPlatProcessorInfoInit()
        {
            return 0;
        }

        static ulong CxPlatByteSwapUint16(ushort value)
        {
            return (ushort)((value >> 8) | (value << 8));
        }

        static ulong CxPlatByteSwapUint32(uint value)
        {
            return (ushort)((value >> 8) | (value << 8));
        }
        static ulong CxPlatByteSwapUint64(ulong value)
        {
            return (ushort)((value >> 8) | (value << 8));
        }
    }
}
