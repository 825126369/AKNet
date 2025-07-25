#if TARGET_WINDOWS
using AKNet.Common;
using AKNet.Platform;
using System;
using System.Net;
using System.Net.Sockets;
using System.Runtime.InteropServices;

namespace AKNet.Udp2MSQuic.Common
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
        static CXPLAT_PROCESSOR_INFO* CxPlatProcessorInfo;
        static CXPLAT_PROCESSOR_GROUP_INFO* CxPlatProcessorGroupInfo;
        static int CxPlatProcessorCount;
        static long CxPlatTotalMemory;

        static void CxPlatSystemLoad()
        {

        }

        public static int CxPlatInitialize()
        {
            int Status;
            bool CryptoInitialized = false;
            bool ProcInfoInitialized = false;

            CxPlatform.Heap = Interop.Kernel32.HeapCreate(0, 0, 0);
            if (CxPlatform.Heap == IntPtr.Zero)
            {
                Status = 1;
                goto Error;
            }

            if (QUIC_FAILED(Status = CxPlatProcessorInfoInit()))
            {
                NetLog.LogError("CxPlatProcessorInfoInit failed");
                goto Error;
            }
            ProcInfoInitialized = true;

            var memInfo = OSPlatformFunc.GlobalMemoryStatusEx();
            CxPlatTotalMemory = (long)memInfo.ullTotalPageFile;
            CryptoInitialized = true;
        Error:
            if (QUIC_FAILED(Status))
            {
                if (ProcInfoInitialized)
                {
                    CxPlatProcessorInfoUnInit();
                }
                if (CxPlatform.Heap != IntPtr.Zero)
                {
                    Interop.Kernel32.HeapDestroy(CxPlatform.Heap);
                    CxPlatform.Heap = IntPtr.Zero;
                }
            }
            return Status;
        }

        static void CxPlatUninitialize()
        {
            NetLog.Assert(CxPlatform.Heap != IntPtr.Zero);
            CxPlatProcessorInfoUnInit();
            Interop.Kernel32.HeapDestroy(CxPlatform.Heap);
            CxPlatform.Heap = IntPtr.Zero;
        }

        //public static int CxPlatProcCount()
        //{
        //    return CxPlatProcessorCount;
        //}

        public static int CxPlatProcessorInfoInit()
        {
            int Status = 0;
            int InfoLength = 0;
            SYSTEM_LOGICAL_PROCESSOR_INFORMATION_EX* Info = null;
            int ActiveProcessorCount = 0, MaxProcessorCount = 0;
            Status = CxPlatGetProcessorGroupInfo(LOGICAL_PROCESSOR_RELATIONSHIP.RelationGroup, &Info, out InfoLength);
            if (Status != 0)
            {
                goto Error;
            }

            NetLog.Assert(InfoLength != 0);
            NetLog.Assert(Info->Relationship == LOGICAL_PROCESSOR_RELATIONSHIP.RelationGroup);
            NetLog.Assert(Info->DUMMYUNIONNAME.Group.ActiveGroupCount != 0);
            NetLog.Assert(Info->DUMMYUNIONNAME.Group.ActiveGroupCount <= Info->DUMMYUNIONNAME.Group.MaximumGroupCount);
            if (Info->DUMMYUNIONNAME.Group.ActiveGroupCount == 0)
            {
                goto Error;
            }

            for (int i = 0; i < Info->DUMMYUNIONNAME.Group.ActiveGroupCount; ++i)
            {
                ActiveProcessorCount += Info->DUMMYUNIONNAME.Group.GroupInfo[i].ActiveProcessorCount;
                MaxProcessorCount += Info->DUMMYUNIONNAME.Group.GroupInfo[i].MaximumProcessorCount;
            }

            NetLog.Assert(ActiveProcessorCount > 0);
            NetLog.Assert(ActiveProcessorCount <= ushort.MaxValue);
            if (ActiveProcessorCount == 0 || ActiveProcessorCount > ushort.MaxValue)
            {
                goto Error;
            }

            NetLog.Log(string.Format("[ dll] Processors: ({0} active, {1} max), Groups: ({2} active, {3} max)",
                ActiveProcessorCount,
                MaxProcessorCount,
                Info->DUMMYUNIONNAME.Group.ActiveGroupCount,
                Info->DUMMYUNIONNAME.Group.MaximumGroupCount));

            NetLog.Assert(CxPlatProcessorInfo == null);
            CxPlatProcessorInfo = (CXPLAT_PROCESSOR_INFO*)OSPlatformFunc.CxPlatAlloc(ActiveProcessorCount * sizeof(CXPLAT_PROCESSOR_INFO));
            if (CxPlatProcessorInfo == null)
            {
                goto Error;
            }

            OSPlatformFunc.CxPlatZeroMemory(
                CxPlatProcessorInfo,
                ActiveProcessorCount * sizeof(CXPLAT_PROCESSOR_INFO));

            NetLog.Assert(CxPlatProcessorGroupInfo == null);
            CxPlatProcessorGroupInfo = (CXPLAT_PROCESSOR_GROUP_INFO*)OSPlatformFunc.CxPlatAlloc(
                    Info->DUMMYUNIONNAME.Group.ActiveGroupCount * sizeof(CXPLAT_PROCESSOR_GROUP_INFO));

            if (CxPlatProcessorGroupInfo == null)
            {
                goto Error;
            }

            CxPlatProcessorCount = 0;
            for (int i = 0; i < Info->DUMMYUNIONNAME.Group.ActiveGroupCount; ++i)
            {
                CxPlatProcessorGroupInfo[i].Mask = Info->DUMMYUNIONNAME.Group.GroupInfo[i].ActiveProcessorMask;
                CxPlatProcessorGroupInfo[i].Count = Info->DUMMYUNIONNAME.Group.GroupInfo[i].ActiveProcessorCount;
                CxPlatProcessorGroupInfo[i].Offset = CxPlatProcessorCount;
                CxPlatProcessorCount += Info->DUMMYUNIONNAME.Group.GroupInfo[i].ActiveProcessorCount;
            }

            for (int Proc = 0; Proc < ActiveProcessorCount; ++Proc)
            {
                for (int Group = 0; Group < Info->DUMMYUNIONNAME.Group.ActiveGroupCount; ++Group)
                {
                    if (Proc >= CxPlatProcessorGroupInfo[Group].Offset &&
                        Proc < CxPlatProcessorGroupInfo[Group].Offset + Info->DUMMYUNIONNAME.Group.GroupInfo[Group].ActiveProcessorCount)
                    {
                        CxPlatProcessorInfo[Proc].Group = (ushort)Group;
                        NetLog.Assert(Proc - CxPlatProcessorGroupInfo[Group].Offset <= byte.MaxValue);
                        CxPlatProcessorInfo[Proc].Index = (byte)(Proc - CxPlatProcessorGroupInfo[Group].Offset);
                        break;
                    }
                }
            }

            if (Info != null)
            {
                OSPlatformFunc.CxPlatFree(Info);
            }

            return 0;
        Error:
            if (Info != null)
            {
                OSPlatformFunc.CxPlatFree(Info);
            }
            if (CxPlatProcessorGroupInfo != null)
            {
                OSPlatformFunc.CxPlatFree(CxPlatProcessorGroupInfo);
                CxPlatProcessorGroupInfo = null;
            }
            if (CxPlatProcessorInfo != null)
            {
                OSPlatformFunc.CxPlatFree(CxPlatProcessorInfo);
                CxPlatProcessorInfo = null;
            }
            return 1;
        }

        static void CxPlatProcessorInfoUnInit()
        {
            OSPlatformFunc.CxPlatFree(CxPlatProcessorGroupInfo);
            CxPlatProcessorGroupInfo = null;
            OSPlatformFunc.CxPlatFree(CxPlatProcessorInfo);
            CxPlatProcessorInfo = null;
        }

        static int CxPlatGetProcessorGroupInfo(LOGICAL_PROCESSOR_RELATIONSHIP Relationship, SYSTEM_LOGICAL_PROCESSOR_INFORMATION_EX** Buffer, out int BufferLength)
        {
            BufferLength = 0;
            Interop.Kernel32.GetLogicalProcessorInformationEx(Relationship, null, out BufferLength);
            if (BufferLength == 0)
            {
                return Interop.Kernel32.GetLastError();
            }

            *Buffer = (SYSTEM_LOGICAL_PROCESSOR_INFORMATION_EX*)OSPlatformFunc.CxPlatAlloc(BufferLength);
            if (*Buffer == null)
            {
                return 1;
            }

            if (!Interop.Kernel32.GetLogicalProcessorInformationEx(Relationship, *Buffer, out BufferLength))
            {
                OSPlatformFunc.CxPlatFree(*Buffer);
                return Interop.Kernel32.GetLastError();
            }
            return 0;
        }

        public static int CxPlatThreadCreate(CXPLAT_THREAD_CONFIG Config, out CXPLAT_THREAD mThread)
        {
            mThread = new CXPLAT_THREAD();
            mThread.mConfig = Config;
            IntPtr Thread = Interop.Kernel32.CreateThread(IntPtr.Zero, IntPtr.Zero, mThread.ThreadFunc, IntPtr.Zero, 0, out _);
            if (Thread == IntPtr.Zero)
            {
                int Error = Interop.Kernel32.GetLastError();
                NetLog.LogError(Error);
                return Error;
            }
            mThread.mThreadPtr = Thread;

            NetLog.Assert(Config.IdealProcessor < CxPlatProcCount());
            CXPLAT_PROCESSOR_INFO ProcInfo = CxPlatProcessorInfo[Config.IdealProcessor];
            GROUP_AFFINITY Group;
            if (HasFlag(Config.Flags, (ushort)CXPLAT_THREAD_FLAGS.CXPLAT_THREAD_FLAG_SET_AFFINITIZE))
            {
                Group.Mask = (ulong)(1ul << ProcInfo.Index);          // Fixed processor
            }
            else
            {
                Group.Mask = CxPlatProcessorGroupInfo[ProcInfo.Group].Mask;
            }

            Group.Group = ProcInfo.Group;
            if (!Interop.Kernel32.SetThreadGroupAffinity(Thread, &Group, null))
            {
                NetLog.LogError("SetThreadGroupAffinity");
            }
            if (HasFlag(Config.Flags, (ulong)CXPLAT_THREAD_FLAGS.CXPLAT_THREAD_FLAG_SET_IDEAL_PROC) &&
                !Interop.Kernel32.SetThreadIdealProcessorEx(Thread, (PROCESSOR_NUMBER*)&ProcInfo, null))
            {
                NetLog.LogError("SetThreadIdealProcessorEx");
            }
            if (HasFlag(Config.Flags, (ulong)CXPLAT_THREAD_FLAGS.CXPLAT_THREAD_FLAG_HIGH_PRIORITY) &&
                !Interop.Kernel32.SetThreadPriority(Thread, OSPlatformFunc.THREAD_PRIORITY_HIGHEST))
            {
                NetLog.LogError("SetThreadPriority");
            }

            if (Config.Name != null)
            {
                Interop.Kernel32.SetThreadDescription(Thread, Config.Name);
            }
            return 0;
        }

        static void CxPlatThreadDelete(CXPLAT_THREAD mThread)
        {
            if (mThread.mThreadPtr != IntPtr.Zero)
            {
                Interop.Kernel32.CloseHandle(mThread.mThreadPtr);
                mThread.mThreadPtr = IntPtr.Zero;
            }
        }

        static void CxPlatThreadWait(CXPLAT_THREAD mThread)
        {
            Interop.Kernel32.WaitForSingleObject(mThread.mThreadPtr, OSPlatformFunc.INFINITE);
        }

        internal static bool QuicAddrCompare(QUIC_ADDR Addr1, QUIC_ADDR Addr2)
        {
            if (Addr1.RawAddr->si_family != Addr2.RawAddr->si_family || Addr1.RawAddr->Ipv4.sin_port != Addr2.RawAddr->Ipv4.sin_port)
            {
                return false;
            }
            return QuicAddrCompareIp(Addr1, Addr2);
        }

        static bool QuicAddrCompareIp(QUIC_ADDR Addr1, QUIC_ADDR Addr2)
        {
            if (Addr1.RawAddr->si_family == OSPlatformFunc.AF_INET)
            {
                return orBufferEqual(Addr1.RawAddr->Ipv4.sin_addr.GetSpan(), Addr2.RawAddr->Ipv4.sin_addr.GetSpan());
            }
            else
            {
                return orBufferEqual(Addr1.RawAddr->Ipv6.sin6_addr.GetSpan(), Addr2.RawAddr->Ipv6.sin6_addr.GetSpan());
            }
        }

        static int QuicAddrGetPort(QUIC_ADDR Addr)
        {
            return Addr.RawAddr->Ipv4.sin_port;
        }

        static void QuicAddrSetPort(QUIC_ADDR Addr, int Port)
        {
            Addr.RawAddr->Ipv4.sin_port = (ushort)Port;
        }

        static AddressFamily QuicAddrGetFamily(QUIC_ADDR Addr)
        {
            return (AddressFamily)Addr.RawAddr->si_family;
        }

        static bool QuicAddrIsWildCard(QUIC_ADDR Addr)
        {
            /*
            public static readonly IPAddress Any = new ReadOnlyIPAddress([0, 0, 0, 0]);
            public static readonly IPAddress Loopback = new ReadOnlyIPAddress([127, 0, 0, 1]);
            public static readonly IPAddress Broadcast = new ReadOnlyIPAddress([255, 255, 255, 255]);
            public static readonly IPAddress None = Broadcast;
            public static readonly IPAddress IPv6Any = new IPAddress((ReadOnlySpan<byte>)[0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0], 0);
             */

            if (Addr.RawAddr->si_family == (ushort)AddressFamily.Unspecified)
            {
                return true;
            }
            else if (Addr.RawAddr->si_family == OSPlatformFunc.AF_INET)
            {
                IN_ADDR ZeroAddr = new IN_ADDR();
                return OSPlatformFunc.memcmp(&Addr.RawAddr->Ipv4.sin_addr, &ZeroAddr, sizeof(IN_ADDR));
            }
            else
            {
                IN6_ADDR ZeroAddr = new IN6_ADDR();
                return OSPlatformFunc.memcmp(&Addr.RawAddr->Ipv6.sin6_addr, &ZeroAddr, sizeof(IN6_ADDR));
            }
        }

        static bool QuicAddrIsValid(QUIC_ADDR Addr)
        {
            return Addr.Family == AddressFamily.InterNetwork ||
                Addr.Family == AddressFamily.InterNetworkV6;
        }

        public static int QUIC_ADDR_V4_PORT_OFFSET()
        {
            return (int)Marshal.OffsetOf(typeof(SOCKADDR_IN), "sin_port");
        }
        public static int QUIC_ADDR_V4_IP_OFFSET()
        {
            return (int)Marshal.OffsetOf(typeof(SOCKADDR_IN), "sin_addr");
        }

        public static int QUIC_ADDR_V6_PORT_OFFSET()
        {
            return (int)Marshal.OffsetOf(typeof(SOCKADDR_IN6), "sin6_port");
        }

        public static int QUIC_ADDR_V6_IP_OFFSET()
        {
            return (int)Marshal.OffsetOf(typeof(SOCKADDR_IN6), "sin6_addr");
        }

        static void UPDATE_HASH(uint value, ref uint Hash)
        {
            Hash = (Hash << 5) - Hash + (value);
        }

        static uint QuicAddrHash(QUIC_ADDR Addr)
        {
            uint Hash = 5387;
            if (Addr.RawAddr->si_family == OSPlatformFunc.AF_INET)
            {
                UPDATE_HASH((uint)(Addr.RawAddr->Ipv4.sin_port & 0xFF), ref Hash);
                UPDATE_HASH((uint)Addr.RawAddr->Ipv4.sin_port >> 8, ref Hash);
                ReadOnlySpan<byte> addr_bytes = Addr.RawAddr->Ipv4.sin_addr.GetSpan();
                for (int i = 0; i < addr_bytes.Length; ++i)
                {
                    UPDATE_HASH(addr_bytes[i], ref Hash);
                }
            }
            else
            {
                UPDATE_HASH((uint)(Addr.RawAddr->Ipv6.sin6_port & 0xFF), ref Hash);
                UPDATE_HASH((uint)Addr.RawAddr->Ipv6.sin6_port >> 8, ref Hash);
                ReadOnlySpan<byte> addr_bytes = Addr.RawAddr->Ipv6.sin6_addr.GetSpan();
                for (int i = 0; i < addr_bytes.Length; ++i)
                {
                    UPDATE_HASH(addr_bytes[i], ref Hash);
                }
            }
            return Hash;
        }

    }
}
#endif
