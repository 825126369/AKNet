using System.Runtime.InteropServices;

namespace AKNet.MSQuicWrapper
{
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    [return: NativeTypeName("HRESULT")]
    public unsafe delegate int QUIC_CONNECTION_COMP_CERT_FN([NativeTypeName("HQUIC")] QUIC_HANDLE* Connection, [NativeTypeName("BOOLEAN")] byte Result, QUIC_TLS_ALERT_CODES TlsAlert);
}
