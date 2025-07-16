// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Globalization;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace AKNet.Socket
{
    internal static class IPAddressParser
    {
        internal const int MaxIPv4StringLength = 15; // 4 numbers separated by 3 periods, with up to 3 digits per number
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

            throw new FormatException(SR.dns_bad_ip_address, new SocketException(SocketError.InvalidArgument));
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
            Debug.Assert(typeof(TChar) == typeof(char) || typeof(TChar) == typeof(byte));
            Debug.Assert(numbersLength >= IPAddressParserStatics.IPv6AddressShorts);

            int end = ipSpan.Length;
            bool isValid = false;
            fixed (TChar* ipStringPtr = &MemoryMarshal.GetReference(ipSpan))
            {
                isValid = IPv6AddressHelper.IsValidStrict(ipStringPtr, 0, ref end);
            }

            scope = 0;
            if (isValid || (end != ipSpan.Length))
            {
                IPv6AddressHelper.Parse(ipSpan, numbers, out ReadOnlySpan<TChar> scopeIdSpan);

                if (scopeIdSpan.Length > 1)
                {
                    bool parsedNumericScope = false;
                    scopeIdSpan = scopeIdSpan.Slice(1);

                    // scopeId is a numeric value
                    if (typeof(TChar) == typeof(byte))
                    {
                        ReadOnlySpan<byte> castScopeIdSpan = MemoryMarshal.Cast<TChar, byte>(scopeIdSpan);

                        parsedNumericScope = uint.TryParse(castScopeIdSpan, NumberStyles.None, CultureInfo.InvariantCulture, out scope);
                    }
                    else if (typeof(TChar) == typeof(char))
                    {
                        ReadOnlySpan<char> castScopeIdSpan = MemoryMarshal.Cast<TChar, char>(scopeIdSpan);

                        parsedNumericScope = uint.TryParse(castScopeIdSpan, NumberStyles.None, CultureInfo.InvariantCulture, out scope);
                    }

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
                            return true; // scopeId is a known interface name
                        }
                    }

                    // scopeId is an unknown interface name
                }

                // scopeId is not presented
                return true;
            }

            return false;
        }

        internal static int FormatIPv4Address<TChar>(uint address, Span<TChar> addressString)
            where TChar : unmanaged, IBinaryInteger<TChar>
        {
            address = (uint)IPAddress.NetworkToHostOrder(unchecked((int)address));

            int pos = FormatByte(address >> 24, addressString);
            addressString[pos++] = TChar.CreateTruncating('.');
            pos += FormatByte(address >> 16, addressString.Slice(pos));
            addressString[pos++] = TChar.CreateTruncating('.');
            pos += FormatByte(address >> 8, addressString.Slice(pos));
            addressString[pos++] = TChar.CreateTruncating('.');
            pos += FormatByte(address, addressString.Slice(pos));

            return pos;

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            static int FormatByte(uint number, Span<TChar> addressString)
            {
                number &= 0xFF;

                if (number >= 10)
                {
                    uint hundreds, tens;
                    if (number >= 100)
                    {
                        (uint hundredsAndTens, number) = Math.DivRem(number, 10);
                        (hundreds, tens) = Math.DivRem(hundredsAndTens, 10);

                        addressString[2] = TChar.CreateTruncating('0' + number);
                        addressString[1] = TChar.CreateTruncating('0' + tens);
                        addressString[0] = TChar.CreateTruncating('0' + hundreds);
                        return 3;
                    }

                    (tens, number) = Math.DivRem(number, 10);
                    addressString[1] = TChar.CreateTruncating('0' + number);
                    addressString[0] = TChar.CreateTruncating('0' + tens);
                    return 2;
                }

                addressString[0] = TChar.CreateTruncating('0' + number);
                return 1;
            }
        }

        internal static int FormatIPv6Address(ushort[] address, uint scopeId, Span<char> destination)
        {
            int pos = 0;

            if (IPv6AddressHelper.ShouldHaveIpv4Embedded(address))
            {
                AppendSections(address.AsSpan(0, 6), destination, ref pos);
                if (destination[pos - 1] != TChar.CreateTruncating(':'))
                {
                    destination[pos++] = TChar.CreateTruncating(':');
                }

                pos += FormatIPv4Address(ExtractIPv4Address(address), destination.Slice(pos));
            }
            else
            {
                AppendSections(address.AsSpan(0, 8), destination, ref pos);
            }
            
            if (scopeId != 0)
            {
                destination[pos++] = TChar.CreateTruncating('%');
                Span<TChar> chars = stackalloc TChar[10];
                int bytesPos = 10;
                do
                {
                    (scopeId, uint digit) = Math.DivRem(scopeId, 10);
                    chars[--bytesPos] = TChar.CreateTruncating('0' + digit);
                }
                while (scopeId != 0);
                Span<TChar> used = chars.Slice(bytesPos);
                used.CopyTo(destination.Slice(pos));
                pos += used.Length;
            }

            return pos;
            
            static void AppendSections(ReadOnlySpan<ushort> address, Span<TChar> destination, ref int offset)
            {
                // Find the longest sequence of zeros to be combined into a "::"
                (int zeroStart, int zeroEnd) = IPv6AddressHelper.FindCompressionRange(address);
                bool needsColon = false;

                // Handle a zero sequence if there is one
                if (zeroStart >= 0)
                {
                    // Output all of the numbers before the zero sequence
                    for (int i = 0; i < zeroStart; i++)
                    {
                        if (needsColon)
                        {
                            destination[offset++] = TChar.CreateTruncating(':');
                        }
                        needsColon = true;
                        AppendHex(address[i], destination, ref offset);
                    }

                    // Output the zero sequence if there is one
                    destination[offset++] = TChar.CreateTruncating(':');
                    destination[offset++] = TChar.CreateTruncating(':');
                    needsColon = false;
                }

                // Output everything after the zero sequence
                for (int i = zeroEnd; i < address.Length; i++)
                {
                    if (needsColon)
                    {
                        destination[offset++] = TChar.CreateTruncating(':');
                    }
                    needsColon = true;
                    AppendHex(address[i], destination, ref offset);
                }
            }

            // Appends a number as hexadecimal (without the leading "0x")
            static void AppendHex(ushort value, Span<TChar> destination, ref int offset)
            {
                if ((value & 0xFFF0) != 0)
                {
                    if ((value & 0xFF00) != 0)
                    {
                        if ((value & 0xF000) != 0)
                        {
                            destination[offset++] = TChar.CreateTruncating(HexConverter.ToCharLower(value >> 12));
                        }

                        destination[offset++] = TChar.CreateTruncating(HexConverter.ToCharLower(value >> 8));
                    }

                    destination[offset++] = TChar.CreateTruncating(HexConverter.ToCharLower(value >> 4));
                }

                destination[offset++] = TChar.CreateTruncating(HexConverter.ToCharLower(value));
            }
        }
        
        private static uint ExtractIPv4Address(ushort[] address)
        {
            uint ipv4address = (uint)address[6] << 16 | address[7];
            return (uint)IPAddress.HostToNetworkOrder(unchecked((int)ipv4address));
        }
    }
}
