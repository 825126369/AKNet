using System;
using System.Runtime.InteropServices;

namespace AKNet.MSQuicWrapper
{
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    [return: NativeTypeName("HRESULT")]
    public unsafe delegate int QUIC_LISTENER_OPEN_FN([NativeTypeName("HQUIC")] QUIC_HANDLE* Registration, [NativeTypeName("QUIC_LISTENER_CALLBACK_HANDLER")] IntPtr Handler, void* Context, [NativeTypeName("HQUIC *")] QUIC_HANDLE** Listener);
}
