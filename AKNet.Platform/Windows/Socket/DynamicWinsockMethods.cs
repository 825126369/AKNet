/************************************Copyright*****************************************
*        ProjectName:AKNet
*        Web:https://github.com/825126369/AKNet
*        Description:C#游戏网络库
*        Author:许珂
*        StartTime:2024/11/01 00:00:00
*        ModifyTime:2025/11/30 19:43:20
*        Copyright:MIT软件许可证
************************************Copyright*****************************************/
#if TARGET_WINDOWS
using System.Runtime.InteropServices;

namespace AKNet.Platform
{
    public static unsafe class DynamicWinsockMethods
    {
        private static readonly Guid WSASendMsgGuid = new Guid(0xa441e712, 0x754f, 0x43ca, 0x84, 0xa7, 0x0d, 0xee, 0x44, 0xcf, 0x60, 0x6d);
        private static readonly Guid WSARecvMsgGuid = new Guid(0xf689d7c8, 0x6f1f, 0x436b, 0x8a, 0x53, 0xe5, 0x4f, 0xe3, 0x51, 0xc3, 0x22);
        static WSARecvMsg _recvMsg;
        static WSASendMsg _sendMsg;

        private static T CreateDelegate<T>(SafeHandle socketHandle, Guid guid) where T : Delegate
        {
            IntPtr ptr = IntPtr.Zero;
            int Result = Interop.Winsock.WSAIoctl(
               socketHandle,
               OSPlatformFunc.SIO_GET_EXTENSION_FUNCTION_POINTER,
               &guid,
               sizeof(Guid),
               &ptr,
               sizeof(IntPtr),
               out _,
               null,
               null);

            if (Result != OSPlatformFunc.NO_ERROR)
            {
                int WsaError = Marshal.GetLastWin32Error();
                return null;
            }

            return Marshal.GetDelegateForFunctionPointer<T>((IntPtr)ptr);
        }

        public static unsafe WSARecvMsg GetWSARecvMsgDelegate(SafeHandle socketHandle)
        {
            if (_recvMsg == null)
            {
                _recvMsg = CreateDelegate<WSARecvMsg>(socketHandle, WSARecvMsgGuid);
            }
            return _recvMsg;
        }

        public static unsafe WSASendMsg GetWSASendMsgDelegate(SafeHandle socketHandle)
        {
            if (_sendMsg == null)
            {
                _sendMsg = CreateDelegate<WSASendMsg>(socketHandle, WSASendMsgGuid);
            }
            return _sendMsg;
        }
    }

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    public unsafe delegate int WSARecvMsg(
                SafeHandle socketHandle,
                WSAMSG* msg,
                int* bytesTransferred,
                OVERLAPPED* overlapped,
                void* completionRoutine);
    
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    public unsafe delegate int WSASendMsg(
                SafeHandle Handle,
                WSAMSG* lpMsg,
                uint dwFlags,
                int* lpNumberOfBytesSent,
                OVERLAPPED* lpOverlapped,
                void* lpCompletionRoutine);
}
#endif
