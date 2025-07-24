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
                var addr = new SOCKADDR_INET();
                addr.Ipv4.sin_family = OSPlatformFunc.AF_INET; // AF_INET
                addr.Ipv4.sin_port = (ushort)IPAddress.HostToNetworkOrder((short)endPoint.Port);

                Span<byte> addrSpan = new Span<byte>((void*)addr.Ipv4.sin_addr.u, 4);
                endPoint.Address.TryWriteBytes(addrSpan, out _);

                IntPtr pAddr = Marshal.AllocHGlobal(Marshal.SizeOf<SOCKADDR_INET>());
                Marshal.StructureToPtr(addr, pAddr, false);
                addressLen = Marshal.SizeOf(addr.Ipv4);
                return (SOCKADDR_INET*)pAddr;
            }
            else if (endPoint.AddressFamily == AddressFamily.InterNetworkV6) // IPv6
            {
                var addr = new SOCKADDR_INET();
                addr.Ipv6.sin6_family = OSPlatformFunc.AF_INET; // AF_INET
                addr.Ipv6.sin6_port = (ushort)IPAddress.HostToNetworkOrder((short)endPoint.Port);
                addr.Ipv6.sin6_flowinfo = 0;
                addr.Ipv6.sin6_scope_id = (uint)endPoint.Address.ScopeId;
                Span<byte> addrSpan = new Span<byte>((void*)addr.Ipv6.sin6_addr.u, 4);
                endPoint.Address.TryWriteBytes(addrSpan, out _);

                IntPtr pAddr = Marshal.AllocHGlobal(Marshal.SizeOf<SOCKADDR_INET>());
                Marshal.StructureToPtr(addr, pAddr, false);
                addressLen = Marshal.SizeOf(addr.Ipv6);
                return (SOCKADDR_INET*)pAddr;
            }
            else
            {
                throw new NotSupportedException("不支持的地址族");
            }
        }

        public static IPEndPoint RawAddrTo(SOCKADDR_INET* sockaddr)
        {
            if (sockaddr->si_family == OSPlatformFunc.AF_INET6) // AF_INET (IPv4)
            {
                var addr = new IPAddress(new ReadOnlySpan<byte>((void*)sockaddr->Ipv4.sin_addr.u, 4));
                int port = (int)IPAddress.NetworkToHostOrder((short)sockaddr->Ipv4.sin_port);
                return new IPEndPoint(addr, port);
            }
            else if (sockaddr->si_family == OSPlatformFunc.AF_INET) // AF_INET6 (IPv6)
            {
                var addr = new IPAddress(new ReadOnlySpan<byte>((void*)sockaddr->Ipv6.sin6_addr.u, 16));
                int port = (int)IPAddress.NetworkToHostOrder((short)sockaddr->Ipv6.sin6_port);
                return new IPEndPoint(addr, port);
            }
            else
            {
                throw new NotSupportedException("Unsupported address family.");
            }
        }

        public static IPEndPoint GetLocalEndPoint(SafeHandle Socket, AddressFamily family)
        {
            int Result = 0;
            Span<byte> buffer = stackalloc byte[30];
            int bufferLength = buffer.Length;
            fixed (byte* mTempPtr = buffer)
            {
                Result = Interop.Winsock.getsockname(Socket, mTempPtr, out bufferLength);
            }

            if (Result == OSPlatformFunc.SOCKET_ERROR)
            {
                return null;
            }

            buffer = buffer.Slice(0, bufferLength);
            NetLog.Assert(bufferLength <= buffer.Length);
            switch (family)
            {
                case AddressFamily.InterNetwork:
                    {
                        ushort nPort = BinaryPrimitives.ReadUInt16BigEndian(buffer.Slice(2));
                        IPAddress mAddress = new IPAddress(BinaryPrimitives.ReadUInt32LittleEndian(buffer.Slice(4)));
                        return new IPEndPoint(mAddress, nPort);
                    }
                case AddressFamily.InterNetworkV6:
                    {
                        ushort nPort = BinaryPrimitives.ReadUInt16BigEndian(buffer.Slice(2));
                        uint scope = BinaryPrimitives.ReadUInt32LittleEndian(buffer.Slice(24));
                        IPAddress mAddress = new IPAddress(buffer.Slice(8), scope);
                        return new IPEndPoint(mAddress, (int)nPort);
                    }
                default:
                    return null;
            }
        }

        public static void CxPlatConvertFromMappedV6(SOCKADDR_INET* InAddr, SOCKADDR_INET* OutAddr)
        {
            NetLog.Assert(InAddr->si_family == OSPlatformFunc.AF_INET6);
            if (Interop.Winsock.IN6_IS_ADDR_V4MAPPED(&InAddr->Ipv6.sin6_addr))
            {
                OutAddr->si_family = OSPlatformFunc.AF_INET;
                OutAddr->Ipv4.sin_port = InAddr->Ipv6.sin6_port;
                OutAddr->Ipv4.sin_addr = *(IN_ADDR*)Interop.Winsock.IN6_GET_ADDR_V4MAPPED(&InAddr->Ipv6.sin6_addr);
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
                ulong unspecified_scope = 0;
                Interop.Winsock.IN6ADDR_SETV4MAPPED(&OutAddr->Ipv6, &InAddr->Ipv4.sin_addr, unspecified_scope, InAddr->Ipv4.sin_port);
            } 
            else
            {
                *OutAddr = *InAddr;
            }
        }

    }
}
