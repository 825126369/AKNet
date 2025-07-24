using System;
using System.Security.Cryptography;

namespace AKNet.Udp1MSQuic.Common
{
    internal static class CxPlatRandom
    {
        public static void Random(QUIC_SSBuffer randomBytes)
        {
            RandomNumberGenerator.Fill(randomBytes.GetSpan());
        }

        public static void Random(Span<byte> randomBytes)
        {
            RandomNumberGenerator.Fill(randomBytes);
        }

        public static void Random(byte[] randomBytes)
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

        public static byte RandomByte()
        {
            return (byte)RandomNumberGenerator.GetInt32(0, byte.MaxValue);
        }

        public static int RandomInt32()
        {
            return (Int32)RandomNumberGenerator.GetInt32(0, int.MaxValue);
        }
    }
}
