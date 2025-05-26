using System.Runtime.InteropServices;

namespace AKNet.MSQuicWrapper
{
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    [return: NativeTypeName("HRESULT")]
    public unsafe delegate int QUIC_STREAM_SHUTDOWN_FN([NativeTypeName("HQUIC")] QUIC_HANDLE* Stream, QUIC_STREAM_SHUTDOWN_FLAGS Flags, [NativeTypeName("QUIC_UINT62")] ulong ErrorCode);
}
