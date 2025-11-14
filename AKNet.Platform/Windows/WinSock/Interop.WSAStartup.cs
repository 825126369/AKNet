using System.Diagnostics;
using System.Runtime.InteropServices;

namespace AKNet.Platform
{
    public static unsafe partial class Interop
    {
#if NET7_0_OR_GREATER
        public static unsafe partial class Winsock
        {
            [LibraryImport(Libraries.Ws2_32)]
            private static unsafe partial int WSAStartup(ushort wVersionRequested, WSAData* lpWSAData);
            [LibraryImport(Libraries.Ws2_32)]
            private static partial int WSACleanup();
        }
#else
        public static unsafe partial class Winsock
        {
            [DllImport(Interop.Libraries.Ws2_32)]
            private static unsafe extern int WSAStartup(ushort wVersionRequested, WSAData* lpWSAData);

            [DllImport(Libraries.Ws2_32)]
            private static extern int WSACleanup();
        }
#endif

        public static unsafe partial class Winsock
        {
            private static int s_initialized;

            [StructLayout(LayoutKind.Sequential, Size = 408)]
            private struct WSAData
            {

            }

            public static void EnsureInitialized()
            {
                if (s_initialized == 0)
                {
                    Initialize();
                }

                static unsafe void Initialize()
                {
                    WSAData d;
                    int errorCode = WSAStartup(0x0202, &d);
                    if (errorCode != 0)
                    {
                        throw new Exception("WSAStartup Error: " + errorCode);
                    }

                    if (Interlocked.CompareExchange(ref s_initialized, 1, 0) != 0)
                    {
                        errorCode = WSACleanup();
                        Debug.Assert(errorCode == 0);
                    }
                }
            }
        }
    }
}
