using System.Runtime.InteropServices;
using static AKNet.Platform.Interop.Kernel32;

namespace AKNet.Platform.Socket
{
    internal static unsafe class DynamicWinsockMethods
    {
        private static readonly ulong[] WSAID_WSASENDMSG = { 0xa441e712, 0x754f, 0x43ca, 0x84, 0xa7, 0x0d, 0xee, 0x44, 0xcf, 0x60, 0x6d };
        private static readonly ulong[] WSAID_WSARECVMSG = { 0xf689d7c8, 0x6f1f, 0x436b, 0x8a, 0x53, 0xe5, 0x4f, 0xe3, 0x51, 0xc3, 0x22 };

        static GUID GetGUID(ulong[] guid)
        {
            GUID mGuid = new GUID();
            mGuid.Data1 = guid[0];
            mGuid.Data2 = (ushort)guid[1];
            mGuid.Data3 = (ushort)guid[2];
            byte* pDest = mGuid.Data4;
            for (int i = 0; i < 8; i++)
            {
                pDest[i] = (byte)guid[3 + i];
            }
            return mGuid;
        }
        
        static readonly GUID WSARecvMsgGuid = GetGUID(WSAID_WSARECVMSG);
        static WSARecvMsgDelegate _recvMsg;

        private static T CreateDelegate<T>(SafeHandle socketHandle, GUID guid) where T : Delegate
        {
            IntPtr ptr;
            int errorCode;
            unsafe
            {
                errorCode = Interop.Winsock.WSAIoctl(
                   socketHandle,
                   Interop.Winsock.IoctlSocketConstants.SIOGETEXTENSIONFUNCTIONPOINTER,
                   &guid,
                   sizeof(GUID),
                   out ptr,
                   sizeof(IntPtr),
                   out _,
                   IntPtr.Zero,
                   IntPtr.Zero);
            }

            if (errorCode != 0)
            {
                throw new SocketException();
            }

            return Marshal.GetDelegateForFunctionPointer<T>(ptr);
        }

        internal static unsafe WSARecvMsgDelegate GetWSARecvMsgDelegate(SafeHandle socketHandle)
        {
            if (_recvMsg == null)
            {
                _recvMsg = CreateDelegate<WSARecvMsgDelegate>(socketHandle, WSARecvMsgGuid);
            }
            return _recvMsg;
        }
    }

    internal unsafe delegate SocketError WSARecvMsgDelegate(
                SafeHandle socketHandle,
                IntPtr msg,
                out int bytesTransferred,
                OVERLAPPED* overlapped,
                IntPtr completionRoutine);
}
