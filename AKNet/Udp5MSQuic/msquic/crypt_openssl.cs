using AKNet.Common;
using System.Security.Cryptography;
using System;
using System.Diagnostics;

namespace AKNet.Udp5MSQuic.Common
{
    internal class EVP_aes_128_gcm
    {
        public const int KeySize = 16;
        public const int NonceSize = 12;
        public const int TagSize = 16;

        public void Encrypt(CXPLAT_HP_KEY Key, byte[] plaintext, out byte[] cipher, out byte[] tag)
        {
            NetLog.Assert(Key.Key.Length == KeySize);
            NetLog.Assert(Key.nonce.Length == NonceSize);

            using AesGcm aes = new AesGcm(Key.Key);
            cipher = new byte[plaintext.Length];
            tag = new byte[TagSize];
            aes.Encrypt(Key.nonce, plaintext, cipher, tag);
        }

        public void Decrypt(CXPLAT_HP_KEY Key, byte[] tag, byte[] cipher, out byte[] plaintext)
        {
            NetLog.Assert(tag.Length == TagSize);
            NetLog.Assert(Key.Key.Length == KeySize);
            NetLog.Assert(Key.nonce.Length == NonceSize);

            plaintext = new byte[cipher.Length];
            using AesGcm aes = new AesGcm(Key.Key);
            aes.Decrypt(Key.nonce, cipher, tag, plaintext);
        }

        public void Encrypt(CXPLAT_HP_KEY Key, int BatchSize, QUIC_SSBuffer Cipher, QUIC_SSBuffer Mask)
        {
            NetLog.Assert(Key.Key.Length == KeySize);
            NetLog.Assert(Key.nonce.Length == NonceSize);

            
        }

        //解码
        public void Decrypt(CXPLAT_HP_KEY Key, int tagLength, QUIC_SSBuffer CipherAndTag, ref QUIC_SSBuffer Mask)
        {
            NetLog.Assert(tagLength / TagSize == 0);
            NetLog.Assert(Key.Key.Length == KeySize);
            NetLog.Assert(Key.nonce.Length == NonceSize);

            using AesGcm aes = new AesGcm(Key.Key);
            ReadOnlySpan<byte> Cipher = CipherAndTag.GetSpan().Slice(0, Cipher.Length - tagLength);
            ReadOnlySpan<byte> Tag = CipherAndTag.GetSpan().Slice(Cipher.Length - tagLength, tagLength);
            aes.Decrypt(Key.nonce, Cipher, Tag, Mask.GetSpan());
        }
    }

    internal class EVP_aes_256_gcm
    {
        public int KeySize => 32;
        public int NonceSize => 12;
        public int TagSize => 16;

        public byte[] Encrypt(byte[] key, byte[] nonce, byte[] aad, byte[] plaintext)
        {
            NetLog.Assert(key.Length == 32);

            using var aes = new AesGcm(key);
            var ciphertext = new byte[plaintext.Length];
            var tag = new byte[16];

            aes.Encrypt(nonce, plaintext, ciphertext, tag, aad);

            var result = new byte[ciphertext.Length + tag.Length];
            Buffer.BlockCopy(ciphertext, 0, result, 0, ciphertext.Length);
            Buffer.BlockCopy(tag, 0, result, ciphertext.Length, tag.Length);

            return result;
        }

        public byte[] Decrypt(byte[] key, byte[] nonce, byte[] aad, byte[] cipherWithTag)
        {
            NetLog.Assert(key.Length == 32);

            using var aes = new AesGcm(key);
            var ciphertext = new byte[cipherWithTag.Length - 16];
            var tag = new byte[16];

            Buffer.BlockCopy(cipherWithTag, 0, ciphertext, 0, ciphertext.Length);
            Buffer.BlockCopy(tag, 0, cipherWithTag, ciphertext.Length, tag.Length);

            var plaintext = new byte[ciphertext.Length];
            aes.Decrypt(nonce, ciphertext, tag, plaintext, aad);

            return plaintext;
        }
    }

    internal static partial class MSQuicFunc
    {
        static readonly EVP_aes_128_gcm CXPLAT_AES_128_GCM_ALG_HANDLE = new EVP_aes_128_gcm();
        static readonly EVP_aes_256_gcm CXPLAT_AES_256_GCM_ALG_HANDLE = new EVP_aes_256_gcm();

        static ulong CxPlatCryptInitialize()
        {
            return 0;
        }

        static bool CxPlatCryptSupports(CXPLAT_AEAD_TYPE AeadType)
        {
            switch (AeadType)
            {
                case CXPLAT_AEAD_TYPE.CXPLAT_AEAD_AES_128_GCM:
                    return true;
                case CXPLAT_AEAD_TYPE.CXPLAT_AEAD_AES_256_GCM:
                    return true;
                case CXPLAT_AEAD_TYPE.CXPLAT_AEAD_CHACHA20_POLY1305:
                    return false;
                default:
                    return false;
            }
        }

        static ulong CxPlatKeyCreate(CXPLAT_AEAD_TYPE AeadType, QUIC_SSBuffer RawKey, ref CXPLAT_KEY NewKey)
        {
            ulong Status = QUIC_STATUS_SUCCESS;
            NewKey = new CXPLAT_KEY(AeadType, RawKey);
            return Status;
        }

        static ulong CxPlatEncrypt(CXPLAT_KEY Key, byte[] Iv, QUIC_SSBuffer AuthData, QUIC_SSBuffer out_Buffer)
        {
            NetLog.Assert(CXPLAT_ENCRYPTION_OVERHEAD <= out_Buffer.Length);
            //int PlainTextLength = out_Buffer.Length - CXPLAT_ENCRYPTION_OVERHEAD;
            //if (Key.nType == CXPLAT_AEAD_TYPE.CXPLAT_AEAD_AES_256_GCM) 
            //{
            //    CXPLAT_AES_256_GCM_ALG_HANDLE.Encode(AuthData, Key.Key, Iv, out_Buffer, out_Tag);
            //}
            return QUIC_STATUS_SUCCESS;
        }

        static ulong CxPlatDecrypt(CXPLAT_KEY Key, QUIC_SSBuffer Iv, QUIC_SSBuffer Encrypted_Buffer, QUIC_SSBuffer out_Buffer)
        {
            //NetLog.Assert(CXPLAT_ENCRYPTION_OVERHEAD <= Encrypted_Buffer.Length);
            //if (Key.nType == CXPLAT_AEAD_TYPE.CXPLAT_AEAD_AES_256_GCM)
            //{
            //    CXPLAT_AES_256_GCM_ALG_HANDLE.Decode(Key.Key, Iv, Encrypted_Buffer, Tag_Buffer, out_Buffer);
            //}
            return QUIC_STATUS_SUCCESS;
        }

        static ulong CxPlatHpComputeMask(CXPLAT_HP_KEY Key, int BatchSize, QUIC_SSBuffer Cipher, QUIC_SSBuffer Mask)
        {
            if (Key.Aead == CXPLAT_AEAD_TYPE.CXPLAT_AEAD_AES_128_GCM)
            {
                int tagLength = CXPLAT_HP_SAMPLE_LENGTH * BatchSize;
                CXPLAT_AES_128_GCM_ALG_HANDLE.Decrypt(Key, tagLength, Cipher.Buffer, ref Mask);
            }
            else if (Key.Aead == CXPLAT_AEAD_TYPE.CXPLAT_AEAD_AES_256_GCM)
            {
                Debug.Assert(false);
            }
            else
            {
                Debug.Assert(false);
            }
            return QUIC_STATUS_SUCCESS;
        }

        //在 QUIC 中，为了防止中间设备（如网络监控或负载均衡器）通过观察数据包头部字段来干扰连接，QUIC 使用了一种称为 Header Protection（头保护） 的机制：
        //对数据包头部的某些关键字段（如 Packet Number 和 Key Phase）进行加密；
        //加密使用的密钥来自当前加密密钥（Packet Key）；
        //加密算法是基于 AES 或 ChaCha20 等 HP（Header Protection）算法；
        //每个方向（发送/接收）都需要独立的 HP Key。
        static ulong CxPlatHpKeyCreate(CXPLAT_AEAD_TYPE AeadType, QUIC_SSBuffer RawKey, ref CXPLAT_HP_KEY NewKey)
        {
            ulong Status = QUIC_STATUS_SUCCESS;
            if (!CxPlatCryptSupports(AeadType))
            {
                Status = QUIC_STATUS_NOT_SUPPORTED;
                goto Exit;
            }

            int nLength = CxPlatKeyLength(AeadType);
            byte[] tt = new byte[nLength];
            RawKey.GetSpan().Slice(0, nLength).CopyTo(tt);
            CXPLAT_HP_KEY Key = new CXPLAT_HP_KEY(AeadType, tt);
            NewKey = Key;
        Exit:
            return Status;
        }
    }
}
