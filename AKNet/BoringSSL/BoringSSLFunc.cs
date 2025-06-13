using System;
using System.Runtime.InteropServices;

namespace AKNet.BoringSSL
{
    //����� ��Ҫ�� ���� C# ֱ�ӵ��ã����ε�����ȫ���
    internal static unsafe partial class BoringSSLFunc
    {
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
    }
}
