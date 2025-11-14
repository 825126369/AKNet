/************************************Copyright*****************************************
*        ProjectName:AKNet
*        Web:https://github.com/825126369/AKNet
*        Description:C#游戏网络库
*        Author:许珂
*        ModifyTime:2025/11/14 8:44:32
*        Copyright:MIT软件许可证
************************************Copyright*****************************************/
using AKNet.Common;
using System;
using System.Threading;

namespace MSQuic1
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
        public readonly CXPLAT_LIST_ENTRY<CXPLAT_EXECUTION_CONTEXT> Entry;
        public QUIC_WORKER Context;
        public CXPLAT_WORKER CxPlatContext;
        public Func<QUIC_WORKER, CXPLAT_EXECUTION_STATE, bool> Callback;
        public long NextTimeUs;
        public volatile int Ready; // volatile 总是读的最新的值，而不是缓存， 表示有更多的工作要做了

        public CXPLAT_EXECUTION_CONTEXT()
        {
            Entry = new CXPLAT_LIST_ENTRY<CXPLAT_EXECUTION_CONTEXT>(this);
        }
    }

    internal class CXPLAT_RUNDOWN_REF
    {
        public long RefCount;
        public EventWaitHandle RundownComplete;
    }

    internal class CXPLAT_EXECUTION_STATE
    {
        public long TimeNow;              
        public long LastWorkTime;         
        public long LastPoolProcessTime; 
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
        public ushort Group;
    }

    internal static partial class MSQuicFunc
    {
        public static bool IS_POWER_OF_TWO(long x)
        {
            return (((x) != 0) && (((x) & ((x) - 1)) == 0));
        }

        //获取线程Id
        static int CxPlatCurThreadID()
        {
            return Thread.CurrentThread.ManagedThreadId;
        }

        //获取处理器Id
        static int CxPlatProcCurrentNumber()
        {
            return Thread.GetCurrentProcessorId();
        }

        //获取处理器数量
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

        static void CxPlatRundownReleaseAndWait(CXPLAT_RUNDOWN_REF Rundown)
        {
            if (!CxPlatRefDecrement(ref Rundown.RefCount))
            {
                CxPlatEventWaitForever(Rundown.RundownComplete);

            }
        }

        static void CxPlatRundownUninitialize(CXPLAT_RUNDOWN_REF Rundown)
        {
            Rundown.RundownComplete.Close();
        }

        static bool CxPlatRefIncrementNonZero(ref long RefCount, long Bias)
        {
            long NewValue;
            long OldValue;

            OldValue = RefCount;
            for (; ; )
            {
                NewValue = OldValue + Bias;
                if (NewValue > Bias) //这里判断是否为0了，如果RefCount 小于等同于0，这个条件不成立
                {
                    //如果目标变量的当前值等于预期值，则将其替换为新值，这个操作是原子性的，也就是说，在多线程环境下不会被其他线程中断。
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
            CxPlatEventInitialize(out Rundown.RundownComplete, false, false);
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

        public static int CxPlatHashSimple(QUIC_SSBuffer Buffer)
        {
            int Hash = 5387; // A random prime number.
            for (int i = 0; i < Buffer.Length; ++i)
            {
                Hash = ((Hash << 5) - Hash) + Buffer[i];
            }
            return Hash;
        }

    }
}
