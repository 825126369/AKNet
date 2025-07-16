// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers.Binary;
using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace AKNet.Socket
{
    internal static partial class IPv4AddressHelper
    {
        internal const long Invalid = -1;
        private const long MaxIPv4Value = uint.MaxValue; // the native parser cannot handle MaxIPv4Value, only MaxIPv4Value - 1

        private const int Octal = 8;
        private const int Decimal = 10;
        private const int Hex = 16;

        private const int NumberOfLabels = 4;

        internal static string ParseCanonicalName(string str, int start, int end, ref bool isLoopback)
        {
            unsafe
            {
                byte* numbers = stackalloc byte[NumberOfLabels];
                isLoopback = Parse(str, numbers, start, end);

                Span<char> stackSpace = stackalloc char[NumberOfLabels * 3 + 3];
                int totalChars = 0, charsWritten;
                for (int i = 0; i < 3; i++)
                {
                    numbers[i].TryFormat(stackSpace.Slice(totalChars), out charsWritten);
                    int periodPos = totalChars + charsWritten;
                    stackSpace[periodPos] = '.';
                    totalChars = periodPos + 1;
                }
                numbers[3].TryFormat(stackSpace.Slice(totalChars), out charsWritten);
                return new string(stackSpace.Slice(0, totalChars + charsWritten));
            }
        }

        private static unsafe bool Parse(string name, byte* numbers, int start, int end)
        {
            fixed (char* ipString = name)
            {
                // end includes ports, so changedEnd may be different from end
                int changedEnd = end;
                long result = IPv4AddressHelper.ParseNonCanonical(name, start, ref changedEnd, true);

                Debug.Assert(result != Invalid, $"Failed to parse after already validated: {name}");

                unchecked
                {
                    numbers[0] = (byte)(result >> 24);
                    numbers[1] = (byte)(result >> 16);
                    numbers[2] = (byte)(result >> 8);
                    numbers[3] = (byte)(result);
                }
            }

            return numbers[0] == 127;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static ushort ToUShort(char value)
        {
            return (byte)value;
        }

        // Only called from the IPv6Helper, only parse the canonical format
        internal static int ParseHostNumber(string str, int start, int end)
        {
            Span<byte> numbers = stackalloc byte[NumberOfLabels];

            for (int i = 0; i < numbers.Length; ++i)
            {
                int b = 0;
                int ch;

                for (; (start < end) && (ch = ToUShort(str[start])) != '.' && ch != ':'; ++start)
                {
                    b = (b * 10) + ch - '0';
                }

                numbers[i] = (byte)b;
                ++start;
            }

            return BinaryPrimitives.ReadInt32BigEndian(numbers);
        }
            
        internal static unsafe bool IsValid(string name, int start, ref int end, bool allowIPv6, bool notImplicitFile, bool unknownScheme)
        {
            // IPv6 can only have canonical IPv4 embedded. Unknown schemes will not attempt parsing of non-canonical IPv4 addresses.
            if (allowIPv6 || unknownScheme)
            {
                return IsValidCanonical(name, start, ref end, allowIPv6, notImplicitFile);
            }
            else
            {
                return ParseNonCanonical(name, start, ref end, notImplicitFile) != Invalid;
            }
        }
        
        internal static unsafe bool IsValidCanonical(string name, int start, ref int end, bool allowIPv6, bool notImplicitFile)
        {
            int dots = 0;
            long number = 0;
            bool haveNumber = false;
            bool firstCharIsZero = false;

            while (start < end)
            {
                int ch = ToUShort(name[start]);

                if (allowIPv6)
                {
                    if (ch == ']' || ch == '/' || ch == '%')
                    {
                        break;
                    }
                }
                else if (ch == '/' || ch == '\\' || (notImplicitFile && (ch == ':' || ch == '?' || ch == '#')))
                {
                    break;
                }

                uint parsedCharacter = (uint)(ch - '0');

                if (parsedCharacter < IPv4AddressHelper.Decimal)
                {
                    if (!haveNumber && parsedCharacter == 0)
                    {
                        if ((start + 1 < end) && name[start + 1] == '0')
                        {
                            return false;
                        }

                        firstCharIsZero = true;
                    }

                    haveNumber = true;
                    number = number * IPv4AddressHelper.Decimal + parsedCharacter;
                    if (number > byte.MaxValue)
                    {
                        return false;
                    }
                }
                else if (ch == '.')
                {
                    // If the current character is not an integer, it may be the IPv4 component separator ('.')

                    if (!haveNumber || (number > 0 && firstCharIsZero))
                    {
                        // 0 is not allowed to prefix a number.
                        return false;
                    }
                    ++dots;
                    haveNumber = false;
                    number = 0;
                    firstCharIsZero = false;
                }
                else
                {
                    return false;
                }
                ++start;
            }
            bool res = (dots == 3) && haveNumber;
            if (res)
            {
                end = start;
            }
            return res;
        }

        //支持非标准格式（如大小写不敏感、冗余字符）
        internal static unsafe long ParseNonCanonical(string name, int start, ref int end, bool notImplicitFile)
        {
            int numberBase = IPv4AddressHelper.Decimal;
            int ch = 0;
            long* parts = stackalloc long[3];
            long currentValue = 0;
            bool atLeastOneChar = false;
            int dotCount = 0;
            int current = start;

            for (; current < end; current++)
            {
                ch = ToUShort(name[current]);
                currentValue = 0;
                numberBase = IPv4AddressHelper.Decimal;

                if (ch == '0')
                {
                    current++;
                    atLeastOneChar = true;
                    if (current < end)
                    {
                        ch = ToUShort(name[current]);

                        if (ch == 'x' || ch == 'X')
                        {
                            numberBase = IPv4AddressHelper.Hex;

                            current++;
                            atLeastOneChar = false;
                        }
                        else
                        {
                            numberBase = IPv4AddressHelper.Octal;
                        }
                    }
                }
                
                for (; current < end; current++)
                {
                    ch = ToUShort(name[current]);
                    int digitValue = HexConverter.FromChar(ch);

                    if (digitValue >= numberBase)
                    {
                        break;
                    }

                    currentValue = (currentValue * numberBase) + digitValue;
                    if (currentValue > MaxIPv4Value)
                    {
                        return Invalid;
                    }

                    atLeastOneChar = true;
                }

                if (current < end && ch == '.')
                {
                    if (dotCount >= 3 || !atLeastOneChar || currentValue > 0xFF)
                    {
                        return Invalid;
                    }

                    parts[dotCount] = currentValue;
                    dotCount++;
                    atLeastOneChar = false;
                    continue;
                }
                break;
            }
            
            if (!atLeastOneChar)
            {
                return Invalid;
            }
            else if (current >= end)
            {
                
            }
            else if (ch == '/' || ch == '\\' || (notImplicitFile && (ch == ':' || ch == '?' || ch == '#')))
            {
                end = current;
            }
            else
            {
                return Invalid;
            }
            
            switch (dotCount)
            {
                case 0: // 0xFFFFFFFF
                    return currentValue;
                case 1: // 0xFF.0xFFFFFF
                    Debug.Assert(parts[0] <= 0xFF);
                    if (currentValue > 0xffffff)
                    {
                        return Invalid;
                    }
                    return (parts[0] << 24) | currentValue;
                case 2: // 0xFF.0xFF.0xFFFF
                    Debug.Assert(parts[0] <= 0xFF);
                    Debug.Assert(parts[1] <= 0xFF);
                    if (currentValue > 0xffff)
                    {
                        return Invalid;
                    }
                    return (parts[0] << 24) | (parts[1] << 16) | currentValue;
                case 3: // 0xFF.0xFF.0xFF.0xFF
                    Debug.Assert(parts[0] <= 0xFF);
                    Debug.Assert(parts[1] <= 0xFF);
                    Debug.Assert(parts[2] <= 0xFF);
                    if (currentValue > 0xff)
                    {
                        return Invalid;
                    }
                    return (parts[0] << 24) | (parts[1] << 16) | (parts[2] << 8) | currentValue;
                default:
                    return Invalid;
            }
        }
    }
}
