using AKNet.Common;
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
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
        //public CXPLAT_WORKER CxPlatContext;
        public Func<QUIC_WORKER, CXPLAT_EXECUTION_STATE, bool> Callback;
        public long NextTimeUs;
        public bool Ready;
    }

    internal class CXPLAT_RUNDOWN_REF
    {
        public long RefCount;
        public readonly CXPLAT_EVENT RundownComplete = new CXPLAT_EVENT();
    }

    internal class CXPLAT_WORKER_POOL
    {
        //public readonly List<CXPLAT_WORKER> Workers = new List<CXPLAT_WORKER>();
        public readonly object WorkerLock = new object();
        public CXPLAT_RUNDOWN_REF Rundown;
    }

    internal class CXPLAT_POOL_EX<T>: CXPLAT_POOL<T> where T : class, CXPLAT_POOL_Interface<T>, new()
    {
        public CXPLAT_LIST_ENTRY Link;
    }

    internal class CXPLAT_EXECUTION_STATE
    {
        public long TimeNow;               // in microseconds
        public long LastWorkTime;          // in microseconds
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
        public object Context;
    }

    internal class CXPLAT_PROCESSOR_INFO
    {
        public int Index;
        public int Group;
    }

    internal static partial class MSQuicFunc
    {
        public static bool IS_POWER_OF_TWO(long x)
        {
            return (((x) != 0) && (((x) & ((x) - 1)) == 0));
        }

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
                CxPlatEventSet(Rundown.RundownComplete);
            }
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

        static bool CxPlatRefIncrementNonZero(long RefCount, long Bias)
        {
            long NewValue;
            long OldValue;

            OldValue = RefCount;
            for (; ; )
            {
                NewValue = OldValue + Bias;
                if (NewValue > Bias)
                {
                    NewValue = Interlocked.CompareExchange(ref RefCount, NewValue, OldValue);
                    if (NewValue == OldValue)
                    {
                        return true;
                    }
                    OldValue = NewValue;
                }
                else if (NewValue == Bias)
                {
                    return false;

                }
                else
                {
                    return false;
                }
            }
        }

        static void CxPlatRefInitialize(ref long RefCount)
        {
            RefCount = 1;
        }

        static void CxPlatRundownInitialize(CXPLAT_RUNDOWN_REF Rundown)
        {
            CxPlatRefInitialize(ref Rundown.RefCount);
            CxPlatEventInitialize(Rundown.RundownComplete, false, false);
        }

        static int CxPlatProcCurrentNumber()
        {
            return Thread.CurrentThread.ManagedThreadId;
        }

        //static bool CxPlatEventQEnqueue(CXPLAT_WORKER Worker)
        //{
        //    //ThreadPool.QueueUserWorkItem((state) =>
        //    //{

        //    //});

        //    //return true;
        //}

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

        static CXPLAT_DATAPATH_TYPE DatapathType(CXPLAT_SEND_DATA SendData)
        {
            return SendData.DatapathType;
        }

        static void CxPlatRefInitializeEx(ref long RefCount, long Initial)
        {
            RefCount = Initial;
        }
    }
}
