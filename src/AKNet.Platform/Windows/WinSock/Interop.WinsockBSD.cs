/************************************Copyright*****************************************
*        ProjectName:AKNet
*        Web:https://github.com/825126369/AKNet
*        Description:C#游戏网络库
*        Author:许珂
*        StartTime:2024/11/01 00:00:00
*        ModifyTime:2026/2/1 20:27:05
*        Copyright:MIT软件许可证
************************************Copyright*****************************************/
#if NET8_0_OR_GREATER
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.Marshalling;

namespace AKNet.Platform
{
    public static unsafe partial class Interop
    {
        public static unsafe partial class Winsock
        {
            internal static class IoctlSocketConstants
            {
                public const int FIONREAD = 0x4004667F;
                public const int FIONBIO = unchecked((int)0x8004667E);
                public const int FIOASYNC = unchecked((int)0x8004667D);
                public const uint SIOGETEXTENSIONFUNCTIONPOINTER = unchecked(0xC8000006);

                // Not likely to block (sync IO ok):
                //
                // FIONBIO
                // FIONREAD
                // SIOCATMARK
                // SIO_RCVALL
                // SIO_RCVALL_MCAST
                // SIO_RCVALL_IGMPMCAST
                // SIO_KEEPALIVE_VALS
                // SIO_ASSOCIATE_HANDLE (opcode setting: I, T==1)
                // SIO_ENABLE_CIRCULAR_QUEUEING (opcode setting: V, T==1)
                // SIO_GET_BROADCAST_ADDRESS (opcode setting: O, T==1)
                // SIO_GET_EXTENSION_FUNCTION_POINTER (opcode setting: O, I, T==1)
                // SIO_MULTIPOINT_LOOPBACK (opcode setting: I, T==1)
                // SIO_MULTICAST_SCOPE (opcode setting: I, T==1)
                // SIO_TRANSLATE_HANDLE (opcode setting: I, O, T==1)
                // SIO_ROUTING_INTERFACE_QUERY (opcode setting: I, O, T==1)
                //
                // Likely to block (recommended for async IO):
                //
                // SIO_FIND_ROUTE (opcode setting: O, T==1)
                // SIO_FLUSH (opcode setting: V, T==1)
                // SIO_GET_QOS (opcode setting: O, T==1)
                // SIO_GET_GROUP_QOS (opcode setting: O, I, T==1)
                // SIO_SET_QOS (opcode setting: I, T==1)
                // SIO_SET_GROUP_QOS (opcode setting: I, T==1)
                // SIO_ROUTING_INTERFACE_CHANGE (opcode setting: I, T==1)
                // SIO_ADDRESS_LIST_CHANGE (opcode setting: T==1)
            }

            [StructLayout(LayoutKind.Sequential)]
            internal struct TimeValue
            {
                public int Seconds;
                public int Microseconds;
            }

            [StructLayout(LayoutKind.Sequential)]
            internal struct Linger
            {
                internal ushort OnOff; // Option on/off.
                internal ushort Time; // Linger time in seconds.
            }
        }
    }
}
#else
using System.Diagnostics;
using System.Runtime.InteropServices;
namespace AKNet.Platform
{
    public static unsafe partial class Interop
    {
        public static unsafe partial class Winsock
        {
            internal static class IoctlSocketConstants
            {
                public const int FIONREAD = 0x4004667F;
                public const int FIONBIO = unchecked((int)0x8004667E);
                public const int FIOASYNC = unchecked((int)0x8004667D);
                public const uint SIOGETEXTENSIONFUNCTIONPOINTER = unchecked(0xC8000006);

                // Not likely to block (sync IO ok):
                //
                // FIONBIO
                // FIONREAD
                // SIOCATMARK
                // SIO_RCVALL
                // SIO_RCVALL_MCAST
                // SIO_RCVALL_IGMPMCAST
                // SIO_KEEPALIVE_VALS
                // SIO_ASSOCIATE_HANDLE (opcode setting: I, T==1)
                // SIO_ENABLE_CIRCULAR_QUEUEING (opcode setting: V, T==1)
                // SIO_GET_BROADCAST_ADDRESS (opcode setting: O, T==1)
                // SIO_GET_EXTENSION_FUNCTION_POINTER (opcode setting: O, I, T==1)
                // SIO_MULTIPOINT_LOOPBACK (opcode setting: I, T==1)
                // SIO_MULTICAST_SCOPE (opcode setting: I, T==1)
                // SIO_TRANSLATE_HANDLE (opcode setting: I, O, T==1)
                // SIO_ROUTING_INTERFACE_QUERY (opcode setting: I, O, T==1)
                //
                // Likely to block (recommended for async IO):
                //
                // SIO_FIND_ROUTE (opcode setting: O, T==1)
                // SIO_FLUSH (opcode setting: V, T==1)
                // SIO_GET_QOS (opcode setting: O, T==1)
                // SIO_GET_GROUP_QOS (opcode setting: O, I, T==1)
                // SIO_SET_QOS (opcode setting: I, T==1)
                // SIO_SET_GROUP_QOS (opcode setting: I, T==1)
                // SIO_ROUTING_INTERFACE_CHANGE (opcode setting: I, T==1)
                // SIO_ADDRESS_LIST_CHANGE (opcode setting: T==1)
            }

            [StructLayout(LayoutKind.Sequential)]
            internal struct TimeValue
            {
                public int Seconds;
                public int Microseconds;
            }

            [StructLayout(LayoutKind.Sequential)]
            internal struct Linger
            {
                internal ushort OnOff; // Option on/off.
                internal ushort Time; // Linger time in seconds.
            }
        }
    }
}
#endif