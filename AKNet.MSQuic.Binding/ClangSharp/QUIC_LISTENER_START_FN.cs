using System.Runtime.InteropServices;

namespace AKNet.MSQuicWrapper;

[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
[return: NativeTypeName("HRESULT")]
public unsafe delegate IntPtr QUIC_LISTENER_START_FN([NativeTypeName("HQUIC")] QUIC_HANDLE* Listener, [NativeTypeName("const QUIC_BUFFER *const")] QUIC_BUFFER* AlpnBuffers, [NativeTypeName("uint32_t")] uint AlpnBufferCount, [NativeTypeName("const QUIC_ADDR *")] _SOCKADDR_INET* LocalAddress);
