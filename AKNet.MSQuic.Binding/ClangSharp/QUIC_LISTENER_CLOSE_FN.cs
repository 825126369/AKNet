using System.Runtime.InteropServices;

namespace AKNet.MSQuicWrapper
{
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    public unsafe delegate void QUIC_LISTENER_CLOSE_FN([NativeTypeName("HQUIC")] QUIC_HANDLE* Listener);
}
