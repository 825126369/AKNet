using System.Runtime.InteropServices;

namespace AKNet.MSQuicWrapper
{
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    [return: NativeTypeName("HRESULT")]
    public unsafe delegate int QUIC_LISTENER_CALLBACK([NativeTypeName("HQUIC")] QUIC_HANDLE* Listener, void* Context, QUIC_LISTENER_EVENT* Event);
}
