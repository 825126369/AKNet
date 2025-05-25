using System.Runtime.InteropServices;

namespace AKNet.MSQuicWrapper;

[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
[return: NativeTypeName("HRESULT")]
public unsafe delegate IntPtr QUIC_CONFIGURATION_LOAD_CREDENTIAL_FN([NativeTypeName("HQUIC")] QUIC_HANDLE* Configuration, [NativeTypeName("const QUIC_CREDENTIAL_CONFIG *")] QUIC_CREDENTIAL_CONFIG* CredConfig);
