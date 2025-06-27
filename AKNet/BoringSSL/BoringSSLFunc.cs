using System;
using System.Runtime.InteropServices;

namespace AKNet.BoringSSL
{
    //这个类 主要是 方便 C# 直接调用
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

        public const int SSL_TICKET_FATAL_ERR_MALLOC = 0;
        public const int SSL_TICKET_FATAL_ERR_OTHER = 1;
        public const int SSL_TICKET_NONE = 2;
        public const int SSL_TICKET_EMPTY = 3;
        public const int SSL_TICKET_NO_DECRYPT = 4;
        public const int SSL_TICKET_SUCCESS = 5;
        public const int SSL_TICKET_SUCCESS_RENEW = 6;

        public const int SSL_TICKET_RETURN_ABORT = 0;
        public const int SSL_TICKET_RETURN_IGNORE = 1;
        public const int SSL_TICKET_RETURN_IGNORE_RENEW = 2;
        public const int SSL_TICKET_RETURN_USE = 3;
        public const int SSL_TICKET_RETURN_USE_RENEW = 4;

        public const ulong SSL_OP_ALL = 0;
        public const ulong SSL_OP_ALLOW_UNSAFE_LEGACY_RENEGOTIATION = 0;
        public const ulong SSL_OP_DONT_INSERT_EMPTY_FRAGMENTS = 0;
        public const ulong SSL_OP_EPHEMERAL_RSA = 0;
        public const ulong SSL_OP_LEGACY_SERVER_CONNECT = 0;
        public const ulong SSL_OP_MICROSOFT_BIG_SSLV3_BUFFER = 0;
        public const ulong SSL_OP_MICROSOFT_SESS_ID_BUG = 0;
        public const ulong SSL_OP_MSIE_SSLV2_RSA_PADDING = 0;
        public const ulong SSL_OP_NETSCAPE_CA_DN_BUG = 0;
        public const ulong SSL_OP_NETSCAPE_CHALLENGE_BUG = 0;
        public const ulong SSL_OP_NETSCAPE_DEMO_CIPHER_CHANGE_BUG = 0;
        public const ulong SSL_OP_NETSCAPE_REUSE_CIPHER_CHANGE_BUG = 0;
        public const ulong SSL_OP_NO_COMPRESSION = 0;
        public const ulong SSL_OP_NO_SESSION_RESUMPTION_ON_RENEGOTIATION = 0;
        public const ulong SSL_OP_NO_SSLv2 = 0;
        public const ulong SSL_OP_PKCS1_CHECK_1 = 0;
        public const ulong SSL_OP_PKCS1_CHECK_2 = 0;
        public const ulong SSL_OP_SINGLE_DH_USE = 0;
        public const ulong SSL_OP_SINGLE_ECDH_USE = 0;
        public const ulong SSL_OP_SSLEAY_080_CLIENT_DH_BUG = 0;
        public const ulong SSL_OP_SSLREF2_REUSE_CERT_TYPE_BUG = 0;
        public const ulong SSL_OP_TLS_BLOCK_PADDING_BUG = 0;
        public const ulong SSL_OP_TLS_D5_BUG = 0;
        public const ulong SSL_OP_TLS_ROLLBACK_BUG = 0;

        public static readonly ulong SSL_OP_ENABLE_MIDDLEBOX_COMPAT = SSL_OP_BIT(20);
        public static readonly ulong SSL_OP_CIPHER_SERVER_PREFERENCE = SSL_OP_BIT(22);
        public static readonly ulong SSL_OP_NO_ANTI_REPLAY = SSL_OP_BIT(24);

        public static readonly uint SSL_MODE_RELEASE_BUFFERS = 0x00000010U;

        public static readonly int  TLS1_AD_INTERNAL_ERROR  = 80;
        public static readonly int SSL_AD_INTERNAL_ERROR = TLS1_AD_INTERNAL_ERROR;

        public static readonly int SSL_CLIENT_HELLO_SUCCESS = 1;
        public static readonly int SSL_CLIENT_HELLO_ERROR = 0;

        public static readonly int SSL_TLSEXT_ERR_OK = 0;
        public static readonly int SSL_TLSEXT_ERR_ALERT_WARNING = 1;

        public static readonly int SSL_TLSEXT_ERR_ALERT_FATAL = 2;
        public static readonly int SSL_TLSEXT_ERR_NOACK = 3;

        public static readonly int SSL_VERIFY_PEER = 0x01;
        public static readonly int SSL_VERIFY_FAIL_IF_NO_PEER_CERT = 0x02;

        public const int X509_V_OK = 0;
        public const int X509_V_ERR_CERT_HAS_EXPIRED = 10;
        public const int X509_V_ERR_OUT_OF_MEM = 17;
        public const int X509_V_ERR_DEPTH_ZERO_SELF_SIGNED_CERT = 18;
        public const int X509_V_ERR_CERT_REVOKED = 23;
        public const int X509_V_ERR_CERT_UNTRUSTED = 27;
        public const int X509_V_ERR_CERT_REJECTED = 28;

        public const int NID_pkcs7_data = 21;
        public const int NID_pkcs7_signed = 22;

        public const int X509_FILETYPE_PEM = 1;
        public const int SSL_FILETYPE_PEM = X509_FILETYPE_PEM;

        public static ulong SSL_OP_BIT(int n)
        {
            return 1UL << n;
        }


        public static IntPtr SSL_CTX_new()
        {
            return BoringSSLNativeFunc.AKNet_SSL_CTX_new();
        }

        public static int SSL_provide_quic_data(IntPtr ssl, ssl_encryption_level_t level, ReadOnlySpan<byte> data)
        {
            fixed (byte* p = data)
            {
                return BoringSSLNativeFunc.AKNet_SSL_provide_quic_data(ssl, level, p, data.Length);
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
            fixed (byte* p = buf)
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
            fixed (byte* p = paramsBuffer)
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
            fixed (byte* p = protos)
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
            fixed (byte* p = data)
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
        
        public static int SSL_CTX_set_max_early_data(IntPtr ctx, uint max_early_data)
        {
            return BoringSSLNativeFunc.AKNet_SSL_CTX_set_max_early_data(ctx, max_early_data);
        }
        public static int SSL_CTX_set_session_ticket_cb(IntPtr ctx, SSL_CTX_generate_session_ticket_fn gen_cb, SSL_CTX_decrypt_session_ticket_fn dec_cb)
        {
            return BoringSSLNativeFunc.AKNet_SSL_CTX_set_session_ticket_cb(ctx, gen_cb, dec_cb, null);
        }

        public static int SSL_CTX_set_num_tickets(IntPtr ctx, int num_tickets)
        {
            return BoringSSLNativeFunc.AKNet_SSL_CTX_set_num_tickets(ctx, num_tickets);
        }

        public static void SSL_CTX_set_default_passwd_cb_userdata(IntPtr ctx, object u)
        {
            IntPtr uPtr = IntPtr.Zero;
            BoringSSLNativeFunc.AKNet_SSL_CTX_set_default_passwd_cb_userdata(ctx, uPtr);
        }

        public static int SSL_CTX_use_PrivateKey_file(IntPtr ctx, string file, int type)
        {
            return BoringSSLNativeFunc.AKNet_SSL_CTX_use_PrivateKey_file(ctx, file, type);
        }

        public static int SSL_CTX_use_certificate_chain_file(IntPtr ctx, string file)
        {
            return BoringSSLNativeFunc.AKNet_SSL_CTX_use_certificate_chain_file(ctx, file);
        }

        public static int SSL_CTX_use_PrivateKey(IntPtr ctx, IntPtr pkey)
        {
            return BoringSSLNativeFunc.AKNet_SSL_CTX_use_PrivateKey(ctx, pkey);
        }

        public static int SSL_CTX_use_certificate(IntPtr ctx, IntPtr x)
        {
            return BoringSSLNativeFunc.AKNet_SSL_CTX_use_certificate(ctx, x);
        }

        //用于设置当从一个 内存 BIO（BIO_TYPE_MEM） 读取数据时，在没有更多数据可读的情况下（即达到 EOF），返回的默认值。
        public static long BIO_set_mem_eof_return(IntPtr bp, long larg)
        {
            return BoringSSLNativeFunc.AKNet_BIO_set_mem_eof_return(bp, larg);
        }

        public static int BIO_write(IntPtr b, Span<byte> data)
        {
            fixed (byte* ptr = data)
            {
                return BoringSSLNativeFunc.AKNet_BIO_write(b, ptr, data.Length);
            }
        }

        public static IntPtr d2i_PKCS12_bio(IntPtr bp)
        {
            IntPtr p = IntPtr.Zero;
            return BoringSSLNativeFunc.AKNet_d2i_PKCS12_bio(bp, out p);
        }

        public static int PKCS12_parse(IntPtr p12, string pass, ref IntPtr pkey, ref IntPtr cert, ref IntPtr ca)
        {
            return BoringSSLNativeFunc.AKNet_PKCS12_parse(p12, pass, ref pkey, ref cert, ref ca);
        }

        public static void PKCS12_free(IntPtr p12)
        {
            BoringSSLNativeFunc.AKNet_PKCS12_free(p12);
        }

        public static long SSL_CTX_add_extra_chain_cert(IntPtr ctx, object parg)
        {
            IntPtr uPtr = IntPtr.Zero;
            return BoringSSLNativeFunc.AKNet_SSL_CTX_add_extra_chain_cert(ctx, uPtr);
        }

        public static void sk_X509_free(IntPtr sk)
        {
            BoringSSLNativeFunc.AKNet_sk_X509_free(sk);
        }

        public static IntPtr sk_X509_pop(IntPtr st)
        {
            return BoringSSLNativeFunc.AKNet_sk_X509_pop(st);
        }

        public static int SSL_CTX_check_private_key(IntPtr ctx)
        {
            return BoringSSLNativeFunc.AKNet_SSL_CTX_check_private_key(ctx);
        }

        public static int SSL_CTX_load_verify_locations(IntPtr ctx, string CAfile, string CApath)
        {
            return BoringSSLNativeFunc.AKNet_SSL_CTX_load_verify_locations(ctx, CAfile, CApath);
        }
        public static void SSL_CTX_set_cert_verify_callback(IntPtr ctx, func_common_1 func, object arg)
        {
            IntPtr uPtr = IntPtr.Zero;
            BoringSSLNativeFunc.AKNet_SSL_CTX_set_cert_verify_callback(ctx, func, uPtr);
        }

        public static void SSL_CTX_set_verify(IntPtr ctx, int mode, SSL_verify_cb callback)
        {
            BoringSSLNativeFunc.AKNet_SSL_CTX_set_verify(ctx, mode, callback);
        }

        //SSL_CTX_set_verify_depth 是 OpenSSL 提供的一个函数，用于设置在验证 SSL/TLS 证书链时，允许的最大证书验证深度（depth）。
        public static void SSL_CTX_set_verify_depth(IntPtr ctx, int depth)
        {
            BoringSSLNativeFunc.AKNet_SSL_CTX_set_verify_depth(ctx, depth);
        }

        public static ulong SSL_CTX_set_options(IntPtr ctx, ulong op)
        {
            return BoringSSLNativeFunc.AKNet_SSL_CTX_set_options(ctx, op);
        }

        public static ulong SSL_CTX_clear_options(IntPtr ctx, ulong op)
        {
            return BoringSSLNativeFunc.AKNet_SSL_CTX_clear_options(ctx, op);
        }

        public static long SSL_CTX_set_mode(IntPtr ctx, long op)
        {
            return BoringSSLNativeFunc.AKNet_SSL_CTX_set_mode(ctx, op);
        }

        public static void SSL_CTX_set_alpn_select_cb(IntPtr ctx, SSL_CTX_alpn_select_cb_func cb, object arg)
        {
            IntPtr uPtr = IntPtr.Zero;
            BoringSSLNativeFunc.AKNet_SSL_CTX_set_alpn_select_cb(ctx, cb, uPtr);
        }

        public static void SSL_CTX_set_client_hello_cb(IntPtr ctx, SSL_client_hello_cb_fn cb, object arg)
        {
            IntPtr uPtr = IntPtr.Zero;
            BoringSSLNativeFunc.AKNet_SSL_CTX_set_client_hello_cb(ctx, cb, uPtr);
        }

        public static void X509_free(IntPtr x)
        {
            BoringSSLNativeFunc.AKNet_X509_free(x);
        }

        public static void EVP_PKEY_free(IntPtr pkey)
        {
            BoringSSLNativeFunc.AKNet_EVP_PKEY_free(pkey);
        }

        public static int SSL_SESSION_get0_ticket_appdata(IntPtr ss, out ReadOnlySpan<byte> data)
        {
            byte* dataPtr = null;
            int nLength = 0;
            int rt = BoringSSLNativeFunc.AKNet_SSL_SESSION_get0_ticket_appdata(ss, out dataPtr, out nLength);
            data = new ReadOnlySpan<byte>(dataPtr, nLength);
            return rt;
        }

        public static int SSL_client_hello_get0_ext(IntPtr ss, uint type, out ReadOnlySpan<byte> data)
        {
            byte* dataPtr = null;
            int nLength = 0;
            int rt = BoringSSLNativeFunc.AKNet_SSL_client_hello_get0_ext(ss, type, out dataPtr, out nLength);
            data = new ReadOnlySpan<byte>(dataPtr, nLength);
            return rt;
        }


        public static IntPtr X509_STORE_CTX_get0_cert(IntPtr ctx)
        {
            return BoringSSLNativeFunc.AKNet_X509_STORE_CTX_get0_cert(ctx);
        }

        public static IntPtr X509_STORE_CTX_get_ex_data(IntPtr ctx)
        {
            return BoringSSLNativeFunc.AKNet_X509_STORE_CTX_get_ex_data(ctx);
        }
        
        public static int X509_verify_cert(IntPtr ctx)
        {
            return BoringSSLNativeFunc.AKNet_X509_verify_cert(ctx);
        }

        public static void X509_STORE_CTX_set_error(IntPtr ctx, int s)
        {
             BoringSSLNativeFunc.AKNet_X509_STORE_CTX_set_error(ctx, s);
        }

        public static int X509_STORE_CTX_get_error(IntPtr ctx)
        {
            return BoringSSLNativeFunc.AKNet_X509_STORE_CTX_get_error(ctx);
        }

        public static void OPENSSL_free(IntPtr ptr)
        {
            BoringSSLNativeFunc.AKNet_OPENSSL_free(ptr);
        }
            

        public static void i2d_X509(IntPtr x, out ReadOnlySpan<byte> outBuf)
        {
            byte* dataPtr = null;
            int nLength = BoringSSLNativeFunc.AKNet_i2d_X509(x, out dataPtr);
            outBuf = new ReadOnlySpan<byte>(dataPtr, nLength);
        }
        public static void i2d_PKCS7(IntPtr x, out ReadOnlySpan<byte> outBuf)
        {
            byte* dataPtr = null;
            int nLength = BoringSSLNativeFunc.AKNet_i2d_PKCS7(x, out dataPtr);
            outBuf = new ReadOnlySpan<byte>(dataPtr, nLength);
        }

        public static IntPtr X509_STORE_CTX_get0_chain(IntPtr ctx)
        {
            return BoringSSLNativeFunc.AKNet_X509_STORE_CTX_get0_chain(ctx);
        }

        public static int sk_X509_num(IntPtr ctx)
        {
            return BoringSSLNativeFunc.AKNet_sk_X509_num(ctx);
        }

        public static IntPtr PKCS7_new()
        {
            return BoringSSLNativeFunc.AKNet_PKCS7_new();
        }

        public static void PKCS7_free(IntPtr a)
        {
            BoringSSLNativeFunc.AKNet_PKCS7_free(a);
        }

        public static int PKCS7_set_type(IntPtr p7, int type)
        {
            return BoringSSLNativeFunc.AKNet_PKCS7_set_type(p7, type);
        }

        public static int PKCS7_content_new(IntPtr p7, int nid)
        {
            return BoringSSLNativeFunc.AKNet_PKCS7_content_new(p7, nid);
        }

        public static int PKCS7_add_certificate(IntPtr p7, IntPtr x)
        {
            return BoringSSLNativeFunc.AKNet_PKCS7_add_certificate(p7, x);
        }

        public static IntPtr sk_X509_value(IntPtr sk, int idx)
        {
            return BoringSSLNativeFunc.AKNet_sk_X509_value(sk, idx);
        }

    }
}