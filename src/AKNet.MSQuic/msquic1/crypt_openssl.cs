/************************************Copyright*****************************************
*        ProjectName:AKNet
*        Web:https://github.com/825126369/AKNet
*        Description:C#游戏网络库
*        Author:许珂
*        StartTime:2024/11/01 00:00:00
*        ModifyTime:2025/11/30 19:43:18
*        Copyright:MIT软件许可证
************************************Copyright*****************************************/
using AKNet.Common;
using System;
using System.Diagnostics;
using System.Security.Cryptography;

namespace MSQuic1
{
    internal class EVP_aes_128_gcm
    {
        public const int KeySize = 16;
        public const int NonceSize = 12;
        public const int TagSize = 16;

        public void Encrypt(QUIC_SSBuffer Key, QUIC_SSBuffer nonce, QUIC_SSBuffer AuthData, QUIC_SSBuffer plaintext, QUIC_SSBuffer Ciper, QUIC_SSBuffer Tag)
        {
            NetLog.Assert(Key.Length == KeySize);
            NetLog.Assert(nonce.Length == NonceSize);
            using AesGcm aes = new AesGcm(Key.GetSpan());
            aes.Encrypt(nonce.GetSpan(), plaintext.GetSpan(), Ciper.GetSpan(), Tag.GetSpan(), AuthData.GetSpan());
        }

        public void Decrypt(QUIC_SSBuffer Key, QUIC_SSBuffer nonce, QUIC_SSBuffer AuthData, QUIC_SSBuffer plaintext, QUIC_SSBuffer Cipher, QUIC_SSBuffer Tag)
        {
            NetLog.Assert(Key.Length == KeySize);
            NetLog.Assert(nonce.Length == NonceSize);
            NetLog.Assert(Tag.Length == TagSize);
            using AesGcm aes = new AesGcm(Key.GetSpan());
            aes.Decrypt(nonce.GetSpan(), Cipher.GetSpan(), Tag.GetSpan(), plaintext.GetSpan(), AuthData.GetSpan());
        }
    }

    internal class EVP_aes_256_gcm
    {
        public const int KeySize = 32;
        public const int NonceSize = 12;
        public const int TagSize = 16;

        public void Encrypt(QUIC_SSBuffer Key, QUIC_SSBuffer nonce, QUIC_SSBuffer AuthData, QUIC_SSBuffer plaintext, QUIC_SSBuffer Ciper, QUIC_SSBuffer Tag)
        {
            NetLog.Assert(Key.Length == KeySize);
            NetLog.Assert(nonce.Length == NonceSize);
            using AesGcm aes = new AesGcm(Key.GetSpan());
            aes.Encrypt(nonce.GetSpan(), plaintext.GetSpan(), Ciper.GetSpan(), Tag.GetSpan(), AuthData.GetSpan());
        }

        public void Decrypt(QUIC_SSBuffer Key, QUIC_SSBuffer nonce, QUIC_SSBuffer AuthData, QUIC_SSBuffer plaintext, QUIC_SSBuffer Cipher, QUIC_SSBuffer Tag)
        {
            NetLog.Assert(Key.Length == KeySize);
            NetLog.Assert(nonce.Length == NonceSize);
            NetLog.Assert(Tag.Length == TagSize);
            using AesGcm aes = new AesGcm(Key.GetSpan());
            aes.Decrypt(nonce.GetSpan(), Cipher.GetSpan(), Tag.GetSpan(), plaintext.GetSpan(), AuthData.GetSpan());
        }
    }

    internal class EVP_aes_128_ecb
    {
        public const int KeySize = 16;
        public const int NonceSize = 12;
        public const int TagSize = 16;

        //编码和解码是一起的
        public void Encrypt(QUIC_SSBuffer Key, QUIC_SSBuffer plaintext, QUIC_SSBuffer Ciper)
        {
            using (Aes aesAlg = Aes.Create())
            {
                aesAlg.Key = Key.GetSpan().ToArray();
                aesAlg.Mode = CipherMode.ECB;         // 设置 ECB 模式
                aesAlg.Padding = PaddingMode.None;   // 使用 PKCS7 填充
                ICryptoTransform encryptor = aesAlg.CreateEncryptor();
                ReadOnlySpan<byte> temp = encryptor.TransformFinalBlock(plaintext.Buffer, plaintext.Offset, plaintext.Length);
                temp.CopyTo(Ciper.GetSpan());
            }
        }
    }

    internal static partial class MSQuicFunc
    {
        static readonly EVP_aes_128_gcm CXPLAT_AES_128_GCM_ALG_HANDLE = new EVP_aes_128_gcm();
        static readonly EVP_aes_256_gcm CXPLAT_AES_256_GCM_ALG_HANDLE = new EVP_aes_256_gcm();
        static readonly EVP_aes_128_ecb CXPLAT_AES_128_ECB_ALG_HANDLE = new EVP_aes_128_ecb();

        static int CxPlatCryptInitialize()
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

        static int CxPlatHashCreate(CXPLAT_HASH_TYPE HashType, QUIC_SSBuffer Salt, out CXPLAT_HASH NewHash)
        {
            /*在密码学和安全领域，盐（Salt） 和 哈希（Hash） 是两个非常重要的概念，它们通常一起使用来增强密码的安全性。以下是对这两个概念的详细解释：
                1. 盐（Salt）
                盐 是一个随机生成的值，通常用于密码哈希过程中，以防止彩虹表攻击（Rainbow Table Attack）和预计算攻击。
                作用
                增加唯一性：即使两个用户使用相同的密码，由于盐的不同，生成的哈希值也会不同。
                防止彩虹表攻击：彩虹表是一种预计算的哈希值表，用于快速查找密码。通过添加盐，可以使得彩虹表攻击变得不可行。
            */

            NewHash = null;
            int Status = QUIC_STATUS_SUCCESS;
            HMAC mHashAlgorithm = null;
            switch (HashType)
            {
                case CXPLAT_HASH_TYPE.CXPLAT_HASH_SHA256:
                    mHashAlgorithm = new HMACSHA256(Salt.Buffer);
                    break;
                case CXPLAT_HASH_TYPE.CXPLAT_HASH_SHA384:
                    mHashAlgorithm = new HMACSHA384(Salt.Buffer);
                    break;
                case CXPLAT_HASH_TYPE.CXPLAT_HASH_SHA512:
                    mHashAlgorithm = new HMACSHA512(Salt.Buffer);
                    break;
                default:
                    NetLog.LogError("不支持的哈希算法:" + HashType);
                    Status = QUIC_STATUS_INTERNAL_ERROR;
                    goto Exit;
            }

            CXPLAT_HASH Hash = new CXPLAT_HASH();
            Hash.Salt = Salt;
            Hash.mHashAlgorithm = mHashAlgorithm;
            NewHash = Hash;
        Exit:
            return Status;
        }

        static int CxPlatHashCompute(CXPLAT_HASH Hash, QUIC_SSBuffer Input, QUIC_SSBuffer Output)
        {
            var tt = Hash.mHashAlgorithm.ComputeHash(Input.Buffer, Input.Offset, Input.Length);
            tt.AsSpan().CopyTo(Output.GetSpan());
            Output.Length = tt.Length;
            return QUIC_STATUS_SUCCESS;
        }

        static int CxPlatKeyCreate(CXPLAT_AEAD_TYPE AeadType, QUIC_SSBuffer RawKey, ref CXPLAT_KEY NewKey)
        {
            int Status = QUIC_STATUS_SUCCESS;
            NewKey = new CXPLAT_KEY(AeadType, RawKey.Slice(0, CxPlatKeyLength(AeadType)));
            return Status;
        }

        static int CxPlatEncrypt(CXPLAT_KEY Key, QUIC_SSBuffer Iv, QUIC_SSBuffer AuthData, QUIC_SSBuffer out_Buffer)
        {
            NetLog.Assert(CXPLAT_ENCRYPTION_OVERHEAD <= out_Buffer.Length);
            int PlainTextLength = out_Buffer.Length - CXPLAT_ENCRYPTION_OVERHEAD;

            QUIC_SSBuffer Ciper = out_Buffer.Slice(0, PlainTextLength);
            QUIC_SSBuffer Tag = out_Buffer + PlainTextLength;
            if (Key.nType == CXPLAT_AEAD_TYPE.CXPLAT_AEAD_AES_128_GCM)
            {
                //NetLog.Log("CxPlatEncrypt");
                //NetLogHelper.PrintByteArray("Key", Key.Key.GetSpan());
                //NetLogHelper.PrintByteArray("Iv", Iv.GetSpan());
                //NetLogHelper.PrintByteArray("AuthData", AuthData.GetSpan());
                //NetLogHelper.PrintByteArray("Tag", Tag.GetSpan());
                //NetLogHelper.PrintByteArray("Ciper", Ciper.GetSpan());
                CXPLAT_AES_128_GCM_ALG_HANDLE.Encrypt(Key.Key, Iv, AuthData, Ciper, Ciper, Tag); //这里输出Tag
            }
            else if (Key.nType == CXPLAT_AEAD_TYPE.CXPLAT_AEAD_AES_256_GCM)
            {
                CXPLAT_AES_256_GCM_ALG_HANDLE.Encrypt(Key.Key, Iv, AuthData, Ciper, Ciper, Tag); //这里输出Tag
            }
            else
            {
                NetLog.Assert(false, Key.nType);
            }
            return QUIC_STATUS_SUCCESS;
        }

        static int CxPlatDecrypt(CXPLAT_KEY Key, QUIC_SSBuffer Iv, QUIC_SSBuffer AuthData, QUIC_SSBuffer out_Buffer)
        {
            NetLog.Assert(CXPLAT_ENCRYPTION_OVERHEAD <= out_Buffer.Length);
            int CipherTextLength = out_Buffer.Length - CXPLAT_ENCRYPTION_OVERHEAD;

            QUIC_SSBuffer Ciper = out_Buffer.Slice(0, CipherTextLength);
            QUIC_SSBuffer Tag = out_Buffer + CipherTextLength;
            if (Key.nType == CXPLAT_AEAD_TYPE.CXPLAT_AEAD_AES_128_GCM)
            {
                // NetLog.Log("CxPlatDecrypt");
                //NetLogHelper.PrintByteArray("Key", Key.Key.GetSpan());
                //NetLogHelper.PrintByteArray("Iv", Iv.GetSpan());
                //NetLogHelper.PrintByteArray("AuthData", AuthData.GetSpan());
                //NetLogHelper.PrintByteArray("Tag", Tag.GetSpan());
                //NetLogHelper.PrintByteArray("Ciper", Ciper.GetSpan());
                CXPLAT_AES_128_GCM_ALG_HANDLE.Decrypt(Key.Key, Iv, AuthData, Ciper, Ciper, Tag);
            }
            else if (Key.nType == CXPLAT_AEAD_TYPE.CXPLAT_AEAD_AES_256_GCM)
            {
                CXPLAT_AES_256_GCM_ALG_HANDLE.Decrypt(Key.Key, Iv, AuthData, Ciper, Ciper, Tag); //这里输出Tag
            }
            return QUIC_STATUS_SUCCESS;
        }

        static int CxPlatHpComputeMask(CXPLAT_HP_KEY Key, int BatchSize, QUIC_SSBuffer Cipher, QUIC_SSBuffer outMask)
        {
            if (Key.Aead == CXPLAT_AEAD_TYPE.CXPLAT_AEAD_AES_128_GCM)
            {
                CXPLAT_AES_128_ECB_ALG_HANDLE.Encrypt(Key.Key, Cipher.Slice(0, CXPLAT_HP_SAMPLE_LENGTH * BatchSize), outMask);
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
        static int CxPlatHpKeyCreate(CXPLAT_AEAD_TYPE AeadType, QUIC_SSBuffer RawKey, ref CXPLAT_HP_KEY NewKey)
        {
            int Status = QUIC_STATUS_SUCCESS;
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
            
        static void CxPlatKeyFree(CXPLAT_KEY Key)
        {
            
        }
    }
}
