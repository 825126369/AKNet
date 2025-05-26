using System.Runtime.InteropServices;

namespace AKNet.MSQuicWrapper
{
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    public unsafe delegate void QUIC_CREDENTIAL_LOAD_COMPLETE([NativeTypeName("HQUIC")] QUIC_HANDLE* Configuration, void* Context, [NativeTypeName("HRESULT")] int Status);
}
