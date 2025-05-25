using System.Runtime.InteropServices;

namespace AKNet.MSQuicWrapper;

[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
public unsafe delegate void QUIC_REGISTRATION_SHUTDOWN_FN([NativeTypeName("HQUIC")] QUIC_HANDLE* Registration, QUIC_CONNECTION_SHUTDOWN_FLAGS Flags, [NativeTypeName("QUIC_UINT62")] ulong ErrorCode);
