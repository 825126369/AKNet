#if TARGET_WINDOWS
using Microsoft.Win32.SafeHandles;
using System.Runtime.InteropServices;

namespace AKNet.Platform
{
    public enum SCOPE_LEVEL
    {
        ScopeLevelInterface = 1,
        ScopeLevelLink = 2,
        ScopeLevelSubnet = 3,
        ScopeLevelAdmin = 4,
        ScopeLevelSite = 5,
        ScopeLevelOrganization = 8,
        ScopeLevelGlobal = 14,
        ScopeLevelCount = 16
    }

    internal unsafe struct GUID
    {
        public ulong Data1;
        public ushort Data2;
        public ushort Data3;
        public fixed byte Data4[8];
    }

    public struct INET_PORT_RANGE
    {
        public ushort StartPort;
        public ushort NumberOfPorts;
    }

    public struct INET_PORT_RESERVATION_TOKEN
    {
        public ulong Token;
    }

    public struct INET_PORT_RESERVATION_INSTANCE
    {
        public INET_PORT_RANGE Reservation;
        public INET_PORT_RESERVATION_TOKEN Token;
    }

    public unsafe struct SOCKADDR
    {
        public ushort sa_family;
        public fixed byte sa_data[14];
    }

    public unsafe struct SOCKADDR_IN
    {
        public ushort sin_family;
        public ushort sin_port;
        public IN_ADDR sin_addr;
        public fixed byte sin_zero[8];
    }

    public unsafe struct SOCKADDR_IN6
    {
        public ushort sin6_family;          // AF_INET6.
        public ushort sin6_port;            // Transport level port number.
        public ulong sin6_flowinfo;         // IPv6 flow information.
        public IN6_ADDR sin6_addr;            // IPv6 address.
        public ulong sin6_scope_id;
    }

    public unsafe struct IN_ADDR
    {
        public fixed byte u[4];

        public ReadOnlySpan<byte> GetSpan()
        {
            fixed (void* uPtr = u)
            {
                return new ReadOnlySpan<byte>(uPtr, 4);
            }
        }
    }

    public unsafe struct IN6_ADDR
    {
        public fixed byte u[16];

        public ReadOnlySpan<byte> GetSpan()
        {
            fixed (void* uPtr = u)
            {
                return new ReadOnlySpan<byte>(uPtr, 16);
            }
        }
    }

    [StructLayout(LayoutKind.Explicit)]
    public struct SOCKADDR_INET
    {
        [FieldOffset(0)] public SOCKADDR_IN Ipv4;
        [FieldOffset(0)] public SOCKADDR_IN6 Ipv6;
        [FieldOffset(0)] public ushort si_family;
    }

    [StructLayout(LayoutKind.Sequential)]
    public unsafe struct WSABUF
    {
        public int len; // Length of Buffer
        public byte* buf; // Pointer to Buffer
    }

    public unsafe struct WSAMSG
    {
        public void* name;        //远程地址
        public int namelen;           // 远程地址长度
        public WSABUF* lpBuffers;     // 用户数据Buffer
        public int dwBufferCount;     // 用户数据Buffer的长度
        public WSABUF Control;        // 控制Buffer
        public uint dwFlags;
    }

    public struct WSACMSGHDR
    {
        public int cmsg_len;
        public int cmsg_level;
        public int cmsg_type;
    }

    public unsafe struct IN6_PKTINFO
    {
        public IN6_ADDR ipi6_addr;    // Source/destination IPv6 address.
        public ulong ipi6_ifindex;    // Send/receive interface index.
    }

    public unsafe struct IN_PKTINFO
    {
        public IN_ADDR ipi_addr;     // Source/destination IPv4 address.
        public ulong ipi_ifindex;    // Send/receive interface index.
    }

    public struct RIO_CMSG_BUFFER
    {
        public long TotalLength;
    }

    public class CxPlatSocketHandle : SafeHandleMinusOneIsInvalid
    {
        public CxPlatSocketHandle() : base(true) { }
        protected override bool ReleaseHandle()
        {
            return Interop.Kernel32.CloseHandle(handle);
        }

        public static implicit operator CxPlatSocketHandle(IntPtr mPtr)
        {
            CxPlatSocketHandle handle = new CxPlatSocketHandle();
            handle.SetHandle(mPtr);
            return handle;
        }
    }

    public static unsafe partial class OSPlatformFunc
    {
        public const int AF_INET = 2;               // internetwork: UDP, TCP, etc.
        public const int AF_INET6 = 23;            // Internetwork Version 6
        public const int SOCK_DGRAM = 2;             /* datagram socket */
        public const int IPPROTO_UDP = 17;
        public const int IPPROTO_IP = 0;

        public const int WSA_FLAG_OVERLAPPED = 0x01;
        public const int WSA_FLAG_REGISTERED_IO = 0x100;
        public const int IPPROTO_IPV6 = 41;
        public const int IPV6_V6ONLY = 27;
        public const int IPV6_UNICAST_IF = 31; // IP unicast interface.

        public const ulong INVALID_SOCKET = (ulong)(~0UL);
        public const int SOCKET_ERROR = (-1);
        public const int NO_ERROR = 0;

        public const ulong ERROR_IO_PENDING = 997;    // dderror
        public const ulong WSAENOTSOCK = 10038;
        public const ulong ERROR_OPERATION_ABORTED = 995;
        public const ulong WSAECONNRESET = 10054L;
        public const ulong WSAEHOSTUNREACH = 10065L;
        public const ulong WSAENETUNREACH = 10051L;
        public const ulong ERROR_PORT_UNREACHABLE = 1234L;
        public const ulong ERROR_PROTOCOL_UNREACHABLE = 1233L;
        public const ulong ERROR_HOST_UNREACHABLE = 1232L;
        public const ulong ERROR_NETWORK_UNREACHABLE = 1231L;
        public const ulong ERROR_MORE_DATA = 234L;    // dderror
        public const ulong WSA_IO_PENDING = (ERROR_IO_PENDING);
        public const ulong WSA_OPERATION_ABORTED = (ERROR_OPERATION_ABORTED);
        public const ulong WSAESHUTDOWN = 10058L;

        public const byte FILE_SKIP_COMPLETION_PORT_ON_SUCCESS = 0x1;
        public const byte FILE_SKIP_SET_EVENT_ON_HANDLE = 0x2;

        public const uint IOC_WS2 = 0x08000000;
        public const uint IOC_OUT = 0x40000000;  
        public const uint IOC_IN = 0x80000000;
        public const uint IOC_INOUT = (IOC_IN | IOC_OUT);
        public const uint IOC_VENDOR = 0x18000000;
        public static readonly uint SIO_CPU_AFFINITY = _WSAIOW(IOC_VENDOR, 21U);
        public static readonly uint SIO_ACQUIRE_PORT_RESERVATION = _WSAIOW(IOC_VENDOR, 100);
        public static readonly uint SIO_ASSOCIATE_PORT_RESERVATION = _WSAIOW(IOC_VENDOR, 102);
        public static readonly uint SIO_GET_EXTENSION_FUNCTION_POINTER = _WSAIORW(IOC_WS2, 6);

        public const int IP_TOS = 3; // IP type of service.
        public const int IP_TTL = 4; // IP TTL (hop limit).
        public const int IP_PKTINFO = 19; // Receive packet information.
        public const int IP_DONTFRAGMENT = 14; // Don't fragment IP datagrams.
        public const int IPV6_DONTFRAG = 14; // Don't fragment IP datagrams.
        public const int IPV6_PKTINFO = 19; // Receive packet information.
        public const int IPV6_TCLASS = 39; // Packet traffic class.

        //IPV6_RECVTCLASS 是一个用于 IPv6 套接字编程的选项，用于启用接收端获取数据包的 TClass（Traffic Class）字段值。
        //这个字段在 IPv6 报文中用于标识数据包的流量类别，类似于 IPv4 中的 TOS（Type of Service）字段。
        public const int IPV6_RECVTCLASS = 40;
        public const int IP_RECVTOS = 40; // Receive packet Type Of Service (TOS)
        public const int IP_ECN = 50;
        public const int IPV6_ECN = 50; // IPv6 ECN codepoint.
        public const int IP_HOPLIMIT = 21;
        public const int IPV6_HOPLIMIT = 21;
        public const int IP_UNICAST_IF = 31;
        public const int SOL_SOCKET = 0xffff;
        public const int SO_RCVBUF = 0x1002;

        public const int RIO_SEND_QUEUE_DEPTH = 256;
        //public static readonly int RIO_CMSG_BASE_SIZE = WSA_CMSGHDR_ALIGN(sizeof(RIO_CMSG_BUFFER));


        public const int UDP_SEND_MSG_SIZE = 2;
        public const int UDP_RECV_MAX_COALESCED_SIZE = 3;

        public static uint _WSAIOW(uint x, uint y)
        {
            return (IOC_IN | (x) | (y));
        }

        public static uint _WSAIORW(uint x, uint y)
        {
            return (IOC_INOUT | (x) | (y));
        }

        public static WSACMSGHDR* WSA_CMSG_FIRSTHDR(WSAMSG* msg)
        {
            return msg->Control.len >= sizeof(WSACMSGHDR) ? (WSACMSGHDR*)(msg->Control.buf) : null;
        }

        public static WSACMSGHDR* WSA_CMSG_NXTHDR(WSAMSG* msg, WSACMSGHDR* cmsg)
        {
            if (cmsg == null)
            {
                return WSA_CMSG_FIRSTHDR(msg);
            }
            else
            {
                byte* ptr2 = msg->Control.buf + msg->Control.len;
                byte* ptr1 = (byte*)cmsg + cmsg->cmsg_len + sizeof(WSACMSGHDR);
                if (ptr1 > ptr2)
                {
                    return null;
                }
                else
                {
                    return (WSACMSGHDR*)((byte*)cmsg + cmsg->cmsg_len);
                }
            }

        }

        public static void* WSA_CMSG_DATA(WSACMSGHDR* cmsg)
        {
            return ((byte*)(cmsg) + WSA_CMSGDATA_ALIGN(sizeof(WSACMSGHDR)));
        }

        public static int WSA_CMSG_LEN(int length)
        {
            return WSA_CMSGDATA_ALIGN(sizeof(WSACMSGHDR)) + length;
        }

        public static int WSA_CMSG_SPACE(int length)
        {
            return WSA_CMSGDATA_ALIGN(sizeof(WSACMSGHDR) + WSA_CMSGHDR_ALIGN(length));
        }

        public static int WSA_CMSGHDR_ALIGN(int length)
        {
            return (length + TYPE_ALIGNMENT<WSACMSGHDR>() - 1) & (~(TYPE_ALIGNMENT<WSACMSGHDR>() - 1));
        }

        public static int WSA_CMSGDATA_ALIGN(int length)
        {
            return (((length) + MAX_NATURAL_ALIGNMENT() - 1) & (~(MAX_NATURAL_ALIGNMENT() - 1)));
        }

        public static int RIO_CMSG_BASE_SIZE()
        {
            return WSA_CMSGHDR_ALIGN(sizeof(RIO_CMSG_BUFFER));
        }

        [StructLayout(LayoutKind.Sequential)]
        struct Test<T> where T : struct
        {
            public byte b;
            public T value;
        }

        public static int TYPE_ALIGNMENT<T>() where T : struct
        {
            return (int)Marshal.OffsetOf<Test<T>>("value");
        }

        public static int MAX_NATURAL_ALIGNMENT()
        {
            return sizeof(ulong);
        }
    }
}

#endif