using AKNet.Common;
using System;
using System.Security.Cryptography;

namespace AKNet.Udp5Quic.Common
{
    internal static class CXPLAT_AES_256_GCM_ALG_HANDLE
    {
        static readonly byte[] nonce = new byte[12];
        public static byte[] Encode(ReadOnlySpan<byte> plaintext, byte[] key, byte[] iv, byte[] tag)
        {
            using (var aesGcm = new AesGcm(key))
            {
                CxPlatRandom.Random(nonce);
                byte[] ciphertext = new byte[plaintext.Length];
                aesGcm.Encrypt(nonce, plaintext, ciphertext, tag);
                return ciphertext;
            }
        }

        public static byte[] Decode(byte[] ciphertext, byte[] key, byte[] iv)
        {
            using (var aesGcm = new AesGcm(key))
            {
                byte[] plaintext = new byte[ciphertext.Length];
                aesGcm.Decrypt(iv, ciphertext, plaintext);
                return plaintext;
            }
        }
    }

    internal class EVP_CIPHER_CTX : CXPLAT_KEY
    {
        public EVP_CIPHER_CTX(CXPLAT_AEAD_TYPE nType, CXPLAT_AEAD_TYPE_SIZE nKeyLength) : base(nType, nKeyLength)
        {

        }
    }

    internal static partial class MSQuicFunc
    {
        static ulong CxPlatKeyCreate(CXPLAT_AEAD_TYPE AeadType, byte[] RawKey, ref CXPLAT_KEY NewKey)
        {
            ulong Status = QUIC_STATUS_SUCCESS;
            switch (AeadType)
            {
                case CXPLAT_AEAD_TYPE.CXPLAT_AEAD_AES_128_GCM:
                    NewKey = new CXPLAT_KEY(CXPLAT_AEAD_TYPE.CXPLAT_AEAD_AES_128_GCM, CXPLAT_AEAD_TYPE_SIZE.CXPLAT_AEAD_AES_128_GCM_SIZE);
                    break;
                case CXPLAT_AEAD_TYPE.CXPLAT_AEAD_AES_256_GCM:
                    NewKey = new CXPLAT_KEY(CXPLAT_AEAD_TYPE.CXPLAT_AEAD_AES_256_GCM, CXPLAT_AEAD_TYPE_SIZE.CXPLAT_AEAD_AES_256_GCM_SIZE);
                    break;
                case CXPLAT_AEAD_TYPE.CXPLAT_AEAD_CHACHA20_POLY1305:
                    NewKey = new CXPLAT_KEY(CXPLAT_AEAD_TYPE.CXPLAT_AEAD_CHACHA20_POLY1305, CXPLAT_AEAD_TYPE_SIZE.CXPLAT_AEAD_CHACHA20_POLY1305_SIZE);
                    break;
                default:
                    Status = QUIC_STATUS_NOT_SUPPORTED;
                    break;
            }
            return Status;
        }

        static ulong CxPlatEncrypt(CXPLAT_KEY Key, byte[] Iv, int AuthDataLength, byte[] AuthData, int BufferLength, byte[] Buffer)
        {
            NetLog.Assert(CXPLAT_ENCRYPTION_OVERHEAD <= BufferLength);

            int PlainTextLength = BufferLength - CXPLAT_ENCRYPTION_OVERHEAD;
            byte Tag = Buffer[PlainTextLength];
            int OutLen;

            if (Key.nType == CXPLAT_AEAD_TYPE.CXPLAT_AEAD_AES_256_GCM) 
            {
                Buffer = CXPLAT_AES_256_GCM_ALG_HANDLE.Encode(AuthData, Key.Key, Iv, Tag);
            }
            return QUIC_STATUS_SUCCESS;
        }
    }
}
