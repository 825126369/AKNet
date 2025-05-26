using System.Runtime.InteropServices;

namespace AKNet.MSQuicWrapper
{
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    [return: NativeTypeName("HRESULT")]
    public unsafe delegate int QUIC_CONFIGURATION_OPEN_FN([NativeTypeName("HQUIC")] QUIC_HANDLE* Registration, [NativeTypeName("const QUIC_BUFFER *const")] QUIC_BUFFER* AlpnBuffers, [NativeTypeName("uint32_t")] uint AlpnBufferCount, [NativeTypeName("const QUIC_SETTINGS *")] QUIC_SETTINGS* Settings, [NativeTypeName("uint32_t")] uint SettingsSize, void* Context, [NativeTypeName("HQUIC *")] QUIC_HANDLE** Configuration);
}
