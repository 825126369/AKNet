using System.Runtime.InteropServices;

namespace AKNet.MSQuicWrapper;

[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
[return: NativeTypeName("HRESULT")]
public unsafe delegate IntPtr QUIC_STREAM_CALLBACK([NativeTypeName("HQUIC")] QUIC_HANDLE* Stream, void* Context, QUIC_STREAM_EVENT* Event);
