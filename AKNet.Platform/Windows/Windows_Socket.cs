#if TARGET_WINDOWS
using AKNet.Platform;
using System;
using System.Net.Sockets;
using System.Runtime.InteropServices.JavaScript;

namespace AKNet.Platform
{
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

    public struct IN_ADDR
    {
        public struct S_un_DATA
        {
            struct S_un_b_DATA 
            { 
                public byte s_b1, s_b2, s_b3, s_b4; 
            }
            public struct S_un_w_DATA
            {
                public ushort s_w1, s_w2; 
            }

            public S_un_b_DATA S_un_b;
            public S_un_b_DATA S_un_w;
            public ulong S_addr;
        }
        public S_un_DATA S_un;

        #define s_addr  S_un.S_addr /* can be used for most tcp & ip code */
        #define s_host  S_un.S_un_b.s_b2    // host on imp
        #define s_net   S_un.S_un_b.s_b1    // network
        #define s_imp   S_un.S_un_w.s_w2    // imp
        #define s_impno S_un.S_un_b.s_b4    // imp #
        #define s_lh    S_un.S_un_b.s_b3    // logical host
    }
    
    public unsafe struct SOCKADDR_IN
    {
        public ushort sin_family;
        public ushort sin_port;
        IN_ADDR sin_addr;
        public fixed byte sin_zero[8];
    }

    public static unsafe partial class OSPlatformFunc
    {
        public const int AF_INET6 = 23;            // Internetwork Version 6
        public const int SOCK_DGRAM = 2;             /* datagram socket */
        public const int IPPROTO_UDP = 17;
        public const int IPPROTO_IP = 0;

        public const int WSA_FLAG_OVERLAPPED = 0x01;
        public const int WSA_FLAG_REGISTERED_IO = 0x100;
        public const int IPPROTO_IPV6 = 41;
        public const int IPV6_V6ONLY = 27;
        
        public const ulong INVALID_SOCKET = (ulong)(~0UL);
        public const int SOCKET_ERROR = (-1);
        public const int NO_ERROR = 0;

        public const byte FILE_SKIP_COMPLETION_PORT_ON_SUCCESS = 0x1;
        public const byte FILE_SKIP_SET_EVENT_ON_HANDLE = 0x2;
        
        public const uint IOC_IN = 0x80000000;
        public const uint IOC_VENDOR = 0x18000000;
        public static readonly uint SIO_CPU_AFFINITY = _WSAIOW(IOC_VENDOR, 21U);
        public static readonly uint SIO_ACQUIRE_PORT_RESERVATION = _WSAIOW(IOC_VENDOR, 100);
        public static readonly uint SIO_ASSOCIATE_PORT_RESERVATION = _WSAIOW(IOC_VENDOR, 102);

        public const int IP_DONTFRAGMENT = 14; // Don't fragment IP datagrams.
        public const int IPV6_DONTFRAG = 14; // Don't fragment IP datagrams.
        public const int IPV6_PKTINFO = 19; // Receive packet information.

        //IPV6_RECVTCLASS 是一个用于 IPv6 套接字编程的选项，用于启用接收端获取数据包的 TClass（Traffic Class）字段值。
        //这个字段在 IPv6 报文中用于标识数据包的流量类别，类似于 IPv4 中的 TOS（Type of Service）字段。
        public const int IPV6_RECVTCLASS = 40;
        public const int IP_RECVTOS = 40; // Receive packet Type Of Service (TOS)
        public const int IP_ECN = 50;
        public const int IPV6_ECN = 50; // IPv6 ECN codepoint.
        public const int IP_HOPLIMIT = 21;
        public const int IPV6_HOPLIMIT = 21;

        public const int SOL_SOCKET = 0xffff;
        public const int SO_RCVBUF = 0x1002;
        public const int UDP_RECV_MAX_COALESCED_SIZE = 3;

        public static uint _WSAIOW(uint x, uint y)
        {
            return (IOC_IN | (x) | (y));
        }
    }
}

#endif