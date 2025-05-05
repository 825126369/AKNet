using System;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace AKNet.Udp5Quic.Common
{
    internal static partial class MSQuicFunc
    {
        static QUIC_TRACE_RUNDOWN_CALLBACK QuicTraceRundownCallback;

        static void CxPlatSystemLoad()
        {

        }

        static ulong CxPlatInitialize()
        {
            CxPlatCryptInitialize();
           // CxPlatWorkersInit();
            return 0;
        }
    }
}
