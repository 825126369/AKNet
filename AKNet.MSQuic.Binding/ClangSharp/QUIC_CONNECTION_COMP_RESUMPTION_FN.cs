using System.Runtime.InteropServices;

namespace AKNet.MSQuicWrapper;

[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
[return: NativeTypeName("HRESULT")]
public unsafe delegate IntPtr QUIC_CONNECTION_COMP_RESUMPTION_FN([NativeTypeName("HQUIC")] QUIC_HANDLE* Connection, [NativeTypeName("BOOLEAN")] byte Result);
