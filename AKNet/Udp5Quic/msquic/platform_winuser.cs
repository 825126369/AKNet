using System.Diagnostics;
using System.Security.Cryptography;
using System.Threading;

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
                rng.GetBytes(randomBytes);
            }
            return 0;
        }

        static ushort CxPlatRandom()
        {
            using (RandomNumberGenerator rng = RandomNumberGenerator.Create())
            {
                rng.GetBytes(randomBytes);
            }
            return 0;
        }

        static void CxPlatSystemLoad()
        {

        }

        static long CxPlatThreadCreate(CXPLAT_THREAD_CONFIG Config, Thread mThread)
        {
            mThread = new Thread(()=>
            {
                Thread.GetCurrentProcessorId();
            });
            
            mThread.Name = Config.Name;
            if (BoolOk(Config.Flags & (int)CXPLAT_THREAD_FLAGS.CXPLAT_THREAD_FLAG_HIGH_PRIORITY))
            {
                mThread.Priority = ThreadPriority.Highest;
            }

            return QUIC_STATUS_SUCCESS;
        }
    }
}
