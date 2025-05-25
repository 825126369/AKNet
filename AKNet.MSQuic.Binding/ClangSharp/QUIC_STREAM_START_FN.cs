using System.Runtime.InteropServices;

namespace AKNet.MSQuicWrapper;

[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
[return: NativeTypeName("HRESULT")]
public unsafe delegate IntPtr QUIC_STREAM_START_FN([NativeTypeName("HQUIC")] QUIC_HANDLE* Stream, QUIC_STREAM_START_FLAGS Flags);
