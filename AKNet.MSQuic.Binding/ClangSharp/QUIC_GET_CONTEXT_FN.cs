using System.Runtime.InteropServices;

namespace AKNet.MSQuicWrapper;

[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
public unsafe delegate void* QUIC_GET_CONTEXT_FN([NativeTypeName("HQUIC")] QUIC_HANDLE* Handle);
