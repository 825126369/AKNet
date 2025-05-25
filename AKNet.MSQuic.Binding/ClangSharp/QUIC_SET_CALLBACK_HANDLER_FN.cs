using System.Runtime.InteropServices;

namespace AKNet.MSQuicWrapper;

[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
public unsafe delegate void QUIC_SET_CALLBACK_HANDLER_FN([NativeTypeName("HQUIC")] QUIC_HANDLE* Handle, void* Handler, void* Context);
