using System.Runtime.InteropServices;
namespace AKNet.Platform.Socket
{
    internal static partial class Interop
    {
        internal static partial class Winsock
        {
#if NET7_0_OR_GREATER
            [LibraryImport(Interop.Libraries.Ws2_32, SetLastError = true)]
            internal static unsafe partial int select(
                int ignoredParameter,
                IntPtr* readfds,
                IntPtr* writefds,
                IntPtr* exceptfds,
                ref TimeValue timeout);

            [LibraryImport(Interop.Libraries.Ws2_32, SetLastError = true)]
            internal static unsafe partial int select(
                int ignoredParameter,
                IntPtr* readfds,
                IntPtr* writefds,
                IntPtr* exceptfds,
                IntPtr nullTimeout);
#else
            [DllImport(Interop.Libraries.Ws2_32, SetLastError = true)]
            internal static unsafe extern int select(int ignoredParameter, IntPtr* readfds, IntPtr* writefds, IntPtr* exceptfds, ref TimeValue timeout);

            [DllImport(Interop.Libraries.Ws2_32, SetLastError = true)]
            internal static unsafe extern int select(int ignoredParameter, IntPtr* readfds, IntPtr* writefds, IntPtr* exceptfds, IntPtr nullTimeout);
#endif
        }
    }
}
