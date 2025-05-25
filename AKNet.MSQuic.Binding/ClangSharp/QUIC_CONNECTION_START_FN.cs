using System.Runtime.InteropServices;

namespace AKNet.MSQuicWrapper;

[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
[return: NativeTypeName("HRESULT")]
public unsafe delegate IntPtr QUIC_CONNECTION_START_FN([NativeTypeName("HQUIC")] QUIC_HANDLE* Connection, [NativeTypeName("HQUIC")] QUIC_HANDLE* Configuration, [NativeTypeName("QUIC_ADDRESS_FAMILY")] ushort Family, [NativeTypeName("const char *")] sbyte* ServerName, [NativeTypeName("uint16_t")] ushort ServerPort);
