using System.Runtime.InteropServices;

namespace AKNet.MSQuicWrapper;

[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
[return: NativeTypeName("HRESULT")]
public unsafe delegate IntPtr QUIC_STREAM_RECEIVE_SET_ENABLED_FN([NativeTypeName("HQUIC")] QUIC_HANDLE* Stream, [NativeTypeName("BOOLEAN")] byte IsEnabled);
