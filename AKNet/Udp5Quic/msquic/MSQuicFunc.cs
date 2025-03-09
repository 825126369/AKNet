using System;
using System.Collections.Generic;

namespace AKNet.Udp5Quic.Common
{
    internal static partial class MSQuicFunc
    {
        public void MsQuicLibraryLoad()
        {
            if (InterlockedIncrement16(&MsQuicLib.LoadRefCount) == 1)
            {
                CxPlatSystemLoad();
                CxPlatLockInitialize(&MsQuicLib.Lock);
                CxPlatDispatchLockInitialize(&MsQuicLib.DatapathLock);
                CxPlatDispatchLockInitialize(&MsQuicLib.StatelessRetryKeysLock);
                CxPlatListInitializeHead(&MsQuicLib.Registrations);
                CxPlatListInitializeHead(&MsQuicLib.Bindings);
                QuicTraceRundownCallback = QuicTraceRundown;
                MsQuicLib.Loaded = TRUE;
                MsQuicLib.Version[0] = VER_MAJOR;
                MsQuicLib.Version[1] = VER_MINOR;
                MsQuicLib.Version[2] = VER_PATCH;
                MsQuicLib.Version[3] = VER_BUILD_ID;
                MsQuicLib.GitHash = VER_GIT_HASH_STR;
            }
        }
    }
}
