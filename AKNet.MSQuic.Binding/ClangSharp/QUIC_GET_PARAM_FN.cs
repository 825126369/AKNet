using System.Runtime.InteropServices;

namespace AKNet.MSQuicWrapper
{
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    [return: NativeTypeName("HRESULT")]
    public unsafe delegate int QUIC_GET_PARAM_FN([NativeTypeName("HQUIC")] QUIC_HANDLE* Handle, [NativeTypeName("uint32_t")] uint Param, [NativeTypeName("uint32_t *")] uint* BufferLength, void* Buffer);
}
