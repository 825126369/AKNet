using System;
using System.Net.Sockets;
using System.Runtime.InteropServices;

namespace AKNet.Platform.Socket
{
    internal static partial class Interop
    {
        internal static partial class Winsock
        {
#if NET7_0_OR_GREATER
        [LibraryImport(Interop.Libraries.Ws2_32, SetLastError = true, StringMarshalling = StringMarshalling.Utf16)]
        internal static partial IntPtr WSASocketW(
            AddressFamily addressFamily,
            int socketType,
            int protocolType,
            IntPtr protocolInfo,
            int group,
            int flags);

#else
            [DllImport(Interop.Libraries.Ws2_32, SetLastError = true, CharSet = CharSet.Unicode)]
            internal static extern IntPtr WSASocketW(
                AddressFamily addressFamily,
                int socketType,
                int protocolType,
                IntPtr protocolInfo,
                int group,
                int flags);
#endif
        }
    }
}
