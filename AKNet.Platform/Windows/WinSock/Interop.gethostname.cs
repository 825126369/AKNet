using System.Runtime.InteropServices;

namespace AKNet.Platform.Socket
{
    internal static partial class Interop
    {
#if NET7_0_OR_GREATER
        internal static partial class Winsock
        {
            [LibraryImport(Interop.Libraries.Ws2_32, SetLastError = true)]
            internal static unsafe partial SocketError gethostname(byte* name, int namelen);
        }
#else
        internal static partial class Winsock
        {
            [DllImport(Interop.Libraries.Ws2_32, SetLastError = true)]
            internal static unsafe extern SocketError gethostname(byte* name, int namelen);
        }
#endif
    }
}
