using System.Security.Cryptography;

namespace AKNet.Udp5Quic.Common
{
    internal static partial class MSQuicFunc
    {
        public static long CxPlatRandom(int BufferLen, byte[] randomBytes)
        {
            using (RandomNumberGenerator rng = RandomNumberGenerator.Create())
            {
                rng.GetBytes(randomBytes); // 填充随机数据
            }
            return 0;
        }
    }
}
