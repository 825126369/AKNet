using System;
using System.Runtime.InteropServices;

namespace AKNet.BoringSSL
{
    internal enum ssl_encryption_level_t : int
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

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    internal delegate int func_new_session_cb(IntPtr Ssl, IntPtr Session);
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    internal delegate int func_set_encryption_secrets(IntPtr ssl, ssl_encryption_level_t level, IntPtr write_secret, IntPtr read_secret, int secret_len);
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    internal delegate int func_add_handshake_data(IntPtr ssl, ssl_encryption_level_t level, IntPtr data, int len);
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    internal delegate int func_flush_flight(IntPtr ssl);
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    internal delegate int func_send_alert(IntPtr ssl, ssl_encryption_level_t level, byte alert);

    internal unsafe struct SSL_QUIC_METHOD_Inner
    {
        public IntPtr set_encryption_secrets;
        public IntPtr add_handshake_data;
        public IntPtr flush_flight;
        public IntPtr send_alert;

        public SSL_QUIC_METHOD_Inner(func_set_encryption_secrets func1,
            func_add_handshake_data func3, func_flush_flight func4, func_send_alert func5)
        {
            set_encryption_secrets = Marshal.GetFunctionPointerForDelegate(func1);
            add_handshake_data = Marshal.GetFunctionPointerForDelegate(func3);
            flush_flight = Marshal.GetFunctionPointerForDelegate(func4);
            send_alert = Marshal.GetFunctionPointerForDelegate(func5);
        }
    }

    internal class SSL_QUIC_METHOD
    {
        public readonly func_set_encryption_secrets set_encryption_secrets;
        public readonly func_add_handshake_data add_handshake_data;
        public readonly func_flush_flight flush_flight;
        public readonly func_send_alert send_alert;

        public SSL_QUIC_METHOD(func_set_encryption_secrets func1,
            func_add_handshake_data func3, func_flush_flight func4, func_send_alert func5)
        {
            set_encryption_secrets = func1;
            add_handshake_data = func3;
            flush_flight = func4;
            send_alert = func5;
        }

        public SSL_QUIC_METHOD_Inner GetUnSafeStruct()
        {
            return new SSL_QUIC_METHOD_Inner(set_encryption_secrets, add_handshake_data, flush_flight, send_alert);
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
        public static extern int AKNet_SSL_CTX_set_ciphersuites(IntPtr ctx, string version);
        [DllImport(DLLNAME, CallingConvention = CallingConvention.Cdecl)]
        public static extern int AKNet_SSL_CTX_set_default_verify_paths(IntPtr ctx);
        [DllImport(DLLNAME, CallingConvention = CallingConvention.Cdecl)]
        public static extern int AKNet_SSL_CTX_set_quic_method(IntPtr ctx, IntPtr meths);
        [DllImport(DLLNAME, CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr AKNet_SSL_get_app_data(IntPtr ssl);
        [DllImport(DLLNAME, CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr AKNet_SSL_get_current_cipher(IntPtr ssl);
        [DllImport(DLLNAME, CallingConvention = CallingConvention.Cdecl)]
        public static extern uint AKNet_SSL_CIPHER_get_id(IntPtr cipher);
        [DllImport(DLLNAME, CallingConvention = CallingConvention.Cdecl)]
        public static extern long AKNet_SSL_CTX_set_session_cache_mode(IntPtr ctx, long m);
        [DllImport(DLLNAME, CallingConvention = CallingConvention.Cdecl)]
        public static extern long AKNet_SSL_CTX_sess_set_new_cb(IntPtr ctx, IntPtr new_session_cb);

        [DllImport(DLLNAME, CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr AKNet_BIO_new();
        [DllImport(DLLNAME, CallingConvention = CallingConvention.Cdecl)]
        public static extern long AKNet_BIO_get_mem_data(IntPtr bio, out IntPtr Data);
        [DllImport(DLLNAME, CallingConvention = CallingConvention.Cdecl)]
        public static extern int AKNet_BIO_free(IntPtr bio);
        [DllImport(DLLNAME, CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr AKNet_SSL_new(IntPtr ctx);


        [DllImport(DLLNAME, CallingConvention = CallingConvention.Cdecl)]
        public static extern int AKNet_PEM_write_bio_SSL_SESSION(IntPtr bio, IntPtr x);
        [DllImport(DLLNAME, CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr AKNet_PEM_read_bio_SSL_SESSION(IntPtr bio, out IntPtr x, IntPtr cb, IntPtr u);
        [DllImport(DLLNAME, CallingConvention = CallingConvention.Cdecl)]
        public static extern int AKNet_SSL_set_session(IntPtr ssl, IntPtr session);
        [DllImport(DLLNAME, CallingConvention = CallingConvention.Cdecl)]
        public static extern void AKNet_SSL_SESSION_free(IntPtr session);
        [DllImport(DLLNAME, CallingConvention = CallingConvention.Cdecl)]
        public static extern void AKNet_SSL_set_quic_use_legacy_codepoint(IntPtr ssl, int use_legacy);
        [DllImport(DLLNAME, CallingConvention = CallingConvention.Cdecl)]
        public static extern int AKNet_SSL_set_quic_transport_params(IntPtr ssl, byte* paramsBuffer, int params_len);
        [DllImport(DLLNAME, CallingConvention = CallingConvention.Cdecl)]
        public static extern int AKNet_SSL_set_app_data(IntPtr ssl, void* AppData);
        [DllImport(DLLNAME, CallingConvention = CallingConvention.Cdecl)]
        public static extern void AKNet_SSL_set_accept_state(IntPtr ssl);
        [DllImport(DLLNAME, CallingConvention = CallingConvention.Cdecl)]
        public static extern void AKNet_SSL_set_connect_state(IntPtr ssl);
        [DllImport(DLLNAME, CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
        public static extern long AKNet_SSL_set_tlsext_host_name(IntPtr ssl, string url);
        [DllImport(DLLNAME, CallingConvention = CallingConvention.Cdecl)]
        public static extern int AKNet_SSL_set_alpn_protos(IntPtr ssl, byte* protos, int protos_len);
        [DllImport(DLLNAME, CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr AKNet_BIO_new_mem_buf(void* buf, int len);
        [DllImport(DLLNAME, CallingConvention = CallingConvention.Cdecl)]
        public static extern void AKNet_SSL_set_quic_early_data_enabled(IntPtr ssl, int enabled);

        
        [DllImport(DLLNAME, CallingConvention = CallingConvention.Cdecl)]
        public static extern void AKNet_SSL_get_peer_quic_transport_params(IntPtr ssl, out byte* paramsBuffer, out int params_len);
        [DllImport(DLLNAME, CallingConvention = CallingConvention.Cdecl)]
        public static extern int AKNet_SSL_SESSION_set1_ticket_appdata(IntPtr session, void* data, int nLength);
        [DllImport(DLLNAME, CallingConvention = CallingConvention.Cdecl)]
        public static extern int AKNet_SSL_process_quic_post_handshake(IntPtr ssl);
        [DllImport(DLLNAME, CallingConvention = CallingConvention.Cdecl)]
        public static extern int AKNet_SSL_new_session_ticket(IntPtr ssl);
        [DllImport(DLLNAME, CallingConvention = CallingConvention.Cdecl)]
        public static extern int AKNet_SSL_do_handshake(IntPtr ssl);
        [DllImport(DLLNAME, CallingConvention = CallingConvention.Cdecl)]
        public static extern int AKNet_SSL_session_reused(IntPtr ssl);
        [DllImport(DLLNAME, CallingConvention = CallingConvention.Cdecl)]
        public static extern int AKNet_SSL_get_early_data_status(IntPtr ssl);
        [DllImport(DLLNAME, CallingConvention = CallingConvention.Cdecl)]
        public static extern void AKNet_SSL_get0_alpn_selected(IntPtr ssl, out byte* data, out int len);
        [DllImport(DLLNAME, CallingConvention = CallingConvention.Cdecl)]
        public static extern int AKNet_SSL_get_error(IntPtr ssl, int ret_code);
        [DllImport(DLLNAME, CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr AKNet_SSL_get_session(IntPtr ssl);
        [DllImport(DLLNAME, CallingConvention = CallingConvention.Cdecl)]
        public static extern void print_openssl_errors();
    }
}
