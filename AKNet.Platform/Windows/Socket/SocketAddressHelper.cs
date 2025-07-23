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

        public static ReadOnlySpan<byte> GetBindAddr(IPEndPoint mIpEndPoint)
        {
            const int IPv6AddressSize = 28;
            const int IPv4AddressSize = 16;
            //Family --2个字节
            //端口 --2个字节
            //地址 --
#if NET8_0
            var RawAddr = mIpEndPoint.Serialize();
            return RawAddr.Buffer.Span.Slice(0, RawAddr.Size);
#else
            SocketAddress RawAddr = mIpEndPoint.Serialize();
            Span<byte> buffer = new byte[RawAddr.Size];
            for (int i = 0; i < buffer.Length; i++)
            {
                buffer[i] = RawAddr[i];
            }
            return buffer;
#endif
        }

        public static IPEndPoint RawAddrTo(SOCKADDR_INET sockaddr)
        {
            if (sockaddr.si_family == OSPlatformFunc.AF_INET6) // AF_INET (IPv4)
            {
                var addr = new IPAddress(new ReadOnlySpan<byte>((void*)sockaddr.Ipv4.sin_addr.u, 4));
                int port = (int)IPAddress.NetworkToHostOrder((short)sockaddr.Ipv4.sin_port);
                return new IPEndPoint(addr, port);
            }
            else if (sockaddr.si_family == OSPlatformFunc.AF_INET) // AF_INET6 (IPv6)
            {
                var addr = new IPAddress(new ReadOnlySpan<byte>((void*)sockaddr.Ipv6.sin6_addr.u, 16));
                int port = (int)IPAddress.NetworkToHostOrder((short)sockaddr.Ipv6.sin6_port);
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
    }
}
