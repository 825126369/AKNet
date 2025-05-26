using System.Runtime.InteropServices;

namespace AKNet.MSQuicWrapper
{
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    [return: NativeTypeName("HRESULT")]
    public unsafe delegate int QUIC_LISTENER_CALLBACK_HANDLER();
}
