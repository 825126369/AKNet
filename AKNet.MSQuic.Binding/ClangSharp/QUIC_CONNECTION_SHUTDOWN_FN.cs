using System.Runtime.InteropServices;

namespace AKNet.MSQuicWrapper
{
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    public unsafe delegate void QUIC_CONNECTION_SHUTDOWN_FN([NativeTypeName("HQUIC")] QUIC_HANDLE* Connection, QUIC_CONNECTION_SHUTDOWN_FLAGS Flags, [NativeTypeName("QUIC_UINT62")] ulong ErrorCode);
}
