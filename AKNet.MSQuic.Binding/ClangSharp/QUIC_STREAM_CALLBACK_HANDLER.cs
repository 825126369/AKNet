using System.Runtime.InteropServices;

namespace AKNet.MSQuicWrapper;

[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
[return: NativeTypeName("HRESULT")]
public unsafe delegate IntPtr QUIC_STREAM_CALLBACK_HANDLER();
