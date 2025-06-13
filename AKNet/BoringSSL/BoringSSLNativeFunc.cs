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

    internal unsafe class SSL_QUIC_METHOD
    {
        internal delegate int func_set_read_secret(IntPtr ssl, ssl_encryption_level_t level, IntPtr cipher, byte* secret, int secret_len);
        internal delegate int func_set_write_secret(IntPtr ssl, ssl_encryption_level_t level, IntPtr cipher, byte* secret, int secret_len);
        internal delegate int func_add_handshake_data(IntPtr ssl, ssl_encryption_level_t level, byte* data, int len);
        internal delegate int func_flush_flight(IntPtr ssl);
        internal delegate int func_send_alert(IntPtr ssl, ssl_encryption_level_t level, byte alert);

        public IntPtr set_read_secret;
        public IntPtr set_write_secret;
        public IntPtr add_handshake_data;
        public IntPtr flush_flight;
        public IntPtr send_alert;

        public SSL_QUIC_METHOD(func_set_read_secret func1, func_set_write_secret func2,
            func_add_handshake_data func3, func_flush_flight func4, func_send_alert func5)
        {
            SetFunc(func1);
            SetFunc(func2);
            SetFunc(func3);
            SetFunc(func4);
            SetFunc(func5);
        }

        public void SetFunc(func_set_read_secret set_read_secret)
        {
            this.set_read_secret = Marshal.GetFunctionPointerForDelegate(set_read_secret);
        }

        public void SetFunc(func_set_write_secret set_read_secret)
        {
            this.set_write_secret = Marshal.GetFunctionPointerForDelegate(set_read_secret);
        }

        public void SetFunc(func_add_handshake_data add_handshake_data)
        {
            this.add_handshake_data = Marshal.GetFunctionPointerForDelegate(add_handshake_data);
        }

        public void SetFunc(func_flush_flight flush_flight)
        {
            this.flush_flight = Marshal.GetFunctionPointerForDelegate(flush_flight);
        }

        public void SetFunc(func_send_alert send_alert)
        {
            this.send_alert = Marshal.GetFunctionPointerForDelegate(send_alert);
        }
    }

    internal static unsafe partial class BoringSSLNativeFunc
    {
        public const string DLLNAME = "QuicTlsCC.dll";


        [DllImport(DLLNAME, CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr AKNet_SSL_CTX_new();
        [DllImport(DLLNAME, CallingConvention = CallingConvention.Cdecl)]
        public static extern int AKNet_SSL_provide_quic_data(IntPtr ssl, ssl_encryption_level_t level, byte* data, IntPtr len);
        [DllImport(DLLNAME, CallingConvention = CallingConvention.Cdecl)]
        public static extern int AKNet_SSL_CTX_set_min_proto_version(IntPtr ctx, UInt16 version);
        [DllImport(DLLNAME, CallingConvention = CallingConvention.Cdecl)]
        public static extern int AKNet_SSL_CTX_set_max_proto_version(IntPtr ctx, UInt16 version);
        [DllImport(DLLNAME, CallingConvention = CallingConvention.Cdecl)]
        public static extern int AKNet_SSL_CTX_set_ciphersuites(IntPtr ctx, char* version);
        [DllImport(DLLNAME, CallingConvention = CallingConvention.Cdecl)]
        public static extern int AKNet_SSL_CTX_set_default_verify_paths(IntPtr ctx);
        [DllImport(DLLNAME, CallingConvention = CallingConvention.Cdecl)]
        public static extern int AKNet_SSL_CTX_set_quic_method(IntPtr ctx, SSL_QUIC_METHOD* meths);
        [DllImport(DLLNAME, CallingConvention = CallingConvention.Cdecl)]
        public static extern void* AKNet_SSL_get_app_data(IntPtr ssl);
        [DllImport(DLLNAME, CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr AKNet_SSL_get_current_cipher(IntPtr ssl);
        [DllImport(DLLNAME, CallingConvention = CallingConvention.Cdecl)]
        public static extern uint AKNet_SSL_CIPHER_get_id(IntPtr cipher);
        
    }
}
