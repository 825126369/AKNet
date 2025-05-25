using System.Runtime.InteropServices;

namespace AKNet.MSQuicWrapper;

[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
[return: NativeTypeName("HRESULT")]
public unsafe delegate IntPtr QUIC_CONNECTION_SET_CONFIGURATION_FN([NativeTypeName("HQUIC")] QUIC_HANDLE* Connection, [NativeTypeName("HQUIC")] QUIC_HANDLE* Configuration);
