/************************************Copyright*****************************************
*        ProjectName:AKNet
*        Web:https://github.com/825126369/AKNet
*        Description:C#游戏网络库
*        Author:许珂
*        StartTime:2024/11/01 00:00:00
*        ModifyTime:2026/2/1 20:27:05
*        Copyright:MIT软件许可证
************************************Copyright*****************************************/
using System;
using System.Runtime.InteropServices;

namespace AKNet.Platform
{
    public static unsafe partial class Interop
    {
#if NET7_0_OR_GREATER
        public static unsafe partial class Winsock
        {
            [Flags]
            internal enum NameInfoFlags
            {
                NI_NOFQDN = 0x01, /* Only return nodename portion for local hosts */
                NI_NUMERICHOST = 0x02, /* Return numeric form of the host's address */
                NI_NAMEREQD = 0x04, /* Error if the host's name not in DNS */
                NI_NUMERICSERV = 0x08, /* Return numeric form of the service (port #) */
                NI_DGRAM = 0x10, /* Service is a datagram service */
            }

            [LibraryImport(Interop.Libraries.Ws2_32, SetLastError = true, StringMarshalling = StringMarshalling.Utf16)]
            internal static unsafe partial int GetNameInfoW(
                byte* pSockaddr,
                int SockaddrLength,
                char* pNodeBuffer,
                int NodeBufferSize,
                char* pServiceBuffer,
                int ServiceBufferSize,
                int Flags);
        }
#else
        public static unsafe partial class Winsock
        {
            [Flags]
            internal enum NameInfoFlags
            {
                NI_NOFQDN = 0x01, /* Only return nodename portion for local hosts */
                NI_NUMERICHOST = 0x02, /* Return numeric form of the host's address */
                NI_NAMEREQD = 0x04, /* Error if the host's name not in DNS */
                NI_NUMERICSERV = 0x08, /* Return numeric form of the service (port #) */
                NI_DGRAM = 0x10, /* Service is a datagram service */
            }

            [DllImport(Interop.Libraries.Ws2_32, SetLastError = true, CharSet = CharSet.Unicode)]
            internal static unsafe extern int GetNameInfoW(
                byte* pSockaddr,
                int SockaddrLength,
                char* pNodeBuffer,
                int NodeBufferSize,
                char* pServiceBuffer,
                int ServiceBufferSize,
                int Flags);
        }
#endif
    }
}
