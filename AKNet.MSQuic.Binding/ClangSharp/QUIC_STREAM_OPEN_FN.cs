using System;
using System.Runtime.InteropServices;

namespace AKNet.MSQuicWrapper
{
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    [return: NativeTypeName("HRESULT")]
    public unsafe delegate int QUIC_STREAM_OPEN_FN([NativeTypeName("HQUIC")] QUIC_HANDLE* Connection, QUIC_STREAM_OPEN_FLAGS Flags, [NativeTypeName("QUIC_STREAM_CALLBACK_HANDLER")] IntPtr Handler, void* Context, [NativeTypeName("HQUIC *")] QUIC_HANDLE** Stream);
}
