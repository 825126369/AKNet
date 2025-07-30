using System.Buffers.Binary;
using System.Net;
using System.Net.Sockets;
using System.Runtime.InteropServices;

namespace AKNet.Platform
{
    public unsafe static class SocketAddressHelper
    {
        public const int IPv6AddressSize = 28;
        public const int IPv4AddressSize = 16;
        public const int MaxAddressSize = 128;

        public static int GetMaximumAddressSize(AddressFamily addressFamily = AddressFamily.Unspecified)
        {
            switch (addressFamily)
            {
                case AddressFamily.InterNetwork:
                    return IPv4AddressSize;
                case AddressFamily.InterNetworkV6:
                    return IPv6AddressSize;
                default:
                    return MaxAddressSize;
            }
        }

        public static SOCKADDR_INET* GetRawAddr(IPEndPoint endPoint, out int addressLen)
        {
            if (endPoint.AddressFamily == AddressFamily.InterNetwork) // IPv4
            {
                SOCKADDR_INET* pAddr = (SOCKADDR_INET*)OSPlatformFunc.CxPlatAlloc(sizeof(SOCKADDR_INET));
                pAddr->Ipv4.sin_family = OSPlatformFunc.AF_INET; // AF_INET
                pAddr->Ipv4.sin_port = (ushort)endPoint.Port;

                Span<byte> addrSpan = new Span<byte>(pAddr->Ipv4.sin_addr.u, 4);
                endPoint.Address.TryWriteBytes(addrSpan, out _);
                addressLen = Marshal.SizeOf(pAddr->Ipv4);
                return pAddr;
            }
            else if (endPoint.AddressFamily == AddressFamily.InterNetworkV6) // IPv6
            {
                SOCKADDR_INET* pAddr = (SOCKADDR_INET*)OSPlatformFunc.CxPlatAlloc(sizeof(SOCKADDR_INET));
                pAddr->Ipv6.sin6_family = OSPlatformFunc.AF_INET6; // AF_INET
                pAddr->Ipv6.sin6_port = (ushort)endPoint.Port;
                pAddr->Ipv6.sin6_flowinfo = 0;
                pAddr->Ipv6.sin6_scope_id = (uint)endPoint.Address.ScopeId;
                Span<byte> addrSpan = new Span<byte>(pAddr->Ipv6.sin6_addr.u, 16);
                endPoint.Address.TryWriteBytes(addrSpan, out _);
                addressLen = Marshal.SizeOf(pAddr->Ipv6);
                return (SOCKADDR_INET*)pAddr;
            }
            else
            {
                throw new NotSupportedException("不支持的地址族");
            }
        }

        public static IPEndPoint RawAddrTo(SOCKADDR_INET* sockaddr)
        {
            if (sockaddr->si_family == OSPlatformFunc.AF_INET) // AF_INET (IPv4)
            {
                var addr = new IPAddress(new ReadOnlySpan<byte>(sockaddr->Ipv4.sin_addr.u, 4));
                int port = (short)sockaddr->Ipv4.sin_port;
                return new IPEndPoint(addr, port);
            }
            else if (sockaddr->si_family == OSPlatformFunc.AF_INET6) // AF_INET6 (IPv6)
            {
                var addr = new IPAddress(new ReadOnlySpan<byte>(sockaddr->Ipv6.sin6_addr.u, 16));
                int port = (short)sockaddr->Ipv6.sin6_port;
                return new IPEndPoint(addr, port);
            }
            else
            {
                throw new NotSupportedException("Unsupported address family.");
            }
        }

        public static void CxPlatConvertFromMappedV6(SOCKADDR_INET* InAddr, SOCKADDR_INET* OutAddr)
        {
            NetLog.Assert(InAddr->si_family == OSPlatformFunc.AF_INET6);
            if (IN6_IS_ADDR_V4MAPPED(&InAddr->Ipv6.sin6_addr))
            {
                OutAddr->si_family = OSPlatformFunc.AF_INET;
                OutAddr->Ipv4.sin_port = InAddr->Ipv6.sin6_port;
                OutAddr->Ipv4.sin_addr = *(IN_ADDR*)IN6_GET_ADDR_V4MAPPED(&InAddr->Ipv6.sin6_addr);
            }
            else if (OutAddr != InAddr)
            {
                *OutAddr = *InAddr;
            }
        }

        public static void CxPlatConvertToMappedV6(SOCKADDR_INET* InAddr, SOCKADDR_INET* OutAddr)
        {
            if (InAddr->si_family == OSPlatformFunc.AF_INET)
            {
                uint unspecified_scope = 0;
                IN6ADDR_SETV4MAPPED(&OutAddr->Ipv6, &InAddr->Ipv4.sin_addr, unspecified_scope, InAddr->Ipv4.sin_port);
            } 
            else
            {
                *OutAddr = *InAddr;
            }
        }

        public static bool IN6_IS_ADDR_V4MAPPED(IN6_ADDR* a)
        {
            return (bool)((a->u[0] == 0) && (a->u[1] == 0) &&
                 (a->u[2] == 0) && (a->u[3] == 0) &&
                 (a->u[4] == 0) && (a->u[5] == 0) &&
                 (a->u[6] == 0) && (a->u[7] == 0) &&
                 (a->u[8] == 0) && (a->u[9] == 0) &&
                 (a->u[10] == 0xff) && (a->u[9] == 0xff));
        }

        public static byte* IN6_GET_ADDR_V4MAPPED(IN6_ADDR* Ipv6Address)
        {
            return (Ipv6Address->u + 6);
        }

        public static void IN6ADDR_SETV4MAPPED(SOCKADDR_IN6* a6, IN_ADDR* a4, uint scope, ushort port)
        {
            a6->sin6_family = OSPlatformFunc.AF_INET6;
            a6->sin6_port = port;
            a6->sin6_flowinfo = 0;
            IN6_SET_ADDR_V4MAPPED(&a6->sin6_addr, a4);
            a6->sin6_scope_id = scope;
            IN4_UNCANONICALIZE_SCOPE_ID(a4, &a6->sin6_scope_id);
        }

        public static void IN6_SET_ADDR_V4MAPPED(IN6_ADDR* a6, IN_ADDR* a4)
        {
            *a6 = new IN6_ADDR();
            a6->u[10] = 0xFF;
            a6->u[11] = 0xFF;
            a6->u[12] = a4->u[0];
            a6->u[13] = a4->u[1];
            a6->u[14] = a4->u[2];
            a6->u[15] = a4->u[3];
        }

        static void IN4_UNCANONICALIZE_SCOPE_ID(IN_ADDR* Address, uint* ScopeId)
        {
            *ScopeId = 0;
        }




    }
}
