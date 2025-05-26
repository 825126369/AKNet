using System.Runtime.InteropServices;

namespace AKNet.MSQuicWrapper
{
    public static unsafe partial class MSQuicWrapperFunc
    {
        [DllImport(@"C:\Users\14261\.nuget\packages\microsoft.native.quic.msquic.openssl\2.4.10\build\native\lib\x64", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("HRESULT")]
        public static extern int MsQuicOpenVersion([NativeTypeName("uint32_t")] uint Version, [NativeTypeName("const void **")] void** QuicApi);

        [DllImport(@"C:\Users\14261\.nuget\packages\microsoft.native.quic.msquic.openssl\2.4.10\build\native\lib\x64", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern void MsQuicClose([NativeTypeName("const void *")] void* QuicApi);

        [return: NativeTypeName("HRESULT")]
        public static int MsQuicOpen2([NativeTypeName("const QUIC_API_TABLE **")] QUIC_API_TABLE** QuicApi)
        {
            return MsQuicOpenVersion(2, unchecked((void**)(QuicApi)));
        }
    }
}
