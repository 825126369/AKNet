/************************************Copyright*****************************************
*        ProjectName:AKNet
*        Web:https://github.com/825126369/AKNet
*        Description:C#游戏网络库
*        Author:许珂
*        StartTime:2024/11/01 00:00:00
*        ModifyTime:2025/11/30 19:43:21
*        Copyright:MIT软件许可证
************************************Copyright*****************************************/
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
