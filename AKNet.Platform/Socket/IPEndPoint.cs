using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;

namespace AKNet.Platform.Socket
{
    public class IPEndPoint : EndPoint
    {
        public const int MinPort = 0x00000000;
        public const int MaxPort = 0x0000FFFF;
        private IPAddress _address;
        private int _port;

        public override AddressFamily AddressFamily => _address.AddressFamily;
        
        public IPEndPoint(long address, int port)
        {
            _port = port;
            _address = new IPAddress(address);
        }
        
        public IPEndPoint(IPAddress address, int port)
        {
            if (address == null)
            {
                throw new ArgumentNullException();
            }

            _port = port;
            _address = address;
        }
        
        public IPAddress Address
        {
            get => _address;
            set
            {
                if (value == null)
                {
                    throw new ArgumentNullException();
                }
                _address = value;
            }
        }
        
        public int Port
        {
            get => _port;
            set
            {
                if (!TcpValidationHelpers.ValidatePortNumber(value))
                {
                    throw new ArgumentOutOfRangeException(nameof(value));
                }

                _port = value;
            }
        }

        public static bool TryParse(string s, [NotNullWhen(true)] out IPEndPoint? result)
        {
            return TryParse(s.AsSpan(), out result);
        }

        public static bool TryParse(ReadOnlySpan<char> s, [NotNullWhen(true)] out IPEndPoint? result)
        {
            int addressLength = s.Length;  // If there's no port then send the entire string to the address parser
            int lastColonPos = s.LastIndexOf(':');

            // Look to see if this is an IPv6 address with a port.
            if (lastColonPos > 0)
            {
                if (s[lastColonPos - 1] == ']')
                {
                    addressLength = lastColonPos;
                }
                // Look to see if this is IPv4 with a port (IPv6 will have another colon)
                else if (s.Slice(0, lastColonPos).LastIndexOf(':') == -1)
                {
                    addressLength = lastColonPos;
                }
            }

            if (IPAddress.TryParse(s.Slice(0, addressLength), out IPAddress? address))
            {
                uint port = 0;
                if (addressLength == s.Length ||
                    (uint.TryParse(s.Slice(addressLength + 1), NumberStyles.None, CultureInfo.InvariantCulture, out port) && port <= MaxPort))

                {
                    result = new IPEndPoint(address, (int)port);
                    return true;
                }
            }

            result = null;
            return false;
        }

        public static IPEndPoint Parse(string s)
        {
            if (s == null)
            {
                throw new ArgumentNullException();
            }
            return Parse(s.AsSpan());
        }

        public static IPEndPoint Parse(ReadOnlySpan<char> s)
        {
            if (TryParse(s, out IPEndPoint? result))
            {
                return result;
            }

            throw new FormatException();
        }

        public override string ToString()
        {
            return _address.AddressFamily == AddressFamily.InterNetworkV6 ? $"[{_address}]:{_port}" : $"{_address}:{_port}";
        }
        public override SocketAddress Serialize() => new SocketAddress(Address, Port);

        public override EndPoint Create(SocketAddress socketAddress)
        {
            if (socketAddress == null)
            {
                throw new ArgumentNullException();
            }

            if (socketAddress.Family is not (AddressFamily.InterNetwork or AddressFamily.InterNetworkV6))
            {
                throw new ArgumentException();
            }

            int minSize = AddressFamily == AddressFamily.InterNetworkV6 ? SocketAddress.IPv6AddressSize : SocketAddress.IPv4AddressSize;
            if (socketAddress.Size < minSize)
            {
                throw new ArgumentException();
            }

            return socketAddress.GetIPEndPoint();
        }

        public override bool Equals([NotNullWhen(true)] object? comparand)
        {
            return comparand is IPEndPoint other && other._address.Equals(_address) && other._port == _port;
        }

        public override int GetHashCode()
        {
            return _address.GetHashCode() ^ _port;
        }

        public static IPAddress GetIPAddress(ReadOnlySpan<byte> socketAddressBuffer)
        {
            AddressFamily family = SocketAddressPal.GetAddressFamily(socketAddressBuffer);

            if (family == AddressFamily.InterNetworkV6)
            {
                Span<byte> address = stackalloc byte[IPAddressParserStatics.IPv6AddressBytes];
                uint scope;
                SocketAddressPal.GetIPv6Address(socketAddressBuffer, address, out scope);
                return new IPAddress(address, (address[0] == 0xFE && (address[1] & 0xC0) == 0x80) ? (long)scope : 0);
            }
            else if (family == AddressFamily.InterNetwork)
            {
                return new IPAddress((long)SocketAddressPal.GetIPv4Address(socketAddressBuffer) & 0x0FFFFFFFF);
            }

            throw new SocketException((int)SocketError.AddressFamilyNotSupported);
        }

        public static void SetIPAddress(Span<byte> socketAddressBuffer, IPAddress address)
        {
            SocketAddressPal.SetAddressFamily(socketAddressBuffer, address.AddressFamily);
            SocketAddressPal.SetPort(socketAddressBuffer, 0);
            if (address.AddressFamily == AddressFamily.InterNetwork)
            {
                SocketAddressPal.SetIPv4Address(socketAddressBuffer, (uint)address.Address);
            }
            else
            {
                Span<byte> addressBuffer = stackalloc byte[IPAddressParserStatics.IPv6AddressBytes];
                address.TryWriteBytes(addressBuffer, out int written);
                Debug.Assert(written == IPAddressParserStatics.IPv6AddressBytes);
                SocketAddressPal.SetIPv6Address(socketAddressBuffer, addressBuffer, (uint)address.ScopeId);
            }
        }

        public static IPEndPoint CreateIPEndPoint(ReadOnlySpan<byte> socketAddressBuffer)
        {
            return new IPEndPoint(GetIPAddress(socketAddressBuffer), SocketAddressPal.GetPort(socketAddressBuffer));
        }

        public static void Serialize(this IPEndPoint endPoint, Span<byte> destination)
        {
            SocketAddressPal.SetAddressFamily(destination, endPoint.AddressFamily);
            SetIPAddress(destination, endPoint.Address);
            SocketAddressPal.SetPort(destination, (ushort)endPoint.Port);
        }

        public bool Equals(ReadOnlySpan<byte> socketAddressBuffer)
        {
            if (socketAddressBuffer.Length >= SocketAddress.GetMaximumAddressSize(AddressFamily) &&
                AddressFamily == SocketAddressPal.GetAddressFamily(socketAddressBuffer) &&
                Port == (int)SocketAddressPal.GetPort(socketAddressBuffer))
            {
                if (AddressFamily == AddressFamily.InterNetwork)
                {
                    return _address.PrivateAddress == (long)SocketAddressPal.GetIPv4Address(socketAddressBuffer);
                }
                else
                {
                    Span<byte> addressBuffer1 = stackalloc byte[IPAddressParserStatics.IPv6AddressBytes];
                    Span<byte> addressBuffer2 = stackalloc byte[IPAddressParserStatics.IPv6AddressBytes];
                    SocketAddressPal.GetIPv6Address(socketAddressBuffer, addressBuffer1, out uint scopeid);
                    if (Address.ScopeId != (long)scopeid)
                    {
                        return false;
                    }
                    Address.TryWriteBytes(addressBuffer2, out _);
                    return addressBuffer1.SequenceEqual(addressBuffer2);
                }
            }

            return false;
        }
    }
}
