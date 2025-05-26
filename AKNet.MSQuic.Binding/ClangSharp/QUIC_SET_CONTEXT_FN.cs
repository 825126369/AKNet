using System.Runtime.InteropServices;

namespace AKNet.MSQuicWrapper
{
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    public unsafe delegate void QUIC_SET_CONTEXT_FN([NativeTypeName("HQUIC")] QUIC_HANDLE* Handle, void* Context);
}
