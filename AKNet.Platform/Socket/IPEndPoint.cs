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
                _port = value;
            }
        }

        public static bool TryParse(string s, [NotNullWhen(true)] out IPEndPoint? result)
        {
            int addressLength = s.Length; 
            int lastColonPos = s.LastIndexOf(':');
            
            if (lastColonPos > 0)
            {
                if (s[lastColonPos - 1] == ']')
                {
                    addressLength = lastColonPos;
                }

                else if (s.Substring(0, lastColonPos).LastIndexOf(':') == -1)
                {
                    addressLength = lastColonPos;
                }
            }

            if (IPAddress.TryParse(s.Substring(0, addressLength), out IPAddress? address))
            {
                uint port = 0;
                if (addressLength == s.Length ||
                    (uint.TryParse(s.Substring(addressLength + 1), NumberStyles.None, CultureInfo.InvariantCulture, out port) && port <= MaxPort))

                {
                    result = new IPEndPoint(address, (int)port);
                    return true;
                }
            }

            result = null;
            return false;
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
                SocketAddressPal.SetIPv4Address(socketAddressBuffer, (uint)address.PrivateAddress);
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

        public void Serialize(Span<byte> destination)
        {
            SocketAddressPal.SetAddressFamily(destination, AddressFamily);
            SetIPAddress(destination, Address);
            SocketAddressPal.SetPort(destination, (ushort)Port);
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
