using System.Buffers.Binary;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace AKNet.Platform.Socket
{
    public class IPAddress
    {
        public static readonly IPAddress Any = new ReadOnlyIPAddress([0, 0, 0, 0]);
        public static readonly IPAddress Loopback = new ReadOnlyIPAddress([127, 0, 0, 1]);
        public static readonly IPAddress Broadcast = new ReadOnlyIPAddress([255, 255, 255, 255]);
        public static readonly IPAddress None = Broadcast;

        internal const uint LoopbackMaskHostOrder = 0xFF000000;
        public static readonly IPAddress IPv6Any = new IPAddress((ReadOnlySpan<byte>)[0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0], 0);
        public static readonly IPAddress IPv6Loopback = new IPAddress((ReadOnlySpan<byte>)[0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 1], 0);
        public static readonly IPAddress IPv6None = IPv6Any;

        private static readonly IPAddress s_loopbackMappedToIPv6 = new IPAddress((ReadOnlySpan<byte>)[0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 255, 255, 127, 0, 0, 1], 0);
        private uint _addressOrScopeId;
        private readonly ushort[]? _numbers;
        private int _hashCode;

        internal const int NumberOfLabels = IPAddressParserStatics.IPv6AddressBytes / 2;
        
        private bool IsIPv4
        {
            get { return _numbers == null; }
        }

        private bool IsIPv6
        {
            get { return _numbers != null; }
        }

        internal uint PrivateAddress
        {
            get
            {
                Debug.Assert(IsIPv4);
                return _addressOrScopeId;
            }
            private set
            {
                Debug.Assert(IsIPv4);
                _hashCode = 0;
                _addressOrScopeId = value;
            }
        }


        internal uint PrivateIPv4Address
        {
            get
            {
                Debug.Assert(IsIPv4 || IsIPv4MappedToIPv6);
                if (IsIPv4)
                {
                    return _addressOrScopeId;
                }
                uint address = (uint)_numbers[6] << 16 | (uint)_numbers[7];
                return (uint)HostToNetworkOrder(unchecked((int)address));
            }
        }

        private uint PrivateScopeId
        {
            get
            {
                Debug.Assert(IsIPv6);
                return _addressOrScopeId;
            }
            set
            {
                Debug.Assert(IsIPv6);
                _hashCode = 0;
                _addressOrScopeId = value;
            }
        }
        
        public IPAddress(long newAddress)
        {
            if (newAddress > 0x00000000FFFFFFFF)
            {
                throw new  ArgumentOutOfRangeException();
            }
            PrivateAddress = (uint)newAddress;
        }
        
        public IPAddress(byte[] address, long scopeid) : this(new ReadOnlySpan<byte>(address ?? ThrowAddressNullException()), scopeid)
        {
        }

        public IPAddress(ReadOnlySpan<byte> address, long scopeid)
        {
            if (address.Length != IPAddressParserStatics.IPv6AddressBytes)
            {
                throw new ArgumentException();
            }

            if (scopeid > 0x00000000FFFFFFFF)
            {
                throw new ArgumentOutOfRangeException();
            }

            _numbers = ReadUInt16NumbersFromBytes(address);
            PrivateScopeId = (uint)scopeid;
        }

        internal IPAddress(ReadOnlySpan<ushort> numbers, uint scopeid)
        {
            Debug.Assert(numbers.Length == NumberOfLabels);

            _numbers = numbers.ToArray();
            PrivateScopeId = scopeid;
        }

        private IPAddress(ushort[] numbers, uint scopeid)
        {
            Debug.Assert(numbers != null);
            Debug.Assert(numbers.Length == NumberOfLabels);

            _numbers = numbers;
            PrivateScopeId = scopeid;
        }
        
        public IPAddress(byte[] address) : this(new ReadOnlySpan<byte>(address ?? ThrowAddressNullException()))
        {
        }

        public IPAddress(ReadOnlySpan<byte> address)
        {
            if (address.Length == IPAddressParserStatics.IPv4AddressBytes)
            {
                PrivateAddress = MemoryMarshal.Read<uint>(address);
            }
            else if (address.Length == IPAddressParserStatics.IPv6AddressBytes)
            {
                _numbers = ReadUInt16NumbersFromBytes(address);
            }
            else
            {
                throw new ArgumentException();
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static ushort[] ReadUInt16NumbersFromBytes(ReadOnlySpan<byte> address)
        {
            ushort[] numbers = new ushort[NumberOfLabels];
            for (int i = 0; i < numbers.Length; i++)
            {
                numbers[i] = BinaryPrimitives.ReadUInt16BigEndian(address.Slice(i * 2));
            }
            return numbers;
        }
        
        internal IPAddress(int newAddress)
        {
            PrivateAddress = (uint)newAddress;
        }
        
        public static bool TryParse(string? ipString, out IPAddress? address)
        {
            if (ipString == null)
            {
                address = null;
                return false;
            }

            address = IPAddressParser.Parse(ipString, tryParse: true);
            return (address != null);
        }

        public static IPAddress Parse(string ipString)
        {
            if (string.IsNullOrWhiteSpace(ipString))
            {
                throw new ArgumentNullException();
            }
            return IPAddressParser.Parse(ipString, tryParse: false)!;
        }

        public bool TryWriteBytes(Span<byte> destination, out int bytesWritten)
        {
            if (IsIPv6)
            {
                if (destination.Length < IPAddressParserStatics.IPv6AddressBytes)
                {
                    bytesWritten = 0;
                    return false;
                }

                WriteIPv6Bytes(destination);
                bytesWritten = IPAddressParserStatics.IPv6AddressBytes;
            }
            else
            {
                if (destination.Length < IPAddressParserStatics.IPv4AddressBytes)
                {
                    bytesWritten = 0;
                    return false;
                }

                WriteIPv4Bytes(destination);
                bytesWritten = IPAddressParserStatics.IPv4AddressBytes;
            }

            return true;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void WriteIPv6Bytes(Span<byte> destination)
        {
            ushort[]? numbers = _numbers;
            Debug.Assert(numbers != null && numbers.Length == NumberOfLabels);

            if (BitConverter.IsLittleEndian)
            {
                for (int i = 0; i < numbers.Length; i++)
                {
                    BinaryPrimitives.WriteUInt16BigEndian(destination.Slice(i * 2), numbers[i]);
                }
            }
            else
            {
                MemoryMarshal.AsBytes<ushort>(numbers).CopyTo(destination);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void WriteIPv4Bytes(Span<byte> destination)
        {
            uint address = PrivateAddress;
            MemoryMarshal.Write(destination, ref address);
        }
        
        public byte[] GetAddressBytes()
        {
            if (IsIPv6)
            {
                Debug.Assert(_numbers != null && _numbers.Length == NumberOfLabels);
                byte[] bytes = new byte[IPAddressParserStatics.IPv6AddressBytes];
                WriteIPv6Bytes(bytes);
                return bytes;
            }
            else
            {
                byte[] bytes = new byte[IPAddressParserStatics.IPv4AddressBytes];
                WriteIPv4Bytes(bytes);
                return bytes;
            }
        }

        public AddressFamily AddressFamily
        {
            get
            {
                return IsIPv4 ? AddressFamily.InterNetwork : AddressFamily.InterNetworkV6;
            }
        }
        
        public uint ScopeId
        {
            get
            {
                if (IsIPv4)
                {
                    ThrowSocketOperationNotSupported();
                }

                return PrivateScopeId;
            }
            set
            {
                if (IsIPv4)
                {
                    ThrowSocketOperationNotSupported();
                }

                if(value < 0 || value > 0xFFFFFFFF)
                {
                    throw new ArgumentOutOfRangeException();
                }
                PrivateScopeId = (uint)value;
            }
        }
        
        public static long HostToNetworkOrder(long host)
        {
            return BitConverter.IsLittleEndian ? BinaryPrimitives.ReverseEndianness(host) : host;
        }

        public static int HostToNetworkOrder(int host)
        {
            return BitConverter.IsLittleEndian ? BinaryPrimitives.ReverseEndianness(host) : host;
        }

        public static short HostToNetworkOrder(short host)
        {
            return BitConverter.IsLittleEndian ? BinaryPrimitives.ReverseEndianness(host) : host;
        }

        public static long NetworkToHostOrder(long network)
        {
            return HostToNetworkOrder(network);
        }

        public static int NetworkToHostOrder(int network)
        {
            return HostToNetworkOrder(network);
        }

        public static short NetworkToHostOrder(short network)
        {
            return HostToNetworkOrder(network);
        }

        public static bool IsLoopback(IPAddress address)
        {
            if (address == null)
            {
               throw new ArgumentNullException();
            }

            if (address.IsIPv6)
            {
                return address.Equals(IPv6Loopback) || address.Equals(s_loopbackMappedToIPv6);
            }
            else
            {
                long LoopbackMask = (uint)HostToNetworkOrder(unchecked((int)LoopbackMaskHostOrder));
                return ((address.PrivateAddress & LoopbackMask) == (Loopback.PrivateAddress & LoopbackMask));
            }
        }
        
        public bool IsIPv6Multicast
        {
            get
            {
                return IsIPv6 && ((_numbers[0] & 0xFF00) == 0xFF00);
            }
        }
        
        public bool IsIPv6LinkLocal
        {
            get
            {
                return IsIPv6 && ((_numbers[0] & 0xFFC0) == 0xFE80);
            }
        }
        
        public bool IsIPv6SiteLocal
        {
            get
            {
                return IsIPv6 && ((_numbers[0] & 0xFFC0) == 0xFEC0);
            }
        }

        public bool IsIPv6Teredo
        {
            get
            {
                return IsIPv6 && (_numbers[0] == 0x2001) && (_numbers[1] == 0);
            }
        }
        
        public bool IsIPv6UniqueLocal
        {
            get
            {
                return IsIPv6 && ((_numbers[0] & 0xFE00) == 0xFC00);
            }
        }
        
        public bool IsIPv4MappedToIPv6
        {
            get
            {
                if (IsIPv4)
                {
                    return false;
                }

                ReadOnlySpan<byte> numbers = MemoryMarshal.AsBytes(new ReadOnlySpan<ushort>(_numbers));
                return MemoryMarshal.Read<ulong>(numbers) == 0 && BinaryPrimitives.ReadUInt32LittleEndian(numbers.Slice(8)) == 0xFFFF0000;
            }
        }

        /// <summary>Compares two IP addresses.</summary>
        public override bool Equals([NotNullWhen(true)] object? comparand)
        {
            return comparand is IPAddress address && Equals(address);
        }

        internal bool Equals(IPAddress comparand)
        {
            Debug.Assert(comparand != null);
            if (AddressFamily != comparand.AddressFamily)
            {
                return false;
            }

            if (IsIPv6)
            {
                ReadOnlySpan<byte> thisNumbers = MemoryMarshal.AsBytes<ushort>(_numbers);
                ReadOnlySpan<byte> comparandNumbers = MemoryMarshal.AsBytes<ushort>(comparand._numbers);
                return
                    MemoryMarshal.Read<ulong>(thisNumbers) == MemoryMarshal.Read<ulong>(comparandNumbers) &&
                    MemoryMarshal.Read<ulong>(thisNumbers.Slice(sizeof(ulong))) == MemoryMarshal.Read<ulong>(comparandNumbers.Slice(sizeof(ulong))) &&
                    PrivateScopeId == comparand.PrivateScopeId;
            }
            else
            {
                return comparand.PrivateAddress == PrivateAddress;
            }
        }

        public override int GetHashCode()
        {
            if (_hashCode == 0)
            {
                if (IsIPv6)
                {
                    ReadOnlySpan<byte> numbers = MemoryMarshal.AsBytes<ushort>(_numbers);
                    _hashCode = HashCode.Combine(
                        MemoryMarshal.Read<uint>(numbers),
                        MemoryMarshal.Read<uint>(numbers.Slice(4)),
                        MemoryMarshal.Read<uint>(numbers.Slice(8)),
                        MemoryMarshal.Read<uint>(numbers.Slice(12)),
                        _addressOrScopeId);
                }
                else
                {
                    _hashCode = HashCode.Combine(_addressOrScopeId);
                }
            }

            return _hashCode;
        }
        
        public IPAddress MapToIPv6()
        {
            if (IsIPv6)
            {
                return this;
            }

            uint address = (uint)NetworkToHostOrder(unchecked((int)PrivateAddress));
            ushort[] labels = new ushort[NumberOfLabels];
            labels[5] = 0xFFFF;
            labels[6] = (ushort)(address >> 16);
            labels[7] = (ushort)address;
            return new IPAddress(labels, 0);
        }
        
        public IPAddress MapToIPv4()
        {
            if (IsIPv4)
            {
                return this;
            }

            uint address = (uint)_numbers[6] << 16 | (uint)_numbers[7];
            return new IPAddress((uint)HostToNetworkOrder(unchecked((int)address)));
        }

        [DoesNotReturn]
        private static byte[] ThrowAddressNullException() => throw new ArgumentNullException("address");

        [DoesNotReturn]
        private static void ThrowSocketOperationNotSupported() => throw new SocketException(SocketError.OperationNotSupported);

        private sealed class ReadOnlyIPAddress : IPAddress
        {
            public ReadOnlyIPAddress(ReadOnlySpan<byte> newAddress) : base(newAddress)
            { }
        }
    }
}
