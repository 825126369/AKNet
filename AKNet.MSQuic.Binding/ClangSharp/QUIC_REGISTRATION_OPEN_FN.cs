using System.Runtime.InteropServices;

namespace AKNet.MSQuicWrapper;

[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
[return: NativeTypeName("HRESULT")]
public unsafe delegate IntPtr QUIC_REGISTRATION_OPEN_FN([NativeTypeName("const QUIC_REGISTRATION_CONFIG *")] QUIC_REGISTRATION_CONFIG* Config, [NativeTypeName("HQUIC *")] QUIC_HANDLE** Registration);
