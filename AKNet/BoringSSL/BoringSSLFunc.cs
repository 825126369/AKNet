using AKNet.Udp4LinuxTcp.Common;
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
            fixed (char* pChar = str)
            {
                return BoringSSLNativeFunc.AKNet_SSL_CTX_set_ciphersuites(ctx, pChar);
            }
        }

        public static int SSL_CTX_set_default_verify_paths(IntPtr ctx)
        {
            return BoringSSLNativeFunc.AKNet_SSL_CTX_set_default_verify_paths(ctx);
        }

        public static int SSL_CTX_set_quic_method(IntPtr ctx, SSL_QUIC_METHOD meths)
        {
            return BoringSSLNativeFunc.AKNet_SSL_CTX_set_quic_method(ctx, &meths);
        }

        //它通常用于在 SSL/TLS 连接中绑定一些上下文信息，例如用户会话、连接状态、用户数据结构等。
        public static T SSL_get_app_data<T>(IntPtr ssl)
        {
            void* data = BoringSSLNativeFunc.AKNet_SSL_get_app_data(ssl);
            return Marshal.PtrToStructure<T>((IntPtr)data);
        }

        public static uint SSL_CIPHER_get_id(IntPtr cipher)
        {
            return BoringSSLNativeFunc.AKNet_SSL_CIPHER_get_id(cipher);
        }

        public static IntPtr SSL_get_current_cipher(IntPtr ssl)
        {
            return BoringSSLNativeFunc.AKNet_SSL_get_current_cipher(ssl);
        }

    }
}
