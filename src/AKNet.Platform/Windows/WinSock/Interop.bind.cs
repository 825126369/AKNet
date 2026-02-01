/************************************Copyright*****************************************
*        ProjectName:AKNet
*        Web:https://github.com/825126369/AKNet
*        Description:C#游戏网络库
*        Author:许珂
*        StartTime:2024/11/01 00:00:00
*        ModifyTime:2026/2/1 20:27:04
*        Copyright:MIT软件许可证
************************************Copyright*****************************************/
using System.Runtime.InteropServices;
namespace AKNet.Platform
{
    public static unsafe partial class Interop
    {
        public static unsafe partial class Winsock
        {
#if NET7_0_OR_GREATER
            [LibraryImport(Interop.Libraries.Ws2_32, SetLastError = true)]
            public static partial int bind(SafeHandle socketHandle, byte* socketAddress, int socketAddressSize);
            [LibraryImport(Interop.Libraries.Ws2_32, SetLastError = true)]
            public static partial int connect(SafeHandle s, byte* name, int namelen);
            //[LibraryImport(Interop.Libraries.Ws2_32, SetLastError = true)]
            //public static partial int WSAGetLastError();
            [LibraryImport(Interop.Libraries.Ws2_32, SetLastError = true)]
            public static partial void FreeAddrInfoW(ADDRINFOW* pAddrInfo);
            [LibraryImport(Interop.Libraries.Ws2_32, SetLastError = true)]
            public static partial int GetAddrInfoW(
                [MarshalAs(UnmanagedType.LPWStr)] string pNodeName,
                [MarshalAs(UnmanagedType.LPWStr)] string pServiceName, 
                ADDRINFOW* pHints, 
                ADDRINFOW** ppResult);
#else
            [DllImport(Interop.Libraries.Ws2_32, SetLastError = true)]
            public static extern int bind(SafeHandle socketHandle, byte* socketAddress, int socketAddressSize);
            [DllImport(Interop.Libraries.Ws2_32, SetLastError = true)]
            public static extern int connect(SafeHandle s, byte* name, int namelen);
            //[DllImport(Interop.Libraries.Ws2_32, SetLastError = true)]
            //public static extern int WSAGetLastError(); //����� Marshal.GetLastWin32Error() ��ͻ��
            [DllImport(Interop.Libraries.Ws2_32, SetLastError = true)]
            public static extern void FreeAddrInfoW(ADDRINFOW* pAddrInfo);
            [DllImport(Interop.Libraries.Ws2_32, SetLastError = true, CharSet = CharSet.Unicode)]
            public static extern int GetAddrInfoW(string pNodeName, string pServiceName, ADDRINFOW* pHints, ADDRINFOW** ppResult);
#endif
        }
    }
}
