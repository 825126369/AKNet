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
            RandomNumberGenerator.GetInt32(0, int.MaxValue);
        }

        public static void Random(ref byte randomBytes)
        {
            RandomNumberGenerator.GetInt32(0, byte.MaxValue);
        }
    }
}
