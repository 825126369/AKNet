using System.Runtime.InteropServices;
using System;
using System.Security.Cryptography;

namespace AKNet.Udp2MSQuic.Common
{
    internal class EVP_CIPHER_CTX
    {

    }

    internal static class CXPLAT_AES_128_GCM_ALG_HANDLE
    {
        public static void Encode(QUIC_SSBuffer key, QUIC_SSBuffer iv, QUIC_SSBuffer plaintext, QUIC_SSBuffer ciphertext, QUIC_SSBuffer tag)
        {
            using (var aesGcm = new AesGcm(key.GetSpan()))
            {
                aesGcm.Encrypt(iv.GetSpan(), plaintext.GetSpan(), ciphertext.GetSpan(), tag.GetSpan());
            }
        }

        public static void Decode(QUIC_SSBuffer key, QUIC_SSBuffer iv, QUIC_SSBuffer ciphertext, QUIC_SSBuffer tag, QUIC_SSBuffer plaintext)
        {
            using (var aesGcm = new AesGcm(key.GetSpan()))
            {
                aesGcm.Decrypt(iv.GetSpan(), ciphertext.GetSpan(), tag.GetSpan(), plaintext.GetSpan());
            }
        }
    }

    internal static class CXPLAT_AES_256_GCM_ALG_HANDLE
    {
        public static void Encode(QUIC_SSBuffer key, QUIC_SSBuffer iv, QUIC_SSBuffer plaintext, QUIC_SSBuffer ciphertext, QUIC_SSBuffer tag)
        {
            using (var aesGcm = new AesGcm(key.GetSpan()))
            {
                aesGcm.Encrypt(iv.GetSpan(), plaintext.GetSpan(), ciphertext.GetSpan(), tag.GetSpan());
            }
        }

        public static void Decode(QUIC_SSBuffer key, QUIC_SSBuffer iv, QUIC_SSBuffer ciphertext, QUIC_SSBuffer tag, QUIC_SSBuffer plaintext)
        {
            using (var aesGcm = new AesGcm(key.GetSpan()))
            {
                aesGcm.Decrypt(iv.GetSpan(), ciphertext.GetSpan(), tag.GetSpan(), plaintext.GetSpan());
            }
        }
    }

    internal unsafe static class OpenSSLWrapper
    {
        
    }
}
