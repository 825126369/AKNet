using System.Runtime.InteropServices;

namespace AKNet.MSQuicWrapper;

[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
public unsafe delegate void QUIC_STREAM_RECEIVE_COMPLETE_FN([NativeTypeName("HQUIC")] QUIC_HANDLE* Stream, [NativeTypeName("uint64_t")] ulong BufferLength);
