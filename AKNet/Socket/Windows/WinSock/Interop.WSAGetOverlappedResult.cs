using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Threading;

namespace AKNet.Socket
{
    internal static partial class Interop
    {
        internal static partial class Winsock
        {
#if NET7_0_OR_GREATER
            [LibraryImport(Interop.Libraries.Ws2_32, SetLastError = true)]
            [return: MarshalAs(UnmanagedType.Bool)]
            internal static unsafe partial bool WSAGetOverlappedResult(
                SafeSocketHandle socketHandle,
                NativeOverlapped* overlapped,
                out uint bytesTransferred,
                [MarshalAs(UnmanagedType.Bool)] bool wait,
                out SocketFlags socketFlags);

#else
            [DllImport(Interop.Libraries.Ws2_32, SetLastError = true)]
            [return: MarshalAs(UnmanagedType.Bool)]
            internal static unsafe extern bool WSAGetOverlappedResult(
                SafeSocketHandle socketHandle,
                NativeOverlapped* overlapped,
                out uint bytesTransferred,
                [MarshalAs(UnmanagedType.Bool)] bool wait,
                out SocketFlags socketFlags);
#endif
        }
    }
}
