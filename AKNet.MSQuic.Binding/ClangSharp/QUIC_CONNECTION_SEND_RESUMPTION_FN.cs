using System.Runtime.InteropServices;

namespace AKNet.MSQuicWrapper;

[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
[return: NativeTypeName("HRESULT")]
public unsafe delegate IntPtr QUIC_CONNECTION_SEND_RESUMPTION_FN([NativeTypeName("HQUIC")] QUIC_HANDLE* Connection, QUIC_SEND_RESUMPTION_FLAGS Flags, [NativeTypeName("uint16_t")] ushort DataLength, [NativeTypeName("const uint8_t *")] byte* ResumptionData);
