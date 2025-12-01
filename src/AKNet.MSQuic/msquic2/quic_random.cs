/************************************Copyright*****************************************
*        ProjectName:AKNet
*        Web:https://github.com/825126369/AKNet
*        Description:C#游戏网络库
*        Author:许珂
*        StartTime:2024/11/01 00:00:00
*        ModifyTime:2025/11/30 19:43:19
*        Copyright:MIT软件许可证
************************************Copyright*****************************************/
using System;
using System.Security.Cryptography;

namespace MSQuic2
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
