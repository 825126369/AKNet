using System.Runtime.InteropServices;

namespace AKNet.MSQuicWrapper;

[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
[return: NativeTypeName("HRESULT")]
public unsafe delegate IntPtr MsQuicOpenVersionFn([NativeTypeName("uint32_t")] uint Version, [NativeTypeName("const void **")] void** QuicApi);
