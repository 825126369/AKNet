using System.Runtime.InteropServices;

namespace AKNet.MSQuicWrapper
{
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    public unsafe delegate void MsQuicCloseFn([NativeTypeName("const void *")] void* QuicApi);
}
