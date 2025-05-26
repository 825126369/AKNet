using System.Runtime.InteropServices;

namespace AKNet.MSQuicWrapper
{
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    public unsafe delegate void QUIC_CONFIGURATION_CLOSE_FN([NativeTypeName("HQUIC")] QUIC_HANDLE* Configuration);
}
