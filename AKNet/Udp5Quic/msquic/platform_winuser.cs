using System.Diagnostics;
using System.Security.Cryptography;

namespace AKNet.Udp5Quic.Common
{
    internal static partial class MSQuicFunc
    {
        static Stopwatch mStopwatch;
        static QUIC_TRACE_RUNDOWN_CALLBACK QuicTraceRundownCallback;

        static long CxPlatRandom(int BufferLen, byte[] randomBytes)
        {
            using (RandomNumberGenerator rng = RandomNumberGenerator.Create())
            {
                rng.GetBytes(randomBytes); // 填充随机数据
            }
            return 0;
        }

        static void CxPlatSystemLoad()
        {
            
        }

    }
}
