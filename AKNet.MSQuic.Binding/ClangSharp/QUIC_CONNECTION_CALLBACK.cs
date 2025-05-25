using System.Runtime.InteropServices;

namespace AKNet.MSQuicWrapper;

[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
[return: NativeTypeName("HRESULT")]
public unsafe delegate IntPtr QUIC_CONNECTION_CALLBACK([NativeTypeName("HQUIC")] QUIC_HANDLE* Connection, void* Context, QUIC_CONNECTION_EVENT* Event);
