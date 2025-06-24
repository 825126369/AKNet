using System;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace AKNet.Udp5MSQuic.Common
{
    internal static partial class MSQuicFunc
    {
        static QUIC_TRACE_RUNDOWN_CALLBACK QuicTraceRundownCallback;

        static void CxPlatSystemLoad()
        {

        }

        static int CxPlatInitialize()
        {
            CxPlatCryptInitialize();
           // CxPlatWorkersInit();
            return 0;
        }
    }
}
