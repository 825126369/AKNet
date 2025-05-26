using System.Runtime.InteropServices;

namespace AKNet.MSQuicWrapper
{
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    public unsafe delegate void QUIC_CONNECTION_CLOSE_FN([NativeTypeName("HQUIC")] QUIC_HANDLE* Connection);
}
