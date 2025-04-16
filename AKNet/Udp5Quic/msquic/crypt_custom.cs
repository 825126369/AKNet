using AKNet.Common;
using System;
using System.Net.NetworkInformation;
using System.Security.Cryptography;
using System.Threading.Tasks;
using static AKNet.Udp5Quic.Common.QUIC_TOKEN_CONTENTS;

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

        static ulong CxPlatEncrypt(CXPLAT_KEY Key, byte[] Iv, ReadOnlySpan<byte> AuthData, ReadOnlySpan<byte> Buffer)
        {
            NetLog.Assert(CXPLAT_ENCRYPTION_OVERHEAD <= BufferLength);

            int PlainTextLength = BufferLength - CXPLAT_ENCRYPTION_OVERHEAD;
            byte Tag = Buffer[PlainTextLength];
            int OutLen = 0;
            if (Key.nType == CXPLAT_AEAD_TYPE.CXPLAT_AEAD_AES_256_GCM) 
            {
                Buffer = CXPLAT_AES_256_GCM_ALG_HANDLE.Encode(AuthData, Key.Key, Iv, Tag);
            }
            return QUIC_STATUS_SUCCESS;
        }

        static ulong CxPlatDecrypt(CXPLAT_KEY Key, byte[] Iv, int AuthDataLength, byte[] AuthData, ReadOnlySpan<byte> Buffer)
        {
            NetLog.Assert(CXPLAT_ENCRYPTION_OVERHEAD <= Buffer.Length);

            //int CipherTextLength = BufferLength - CXPLAT_ENCRYPTION_OVERHEAD;
            //byte[] Tag = Buffer + CipherTextLength;
            //int OutLen;

            //EVP_CIPHER_CTX CipherCtx = (EVP_CIPHER_CTX)Key;
            //OSSL_PARAM AlgParam[2];

            //if (EVP_DecryptInit_ex(CipherCtx, null, null, null, Iv) != 1)
            //{
            //    return QUIC_STATUS_TLS_ERROR;
            //}

            //if (AuthData != null && EVP_DecryptUpdate(CipherCtx, null, &OutLen, AuthData, (int)AuthDataLength) != 1)
            //{
            //    return QUIC_STATUS_TLS_ERROR;
            //}

            //if (EVP_DecryptUpdate(CipherCtx, Buffer, &OutLen, Buffer, (int)CipherTextLength) != 1)
            //{
            //    return QUIC_STATUS_TLS_ERROR;
            //}
            
            //AlgParam[0] = OSSL_PARAM_construct_octet_string("tag", Tag, CXPLAT_ENCRYPTION_OVERHEAD);
            //AlgParam[1] = OSSL_PARAM_construct_end();

            //if (EVP_CIPHER_CTX_set_params(CipherCtx, AlgParam) != 1)
            //{
            //    return QUIC_STATUS_TLS_ERROR;
            //}

            //if (EVP_DecryptFinal_ex(CipherCtx, Tag, &OutLen) != 1)
            //{
            //    return QUIC_STATUS_TLS_ERROR;
            //}

            return QUIC_STATUS_SUCCESS;
        }

        static ulong CxPlatHpComputeMask(CXPLAT_HP_KEY Key, int BatchSize, byte[] Cipher, byte[] Mask)
        {
            //int OutLen = 0;
            //if (Key.Aead == CXPLAT_AEAD_CHACHA20_POLY1305)
            //{
            //    static const uint8_t Zero[] = { 0, 0, 0, 0, 0 };
            //    for (uint32_t i = 0, Offset = 0; i < BatchSize; ++i, Offset += CXPLAT_HP_SAMPLE_LENGTH) {
            //        if (EVP_EncryptInit_ex(Key->CipherCtx, NULL, NULL, NULL, Cipher + Offset) != 1) {
            //            QuicTraceEvent(
            //                LibraryError,
            //                "[ lib] ERROR, %s.",
            //                "EVP_EncryptInit_ex (hp) failed");
            //            return QUIC_STATUS_TLS_ERROR;
            //        }
            //        if (EVP_EncryptUpdate(Key->CipherCtx, Mask + Offset, &OutLen, Zero, sizeof(Zero)) != 1) {
            //            QuicTraceEvent(
            //                LibraryError,
            //                "[ lib] ERROR, %s.",
            //                "EVP_EncryptUpdate (hp) failed");
            //            return QUIC_STATUS_TLS_ERROR;
            //        }
            //    }
            //} else
            //{
            //    if (EVP_EncryptUpdate(Key->CipherCtx, Mask, &OutLen, Cipher, CXPLAT_HP_SAMPLE_LENGTH * BatchSize) != 1)
            //    {
            //        QuicTraceEvent(
            //            LibraryError,
            //            "[ lib] ERROR, %s.",
            //            "EVP_EncryptUpdate failed");
            //        return QUIC_STATUS_TLS_ERROR;
            //    }
            //}
            return QUIC_STATUS_SUCCESS;
        }

        static ulong CxPlatHpKeyCreate(CXPLAT_AEAD_TYPE AeadType, byte[] RawKey, ref CXPLAT_HP_KEY NewKey)
        {
            //    BCRYPT_ALG_HANDLE AlgHandle;
            //        CXPLAT_HP_KEY* Key = NULL;
            //        uint32_t AllocLength;
            //        uint8_t KeyLength;

            //    switch (AeadType) {
            //    case CXPLAT_AEAD_AES_128_GCM:
            //        KeyLength = 16;
            //        AllocLength = sizeof(CXPLAT_HP_KEY);
            //        AlgHandle = CXPLAT_AES_ECB_ALG_HANDLE;
            //        break;
            //    case CXPLAT_AEAD_AES_256_GCM:
            //        KeyLength = 32;
            //        AllocLength = sizeof(CXPLAT_HP_KEY);
            //        AlgHandle = CXPLAT_AES_ECB_ALG_HANDLE;
            //        break;
            //    case CXPLAT_AEAD_CHACHA20_POLY1305:
            //        KeyLength = 32;
            //        AllocLength =
            //            sizeof(CXPLAT_HP_KEY) +
            //            sizeof(BCRYPT_AUTHENTICATED_CIPHER_MODE_INFO) +
            //            CXPLAT_ENCRYPTION_OVERHEAD;
            //        AlgHandle = CXPLAT_CHACHA20_POLY1305_ALG_HANDLE;
            //        break;
            //    default:
            //        return QUIC_STATUS_NOT_SUPPORTED;
            //    }

            //    Key = CXPLAT_ALLOC_NONPAGED(AllocLength, QUIC_POOL_TLS_HP_KEY);
            //    if (Key == NULL) {
            //        QuicTraceEvent(
            //            AllocFailure,
            //            "Allocation of '%s' failed. (%llu bytes)",
            //            "CXPLAT_HP_KEY",
            //            AllocLength);
            //        return QUIC_STATUS_OUT_OF_MEMORY;
            //    }

            //Key->Aead = AeadType;

            //NTSTATUS Status =
            //    BCryptGenerateSymmetricKey(
            //        AlgHandle,
            //        &Key->Key,
            //        NULL, // Let BCrypt manage the memory for this key.
            //        0,
            //        (uint8_t*)RawKey,
            //        KeyLength,
            //        0);
            //if (!NT_SUCCESS(Status))
            //{
            //    QuicTraceEvent(
            //        LibraryErrorStatus,
            //        "[ lib] ERROR, %u, %s.",
            //        Status,
            //        (AeadType == CXPLAT_AEAD_CHACHA20_POLY1305) ?
            //            "BCryptGenerateSymmetricKey (ChaCha)" :
            //            "BCryptGenerateSymmetricKey (ECB)");
            //    goto Error;
            //}

            //if (AeadType == CXPLAT_AEAD_CHACHA20_POLY1305)
            //{
            //    BCRYPT_INIT_AUTH_MODE_INFO(*Key->Info);
            //    Key->Info->pbTag = (uint8_t*)(Key->Info + 1);
            //    Key->Info->cbTag = CXPLAT_ENCRYPTION_OVERHEAD;
            //    Key->Info->pbAuthData = NULL;
            //    Key->Info->cbAuthData = 0;
            //}

            //*NewKey = Key;
            //Key = NULL;

            //Error:

            //if (Key)
            //{
            //    CXPLAT_FREE(Key, QUIC_POOL_TLS_HP_KEY);
            //    Key = NULL;
            //}

            //return NtStatusToQuicStatus(Status);
            return 0;
        }
    }
}
