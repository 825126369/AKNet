using System;
using System.Security.Cryptography;

namespace AKNet.Udp5Quic.Common
{
    internal static class CxPlatRandom
    {
        public static void Random(Span<byte> randomBytes)
        {
            RandomNumberGenerator.Fill(randomBytes);
        }

        public static void Random(ref int randomBytes)
        {
            randomBytes = RandomNumberGenerator.GetInt32(0, int.MaxValue);
        }

        public static void Random(ref byte randomBytes)
        {
            randomBytes = (byte)RandomNumberGenerator.GetInt32(0, byte.MaxValue);
        }

        public static void Random(ref uint randomBytes)
        {
            randomBytes = (uint)RandomNumberGenerator.GetInt32(0, int.MaxValue);
        }
    }
}
