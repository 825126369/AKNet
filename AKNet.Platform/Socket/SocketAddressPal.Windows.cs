#if TARGET_WINDOWS
using System.Buffers.Binary;
namespace AKNet.Platform.Socket
{
    internal static class SocketAddressPal
    {
        public const int IPv6AddressSize = 28;
        public const int IPv4AddressSize = 16;
        public const int UdsAddressSize = 110;
        public const int MaxAddressSize = 128;

        public static AddressFamily GetAddressFamily(ReadOnlySpan<byte> buffer)
        {
            return (AddressFamily)BitConverter.ToInt16(buffer);
        }

        public static void SetAddressFamily(Span<byte> buffer, AddressFamily family)
        {
            if ((int)(family) > ushort.MaxValue)
            {
                throw new PlatformNotSupportedException();
            }
#if BIGENDIAN
            BinaryPrimitives.WriteUInt16BigEndian(buffer, (ushort)family);
#else
            BinaryPrimitives.WriteUInt16LittleEndian(buffer, (ushort)family);
#endif
        }

        public static ushort GetPort(ReadOnlySpan<byte> buffer)
            => BinaryPrimitives.ReadUInt16BigEndian(buffer.Slice(2));

        public static void SetPort(Span<byte> buffer, ushort port)
            => BinaryPrimitives.WriteUInt16BigEndian(buffer.Slice(2), port);

        public static uint GetIPv4Address(ReadOnlySpan<byte> buffer)
            => BinaryPrimitives.ReadUInt32LittleEndian(buffer.Slice(4));

        public static void GetIPv6Address(ReadOnlySpan<byte> buffer, Span<byte> address, out uint scope)
        {
            buffer.Slice(8, address.Length).CopyTo(address);
            scope = BinaryPrimitives.ReadUInt32LittleEndian(buffer.Slice(24));
        }

        public static void SetIPv4Address(Span<byte> buffer, uint address)
        {
            BinaryPrimitives.WriteUInt32LittleEndian(buffer.Slice(4), address);
        }

        public static void SetIPv6Address(Span<byte> buffer, Span<byte> address, uint scope)
        {
            BinaryPrimitives.WriteUInt32LittleEndian(buffer.Slice(4), 0);
            BinaryPrimitives.WriteUInt32LittleEndian(buffer.Slice(24), scope);
            address.CopyTo(buffer.Slice(8));
        }

        public static unsafe void Clear(Span<byte> buffer)
        {
            AddressFamily family = GetAddressFamily(buffer);
            buffer.Clear();
            SetAddressFamily(buffer, family);
        }
    }
}
#endif
