using System;
using System.Runtime.InteropServices;

namespace AKNet.BoringSSL
{
    internal enum ssl_encryption_level_t :int 
    {
          ssl_encryption_initial = 0,
          ssl_encryption_early_data = 1,
          ssl_encryption_handshake = 2,
          ssl_encryption_application = 3,
    };

    internal unsafe partial struct SSL_BUFFER
    {
        public uint Length;
        public byte* Buffer;
    }

    internal static unsafe partial class BoringSSLNativeFunc
    {
        const string DLLNAME = "QuicTlsCC.dll";

        [DllImport(DLLNAME, CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr AKNet_SSL_CTX_new();

        [DllImport(DLLNAME, CallingConvention = CallingConvention.Cdecl)]
        public static extern int AKNet_SSL_provide_quic_data(IntPtr ssl, ssl_encryption_level_t level,byte* data, IntPtr len);


    }
}
