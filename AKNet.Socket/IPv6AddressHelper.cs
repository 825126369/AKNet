using System.Buffers.Binary;
using System.Diagnostics;
using System.Numerics;
using System.Runtime.InteropServices;

namespace AKNet.Socket
{
    internal static partial class IPv6AddressHelper
    {
        private const int Hex = 16;
        private const int NumberOfLabels = 8;


        internal static (int longestSequenceStart, int longestSequenceLength) FindCompressionRange(ReadOnlySpan<ushort> numbers)
        {
            int longestSequenceLength = 0, longestSequenceStart = -1, currentSequenceLength = 0;

            for (int i = 0; i < numbers.Length; i++)
            {
                if (numbers[i] == 0)
                {
                    currentSequenceLength++;
                    if (currentSequenceLength > longestSequenceLength)
                    {
                        longestSequenceLength = currentSequenceLength;
                        longestSequenceStart = i - currentSequenceLength + 1;
                    }
                }
                else
                {
                    currentSequenceLength = 0;
                }
            }

            return longestSequenceLength > 1 ?
                (longestSequenceStart, longestSequenceStart + longestSequenceLength) :
                (-1, 0);
        }

        // Returns true if the IPv6 address should be formatted with an embedded IPv4 address:
        // ::192.168.1.1
        internal static bool ShouldHaveIpv4Embedded(ReadOnlySpan<ushort> numbers)
        {
            // 0:0 : 0:0 : x:x : x.x.x.x
            if (numbers[0] == 0 && numbers[1] == 0 && numbers[2] == 0 && numbers[3] == 0 && numbers[6] != 0)
            {
                // RFC 5952 Section 5 - 0:0 : 0:0 : 0:[0 | FFFF] : x.x.x.x
                if (numbers[4] == 0 && (numbers[5] == 0 || numbers[5] == 0xFFFF))
                {
                    return true;
                }
                // SIIT - 0:0 : 0:0 : FFFF:0 : x.x.x.x
                else if (numbers[4] == 0xFFFF && numbers[5] == 0)
                {
                    return true;
                }
            }

            // ISATAP
            return numbers[4] == 0 && numbers[5] == 0x5EFE;
        }

        internal static unsafe bool IsValidStrict(string name, int start, ref int end)
        {
            int sequenceCount = 0;
            int sequenceLength = 0;
            bool haveCompressor = false;
            bool haveIPv4Address = false;
            bool expectingNumber = true;
            int lastSequence = 1;

            bool needsClosingBracket = false;
            if (start < end && name[start] == ('['))
            {
                start++;
                needsClosingBracket = true;
                Debug.Assert(start < end);
            }

            if (name[start] == ':' && (start + 1 >= end || name[start + 1] != ':'))
            {
                return false;
            }

            int i;
            for (i = start; i < end; ++i)
            {
                int currentCh = IPv4AddressHelper.ToUShort(name[i]);
                if (HexConverter.IsHexChar(currentCh))
                {
                    ++sequenceLength;
                    expectingNumber = false;
                }
                else
                {
                    if (sequenceLength > 4)
                    {
                        return false;
                    }
                    if (sequenceLength != 0)
                    {
                        ++sequenceCount;
                        lastSequence = i - sequenceLength;
                        sequenceLength = 0;
                    }

                    switch (currentCh)
                    {
                        case '%':
                            while (i + 1 < end)
                            {
                                i++;
                                if (name[i] == ']')
                                {
                                    goto case ']';
                                }
                                else if (name[i] == '/')
                                {
                                    goto case '/';
                                }
                            }
                            break;

                        case ']':
                            if (!needsClosingBracket)
                            {
                                return false;
                            }
                            needsClosingBracket = false;
                            if (i + 1 < end && name[i + 1] != ':')
                            {
                                return false;
                            }

                            if (i + 3 < end && name[i + 2] == '0' && name[i + 3] == 'x')
                            {
                                i += 4;
                                for (; i < end; i++)
                                {
                                    int ch = IPv4AddressHelper.ToUShort(name[i]);

                                    if (!HexConverter.IsHexChar(ch))
                                    {
                                        return false;
                                    }
                                }
                            }
                            else
                            {
                                i += 2;
                                for (; i < end; i++)
                                {
                                    if (!IsAsciiDigit((char)IPv4AddressHelper.ToUShort(name[i])))
                                    {
                                        return false;
                                    }
                                }
                            }
                            continue;

                        case ':':
                            if ((i > 0) && (name[i - 1] == ':'))
                            {
                                if (haveCompressor)
                                {
                                    // can only have one per IPv6 address
                                    return false;
                                }
                                haveCompressor = true;
                                expectingNumber = false;
                            }
                            else
                            {
                                expectingNumber = true;
                            }
                            break;

                        case '/':
                            // A prefix in an IPv6 address is invalid.
                            return false;

                        case '.':
                            if (haveIPv4Address)
                            {
                                return false;
                            }

                            i = end;
                            if (!IPv4AddressHelper.IsValid(name, lastSequence, ref i, true, false, false))
                            {
                                return false;
                            }
                            // An IPv4 address takes 2 slots in an IPv6 address. One was just counted meeting the '.'
                            ++sequenceCount;
                            lastSequence = i - sequenceLength;
                            sequenceLength = 0;
                            haveIPv4Address = true;
                            --i;            // it will be incremented back on the next loop
                            break;

                        default:
                            return false;
                    }
                    sequenceLength = 0;
                }
            }

            if (sequenceLength != 0)
            {
                if (sequenceLength > 4)
                {
                    return false;
                }

                ++sequenceCount;
            }

            // These sequence counts are -1 because it is implied in end-of-sequence.

            const int ExpectedSequenceCount = 8;
            return
                !expectingNumber &&
                (haveCompressor ? (sequenceCount < ExpectedSequenceCount) : (sequenceCount == ExpectedSequenceCount)) &&
                !needsClosingBracket;
        }

        internal static void Parse(string address, scoped Span<ushort> numbers, out uint scopeId)
        {
            int number = 0;
            int index = 0;
            int compressorIndex = -1;
            bool numberIsValid = true;

            scopeId = 0;
            for (int i = (address[0] == '[') ? 1 : 0; i < address.Length && address[i] != ']';)
            {
                char currentCh = address[i];
                switch (currentCh)
                {
                    case '%':
                        if (numberIsValid)
                        {
                            numbers[index++] = (ushort)number;
                            numberIsValid = false;
                        }

                        int scopeStart = i;
                        for (++i; i < address.Length && address[i] != ']' && address[i] != '/'; ++i)
                        {

                        }

                        scopeId = uint.Parse(address.AsSpan().Slice(scopeStart, i - scopeStart));
                        for (; i < address.Length && address[i] != ']'; ++i)
                        {

                        }
                        break;

                    case ':':
                        numbers[index++] = (ushort)number;
                        number = 0;
                        ++i;
                        if (address[i] == ':')
                        {
                            compressorIndex = index;
                            ++i;
                        }
                        else if ((compressorIndex < 0) && (index < 6))
                        {
                            break;
                        }

                        for (int j = i; (j < address.Length &&
                                        address[j] != ']' &&
                                        address[j] != ':' &&
                                        address[j] != '%' &&
                                        address[j] != '/' &&
                                        j < i + 4); ++j)
                        {

                            if (address[j] == '.')
                            {
                                while (j < address.Length && (address[j] != ']' && (address[j] != '/' && address[j] != '%')))
                                {
                                    ++j;
                                }
                                int ipv4Address = IPv4AddressHelper.ParseHostNumber(address, i, j);

                                numbers[index++] = (ushort)(ipv4Address >> 16);
                                numbers[index++] = (ushort)(ipv4Address & 0xFFFF);
                                i = j;
                                number = 0;
                                numberIsValid = false;
                                break;
                            }
                        }
                        break;

                    case '/':
                        if (numberIsValid)
                        {
                            numbers[index++] = (ushort)number;
                            numberIsValid = false;
                        }

                        for (++i; i < address.Length && address[i] != ']'; i++)
                        {
                        }

                        break;

                    default:
                        int characterValue = HexConverter.FromChar(currentCh);
                        number = number * IPv6AddressHelper.Hex + characterValue;
                        i++;
                        break;
                }
            }

            // Add number to the array if it's not the prefix length or part of
            // an IPv4 address that's already been handled
            if (numberIsValid)
            {
                numbers[index++] = (ushort)number;
            }

            // If we had a compressor sequence ("::") then we need to expand the
            // numbers array.
            if (compressorIndex > 0)
            {
                int toIndex = NumberOfLabels - 1;
                int fromIndex = index - 1;

                // If fromIndex and toIndex are the same, it means that "zero bits" are already in the correct place.
                // This happens for leading and trailing compression.
                if (fromIndex != toIndex)
                {
                    for (int i = index - compressorIndex; i > 0; --i)
                    {
                        numbers[toIndex--] = numbers[fromIndex];
                        numbers[fromIndex--] = 0;
                    }
                }
            }
        }

        private static unsafe bool IsLoopback(ReadOnlySpan<ushort> numbers)
        {
            //
            // is the address loopback? Loopback is defined as one of:
            //
            //  0:0:0:0:0:0:0:1
            //  0:0:0:0:0:0:127.0.0.1       == 0:0:0:0:0:0:7F00:0001
            //  0:0:0:0:0:FFFF:127.0.0.1    == 0:0:0:0:0:FFFF:7F00:0001
            //

            return ((numbers[0] == 0)
                            && (numbers[1] == 0)
                            && (numbers[2] == 0)
                            && (numbers[3] == 0)
                            && (numbers[4] == 0))
                           && (((numbers[5] == 0)
                                && (numbers[6] == 0)
                                && (numbers[7] == 1))
                               || (((numbers[6] == 0x7F00)
                                    && (numbers[7] == 0x0001))
                                   && ((numbers[5] == 0)
                                       || (numbers[5] == 0xFFFF))));
        }
        
        private static unsafe bool InternalIsValid(char* name, int start, ref int end, bool validateStrictAddress)
        {
            int sequenceCount = 0;
            int sequenceLength = 0;
            bool haveCompressor = false;
            bool haveIPv4Address = false;
            bool havePrefix = false;
            bool expectingNumber = true;
            int lastSequence = 1;

            // Starting with a colon character is only valid if another colon follows.
            if (name[start] == ':' && (start + 1 >= end || name[start + 1] != ':'))
            {
                return false;
            }

            int i;
            for (i = start; i < end; ++i)
            {
                if (havePrefix ? IsAsciiDigit(name[i]) : HexConverter.IsHexChar(name[i]))
                {
                    ++sequenceLength;
                    expectingNumber = false;
                }
                else
                {
                    if (sequenceLength > 4)
                    {
                        return false;
                    }
                    if (sequenceLength != 0)
                    {
                        ++sequenceCount;
                        lastSequence = i - sequenceLength;
                    }
                    switch (name[i])
                    {
                        case '%':
                            while (true)
                            {
                                if (++i == end)
                                {
                                    return false;
                                }
                                if (name[i] == ']')
                                {
                                    goto case ']';
                                }
                                else if (name[i] == '/')
                                {
                                    goto case '/';
                                }
                            }
                        case ']':
                            start = i;
                            i = end;
                            continue;
                        case ':':
                            if ((i > 0) && (name[i - 1] == ':'))
                            {
                                if (haveCompressor)
                                {
                                    //
                                    // can only have one per IPv6 address
                                    //

                                    return false;
                                }
                                haveCompressor = true;
                                expectingNumber = false;
                            }
                            else
                            {
                                expectingNumber = true;
                            }
                            break;

                        case '/':
                            if (validateStrictAddress)
                            {
                                return false;
                            }
                            if ((sequenceCount == 0) || havePrefix)
                            {
                                return false;
                            }
                            havePrefix = true;
                            expectingNumber = true;
                            break;

                        case '.':
                            if (haveIPv4Address)
                            {
                                return false;
                            }

                            i = end;
                            if (!IPv4AddressHelper.IsValid(name, lastSequence, ref i, true, false, false))
                            {
                                return false;
                            }
                            // ipv4 address takes 2 slots in ipv6 address, one was just counted meeting the '.'
                            ++sequenceCount;
                            haveIPv4Address = true;
                            --i;            // it will be incremented back on the next loop
                            break;

                        default:
                            return false;
                    }
                    sequenceLength = 0;
                }
            }

            if (havePrefix && ((sequenceLength < 1) || (sequenceLength > 2)))
            {
                return false;
            }

            int expectedSequenceCount = 8 + (havePrefix ? 1 : 0);

            if (!expectingNumber && (sequenceLength <= 4) && (haveCompressor ? (sequenceCount < expectedSequenceCount) : (sequenceCount == expectedSequenceCount)))
            {
                if (i == end + 1)
                {
                    // ']' was found
                    end = start + 1;
                    return true;
                }
                return false;
            }
            return false;
        }

        internal static unsafe bool IsValid(char* name, int start, ref int end)
        {
            return InternalIsValid(name, start, ref end, false);
        }

        static bool IsAsciiDigit(char s)
        {
            return s >= '0' && s <= '9';
        }
    }
}
