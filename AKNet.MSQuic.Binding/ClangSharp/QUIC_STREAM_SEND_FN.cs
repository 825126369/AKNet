using System.Runtime.InteropServices;

namespace AKNet.MSQuicWrapper
{
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    [return: NativeTypeName("HRESULT")]
    public unsafe delegate int QUIC_STREAM_SEND_FN([NativeTypeName("HQUIC")] QUIC_HANDLE* Stream, [NativeTypeName("const QUIC_BUFFER *const")] QUIC_BUFFER* Buffers, [NativeTypeName("uint32_t")] uint BufferCount, QUIC_SEND_FLAGS Flags, void* ClientSendContext);
}
