using System;
using System.Runtime.InteropServices;

namespace AKNet.BoringSSL
{
    //这个类 主要是 方便 C# 直接调用，屏蔽掉不安全语句
    internal static unsafe partial class BoringSSLFunc
    {
        public const ushort TLS1_VERSION = 0x0301;
        public const ushort TLS1_1_VERSION = 0x0302;
        public const ushort TLS1_2_VERSION = 0x0303;
        public const ushort TLS1_3_VERSION = 0x0304;
        public const ushort TLS_MAX_VERSION = TLS1_3_VERSION;

        public const long SSL_SESS_CACHE_OFF = 0x0000;
        public const long SSL_SESS_CACHE_CLIENT = 0x0001;
        public const long SSL_SESS_CACHE_SERVER = 0x0002;
        public const long SSL_SESS_CACHE_BOTH = (SSL_SESS_CACHE_CLIENT | SSL_SESS_CACHE_SERVER);
        public const long SSL_SESS_CACHE_NO_AUTO_CLEAR = 0x0080;
        public const long SSL_SESS_CACHE_NO_INTERNAL_LOOKUP = 0x0100;
        public const long SSL_SESS_CACHE_NO_INTERNAL_STORE = 0x0200;
        public const long SSL_SESS_CACHE_NO_INTERNAL = (SSL_SESS_CACHE_NO_INTERNAL_LOOKUP | SSL_SESS_CACHE_NO_INTERNAL_STORE);

        public const int SSL_EARLY_DATA_NOT_SENT = 0;
        public const int SSL_EARLY_DATA_REJECTED = 1;
        public const int SSL_EARLY_DATA_ACCEPTED = 2;

        public const int SSL_ERROR_SSL = 1;
        public const int SSL_ERROR_WANT_READ = 2;
        public const int SSL_ERROR_WANT_WRITE = 3;

        public static IntPtr SSL_CTX_new()
        {
            return BoringSSLNativeFunc.AKNet_SSL_CTX_new();
        }

        public static int SSL_provide_quic_data(IntPtr ssl, ssl_encryption_level_t level, byte[] data, int len)
        {
            fixed (byte* p = data)
            {
                return BoringSSLNativeFunc.AKNet_SSL_provide_quic_data(ssl, level, p, (IntPtr)len);
            }
        }

        public static int SSL_CTX_set_min_proto_version(IntPtr ctx, UInt16 version)
        {
            return BoringSSLNativeFunc.AKNet_SSL_CTX_set_min_proto_version(ctx, version);
        }

        public static int SSL_CTX_set_max_proto_version(IntPtr ctx, UInt16 version)
        {
            return BoringSSLNativeFunc.AKNet_SSL_CTX_set_max_proto_version(ctx, version);
        }

        //设置 TLS 1.3 加密套件	SSL_CTX_set_ciphersuites(ctx, "TLS_AES_256_GCM_SHA384")
        public static int SSL_CTX_set_ciphersuites(IntPtr ctx, string str)
        {
            return BoringSSLNativeFunc.AKNet_SSL_CTX_set_ciphersuites(ctx, str);
        }

        public static int SSL_CTX_set_default_verify_paths(IntPtr ctx)
        {
            return BoringSSLNativeFunc.AKNet_SSL_CTX_set_default_verify_paths(ctx);
        }

        public static int SSL_CTX_set_quic_method(IntPtr ctx, SSL_QUIC_METHOD meths)
        {
            SSL_QUIC_METHOD_Inner mStruct = meths.GetUnSafeStruct();
            int size = Marshal.SizeOf(mStruct);
            IntPtr ptr = Marshal.AllocHGlobal(size);
            Marshal.StructureToPtr(mStruct, ptr, false);
            return BoringSSLNativeFunc.AKNet_SSL_CTX_set_quic_method(ctx, ptr);
        }

        public static int SSL_set_app_data<T>(IntPtr ssl, T AppData) where T : class
        {
            GCHandle hObject = GCHandle.Alloc(AppData, GCHandleType.Normal);
            return BoringSSLNativeFunc.AKNet_SSL_set_app_data(ssl, (void*)GCHandle.ToIntPtr(hObject));
        }

        //它通常用于在 SSL/TLS 连接中绑定一些上下文信息，例如用户会话、连接状态、用户数据结构等。
        public static T SSL_get_app_data<T>(IntPtr ssl)
        {
            IntPtr data = BoringSSLNativeFunc.AKNet_SSL_get_app_data(ssl);
            GCHandle retrievedHandle = GCHandle.FromIntPtr(data);
            T retrievedObj = (T)retrievedHandle.Target;
            return retrievedObj;
        }

        public static uint SSL_CIPHER_get_id(IntPtr cipher)
        {
            return BoringSSLNativeFunc.AKNet_SSL_CIPHER_get_id(cipher);
        }

        public static IntPtr SSL_get_current_cipher(IntPtr ssl)
        {
            return BoringSSLNativeFunc.AKNet_SSL_get_current_cipher(ssl);
        }

        public static long SSL_CTX_set_session_cache_mode(IntPtr ctx, long m)
        {
            return BoringSSLNativeFunc.AKNet_SSL_CTX_set_session_cache_mode(ctx, m);
        }

        public static void SSL_CTX_sess_set_new_cb(IntPtr ctx, func_new_session_cb new_session_cb)
        {
            BoringSSLNativeFunc.AKNet_SSL_CTX_sess_set_new_cb(ctx, Marshal.GetFunctionPointerForDelegate(new_session_cb));
        }

        public static IntPtr BIO_new()
        {
            return BoringSSLNativeFunc.AKNet_BIO_new();
        }

        public static int BIO_free(IntPtr bio)
        {
            return BoringSSLNativeFunc.AKNet_BIO_free(bio);
        }

        public static long BIO_get_mem_data(IntPtr bio, out Span<byte> Data)
        {
            IntPtr DataPtr = IntPtr.Zero;
            long nLength = BoringSSLNativeFunc.AKNet_BIO_get_mem_data(bio, out DataPtr);
            Data = new Span<byte>(DataPtr.ToPointer(), (int)nLength);
            return nLength;
        }

        public static IntPtr BIO_new_mem_buf(Span<byte> buf)
        {
            fixed (byte* p = &MemoryMarshal.GetReference(buf))
            {
                return BoringSSLNativeFunc.AKNet_BIO_new_mem_buf(p, buf.Length);
            }
        }

        public static IntPtr SSL_new(IntPtr ctx)
        {
            return BoringSSLNativeFunc.AKNet_SSL_new(ctx);
        }

        public static int PEM_write_bio_SSL_SESSION(IntPtr bio, IntPtr x)
        {
            return BoringSSLNativeFunc.AKNet_PEM_write_bio_SSL_SESSION(bio, x);
        }

        public static IntPtr PEM_read_bio_SSL_SESSION(IntPtr bio, out IntPtr session, IntPtr cb, IntPtr u)
        {
            return BoringSSLNativeFunc.AKNet_PEM_read_bio_SSL_SESSION(bio, out session, cb, u);
        }

        public static int SSL_set_session(IntPtr ssl, IntPtr session)
        {
            return BoringSSLNativeFunc.AKNet_SSL_set_session(ssl, session);
        }

        public static void SSL_SESSION_free(IntPtr session)
        {
            BoringSSLNativeFunc.AKNet_SSL_SESSION_free(session);
        }

        public static void SSL_set_quic_use_legacy_codepoint(IntPtr ssl, bool use_legacy)
        {
            BoringSSLNativeFunc.AKNet_SSL_set_quic_use_legacy_codepoint(ssl, use_legacy ? 1 : 0);
        }

        public static int SSL_set_quic_transport_params(IntPtr ssl, ReadOnlySpan<byte> paramsBuffer)
        {
            fixed (byte* p = &MemoryMarshal.GetReference(paramsBuffer))
            {
                return BoringSSLNativeFunc.AKNet_SSL_set_quic_transport_params(ssl, p, paramsBuffer.Length);
            }
        }

        public static void SSL_set_accept_state(IntPtr ssl)
        {
            BoringSSLNativeFunc.AKNet_SSL_set_accept_state(ssl);
        }

        public static void SSL_set_connect_state(IntPtr ssl)
        {
            BoringSSLNativeFunc.AKNet_SSL_set_connect_state(ssl);
        }

        public static long SSL_set_tlsext_host_name(IntPtr ssl, string url)
        {
            return BoringSSLNativeFunc.AKNet_SSL_set_tlsext_host_name(ssl, url);
        }

        public static int SSL_set_alpn_protos(IntPtr ssl, ReadOnlySpan<byte> protos)
        {
            fixed (byte* p = &MemoryMarshal.GetReference(protos))
            {
                return BoringSSLNativeFunc.AKNet_SSL_set_alpn_protos(ssl, p, protos.Length);
            }
        }

        public static void SSL_set_quic_early_data_enabled(IntPtr ssl, bool enabled)
        {
            BoringSSLNativeFunc.AKNet_SSL_set_quic_early_data_enabled(ssl, enabled ? 1 : 0);
        }
        
        public static void SSL_get_peer_quic_transport_params(IntPtr ssl, out Span<byte> paramsBuffer)
        {
            byte* paramsBufferPtr = null;
            int nLength = 0;
            BoringSSLNativeFunc.AKNet_SSL_get_peer_quic_transport_params(ssl, out paramsBufferPtr, out nLength);
            paramsBuffer = new Span<byte>(paramsBufferPtr, nLength);
        }

        public static int SSL_SESSION_set1_ticket_appdata(IntPtr session, ReadOnlySpan<byte> data)
        {
            fixed (byte* p = &MemoryMarshal.GetReference(data))
            {
                return BoringSSLNativeFunc.AKNet_SSL_SESSION_set1_ticket_appdata(session, p, data.Length);
            }
        }

        public static int SSL_process_quic_post_handshake(IntPtr ssl)
        {
            return BoringSSLNativeFunc.AKNet_SSL_process_quic_post_handshake(ssl);
        }

        public static int SSL_new_session_ticket(IntPtr ssl)
        {
            return BoringSSLNativeFunc.AKNet_SSL_new_session_ticket(ssl);
        }

        public static int SSL_do_handshake(IntPtr ssl)
        {
            return BoringSSLNativeFunc.AKNet_SSL_do_handshake(ssl);
        }

        public static int SSL_session_reused(IntPtr ssl)
        {
            return BoringSSLNativeFunc.AKNet_SSL_session_reused(ssl);
        }

        public static int SSL_get_early_data_status(IntPtr ssl)
        {
            return BoringSSLNativeFunc.AKNet_SSL_get_early_data_status(ssl);
        }

        public static void SSL_get0_alpn_selected(IntPtr ssl, out Span<byte> data)
        {
            byte* paramsBufferPtr = null;
            int nLength = 0;
            BoringSSLNativeFunc.AKNet_SSL_get0_alpn_selected(ssl, out paramsBufferPtr, out nLength);
            data = new Span<byte>(paramsBufferPtr, nLength);
        }

        public static int SSL_get_error(IntPtr ssl, int ret_code)
        {
            return BoringSSLNativeFunc.AKNet_SSL_get_error(ssl, ret_code);
        }

        public static IntPtr SSL_get_session(IntPtr ssl)
        {
            return BoringSSLNativeFunc.AKNet_SSL_get_session(ssl);
        }

        public static void print_openssl_errors()
        {
            BoringSSLNativeFunc.print_openssl_errors();
        }

    }
}
