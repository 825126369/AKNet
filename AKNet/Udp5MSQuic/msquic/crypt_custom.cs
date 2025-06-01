using AKNet.Common;
using System.Security.Cryptography;

namespace AKNet.Udp5MSQuic.Common
{
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

    internal static partial class MSQuicFunc
    {
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
                    //return CXPLAT_CHACHA20_POLY1305_ALG_HANDLE != null;
                default:
                    return false;
            }
        }

        static ulong CxPlatKeyCreate(CXPLAT_AEAD_TYPE AeadType, QUIC_SSBuffer RawKey, ref CXPLAT_KEY NewKey)
        {
            ulong Status = QUIC_STATUS_SUCCESS;
            switch (AeadType)
            {
                case CXPLAT_AEAD_TYPE.CXPLAT_AEAD_AES_128_GCM:
                    break;
                default:
                    Status = QUIC_STATUS_NOT_SUPPORTED;
                    break;
            }

            NewKey = new CXPLAT_KEY(AeadType);
            RawKey.CopyTo(NewKey.Key);
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

        static ulong CxPlatHpKeyCreate(CXPLAT_AEAD_TYPE AeadType, QUIC_SSBuffer RawKey, ref CXPLAT_HP_KEY NewKey)
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
