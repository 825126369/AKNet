#if TARGET_WINDOWS
using System.Runtime.InteropServices;
namespace AKNet.Platform.Socket
{
    internal unsafe struct GUID
    {
        public ulong Data1;
        public ushort Data2;
        public ushort Data3;
        public fixed byte Data4[8];
    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct ControlData
    {
        internal UIntPtr length;
        internal uint level;
        internal uint type;
        internal uint address;
        internal uint index;
    }

    [StructLayout(LayoutKind.Sequential)]
    internal unsafe struct ControlDataIPv6
    {
        internal const int IPv6AddressLength = 16;
        internal UIntPtr length;
        internal uint level;
        internal uint type;
        internal fixed byte address[IPv6AddressLength];
        internal uint index;
    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct WSAMsg
    {
        internal IntPtr socketAddress;
        internal uint addressLength;
        internal IntPtr buffers;
        internal uint count;
        internal WSABUF controlBuffer;
        internal SocketFlags flags;
    }

    [StructLayout(LayoutKind.Sequential, Size = 408)]
    internal struct WSAData
    {
        // WSADATA is defined as follows:
        //
        //     typedef struct WSAData {
        //             WORD                    wVersion;
        //             WORD                    wHighVersion;
        //     #ifdef _WIN64
        //             unsigned short          iMaxSockets;
        //             unsigned short          iMaxUdpDg;
        //             char FAR *              lpVendorInfo;
        //             char                    szDescription[WSADESCRIPTION_LEN+1];
        //             char                    szSystemStatus[WSASYS_STATUS_LEN+1];
        //     #else
        //             char                    szDescription[WSADESCRIPTION_LEN+1];
        //             char                    szSystemStatus[WSASYS_STATUS_LEN+1];
        //             unsigned short          iMaxSockets;
        //             unsigned short          iMaxUdpDg;
        //             char FAR *              lpVendorInfo;
        //     #endif
        //     } WSADATA, FAR * LPWSADATA;
        //
        // Important to notice is that its layout / order of fields differs between
        // 32-bit and 64-bit systems.  However, we don't actually need any of the
        // data it contains; it suffices to ensure that this struct is large enough
        // to hold either layout, which is 400 bytes on 32-bit and 408 bytes on 64-bit.
        // Thus, we don't declare any fields here, and simply make the size 408 bytes.
    }
}
#endif

