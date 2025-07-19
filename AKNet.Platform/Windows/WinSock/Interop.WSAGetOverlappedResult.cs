using System.Runtime.InteropServices;

namespace AKNet.Platform
{
    internal static partial class Interop
    {
        internal static partial class Winsock
        {
#if NET7_0_OR_GREATER
            [LibraryImport(Interop.Libraries.Ws2_32, SetLastError = true)]
            [return: MarshalAs(UnmanagedType.Bool)]
            internal static unsafe partial bool WSAGetOverlappedResult(
                IntPtr socketHandle,
                Overlapped* overlapped,
                out uint bytesTransferred,
                [MarshalAs(UnmanagedType.Bool)] bool wait,
                out uint socketFlags);

#else
            [DllImport(Interop.Libraries.Ws2_32, SetLastError = true)]
            [return: MarshalAs(UnmanagedType.Bool)]
            internal static unsafe extern bool WSAGetOverlappedResult(
                IntPtr socketHandle,
                Overlapped* overlapped,
                out uint bytesTransferred,
                [MarshalAs(UnmanagedType.Bool)] bool wait,
                out uint socketFlags);
#endif
        }
    }
}
