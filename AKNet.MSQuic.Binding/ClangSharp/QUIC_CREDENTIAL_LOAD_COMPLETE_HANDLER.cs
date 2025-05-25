using System.Runtime.InteropServices;

namespace AKNet.MSQuicWrapper;

[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
public unsafe delegate void QUIC_CREDENTIAL_LOAD_COMPLETE_HANDLER();
