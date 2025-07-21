using System.Runtime.InteropServices;

namespace AKNet.Platform
{
    internal static partial class Interop
    {
        internal static partial class Winsock
        {
#if NET7_0_OR_GREATER
        [LibraryImport(Interop.Libraries.Ws2_32, SetLastError = true, StringMarshalling = StringMarshalling.Utf16)]
        internal static partial SafeHandle WSASocketW(
            int addressFamily,
            int socketType,
            int protocolType,
            IntPtr protocolInfo,
            int group,
            uint flags);
#else
            [DllImport(Interop.Libraries.Ws2_32, SetLastError = true, CharSet = CharSet.Unicode)]
            internal static extern SafeHandle WSASocketW(
                int addressFamily,
                int socketType,
                int protocolType,
                IntPtr protocolInfo,
                int group,
                uint flags);
#endif
        }
    }
}
