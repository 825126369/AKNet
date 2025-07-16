using System.Diagnostics;
using System.Globalization;
using System.Runtime.InteropServices;

namespace AKNet.Socket
{
    internal static class IPAddressParser
    {
        internal const int MaxIPv4StringLength = 15;
        internal const int MaxIPv6StringLength = 65;

        internal static IPAddress? Parse(string ipSpan, bool tryParse)
        {
            if (ipSpan.Contains(':'))
            {
                Span<ushort> numbers = stackalloc ushort[IPAddressParserStatics.IPv6AddressShorts];
                numbers.Clear();
                if (TryParseIPv6(ipSpan, numbers, IPAddressParserStatics.IPv6AddressShorts, out uint scope))
                {
                    return new IPAddress(numbers, scope);
                }
            }
            else if (TryParseIpv4(ipSpan, out long address))
            {
                return new IPAddress(address);
            }

            if (tryParse)
            {
                return null;
            }

            throw new FormatException();
        }

        private static unsafe bool TryParseIpv4(string ipSpan, out long address)
        {
            int end = ipSpan.Length;
            long tmpAddr;

            fixed (char* ipStringPtr = &MemoryMarshal.GetReference(ipSpan.AsSpan()))
            {
                tmpAddr = IPv4AddressHelper.ParseNonCanonical(ipStringPtr, 0, ref end, notImplicitFile: true);
            }

            if (tmpAddr != IPv4AddressHelper.Invalid && end == ipSpan.Length)
            {
                address = (uint)IPAddress.HostToNetworkOrder(unchecked((int)tmpAddr));
                return true;
            }
            
            address = 0;
            return false;
        }

        private static unsafe bool TryParseIPv6(string ipSpan, Span<ushort> numbers, int numbersLength, out uint scope)
        {
            Debug.Assert(numbersLength >= IPAddressParserStatics.IPv6AddressShorts);

            int end = ipSpan.Length;
            bool isValid = false;
            fixed (char* ipStringPtr = &MemoryMarshal.GetReference(ipSpan.AsSpan()))
            {
                isValid = IPv6AddressHelper.IsValidStrict(ipStringPtr, 0, ref end);
            }

            scope = 0;
            if (isValid || (end != ipSpan.Length))
            {
                IPv6AddressHelper.Parse(ipSpan, numbers, out ReadOnlySpan<byte> scopeIdSpan);

                if (scopeIdSpan.Length > 1)
                {
                    bool parsedNumericScope = false;
                    scopeIdSpan = scopeIdSpan.Slice(1);
                    parsedNumericScope = uint.TryParse(scopeIdSpan, NumberStyles.None, CultureInfo.InvariantCulture, out scope);
                    if (parsedNumericScope)
                    {
                        return true;
                    }
                    else
                    {
                        uint interfaceIndex = InterfaceInfoPal.InterfaceNameToIndex(scopeIdSpan);
                        if (interfaceIndex > 0)
                        {
                            scope = interfaceIndex;
                            return true;
                        }
                    }
                }
                return true;
            }

            return false;
        }  
        
        private static uint ExtractIPv4Address(ushort[] address)
        {
            uint ipv4address = (uint)address[6] << 16 | address[7];
            return (uint)IPAddress.HostToNetworkOrder(unchecked((int)ipv4address));
        }
    }
}
