using System.Runtime.InteropServices;

namespace AKNet.MSQuicWrapper;

[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
[return: NativeTypeName("HRESULT")]
public unsafe delegate IntPtr QUIC_DATAGRAM_SEND_FN([NativeTypeName("HQUIC")] QUIC_HANDLE* Connection, [NativeTypeName("const QUIC_BUFFER *const")] QUIC_BUFFER* Buffers, [NativeTypeName("uint32_t")] uint BufferCount, QUIC_SEND_FLAGS Flags, void* ClientSendContext);
