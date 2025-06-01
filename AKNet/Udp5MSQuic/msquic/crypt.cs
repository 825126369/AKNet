using AKNet.Common;
using System.Security.Authentication;
using System.Security.Cryptography;

namespace AKNet.Udp5MSQuic.Common
{
    internal class CXPLAT_HASH
    {
        public QUIC_BUFFER Salt;
    }

    internal class QUIC_HKDF_LABELS
    {
        public string KeyLabel;
        public string IvLabel;
        public string HpLabel;  // Header protection
        public string KuLabel;  // Key update
    }

    internal static partial class MSQuicFunc
    {
        public const int CXPLAT_HASH_SHA256_SIZE = 32;
        public const int CXPLAT_HASH_SHA384_SIZE = 48;
        public const int CXPLAT_HASH_SHA512_SIZE = 64;
        public const int CXPLAT_HASH_MAX_SIZE = 64;

        static int CxPlatHashLength(CXPLAT_HASH_TYPE Type)
        {
            switch (Type)
            {
                case CXPLAT_HASH_TYPE.CXPLAT_HASH_SHA256: return 32;
                case CXPLAT_HASH_TYPE.CXPLAT_HASH_SHA384: return 48;
                case CXPLAT_HASH_TYPE.CXPLAT_HASH_SHA512: return 64;
                default:
                    NetLog.Assert(false);
                    return 0;
            }
        }

        static ulong CxPlatHashCreate(CXPLAT_HASH_TYPE HashType, QUIC_SSBuffer Salt, ref CXPLAT_HASH NewHash)
        {
            ulong Status = QUIC_STATUS_SUCCESS;
            HashAlgorithm mHashAlgorithm = null;
            HashAlgorithmType nType = HashAlgorithmType.Sha256;
            switch (HashType)
            {
                case CXPLAT_HASH_TYPE.CXPLAT_HASH_SHA256:
                    nType = HashAlgorithmType.Sha256;
                    mHashAlgorithm = SHA256.Create();
                    break;
                case CXPLAT_HASH_TYPE.CXPLAT_HASH_SHA384:
                    nType = HashAlgorithmType.Sha384;
                    mHashAlgorithm = SHA384.Create();
                    break;
                case CXPLAT_HASH_TYPE.CXPLAT_HASH_SHA512:
                    nType = HashAlgorithmType.Sha512;
                    mHashAlgorithm = SHA512.Create();
                    break;
                default:
                    NetLog.LogError("不支持的哈希算法:" + HashType);
                    Status = QUIC_STATUS_INTERNAL_ERROR;
                    goto Exit;
            }

            CXPLAT_HASH Hash = new CXPLAT_HASH();
            Hash.Salt = mHashAlgorithm.ComputeHash(Salt.Buffer, Salt.Offset, Salt.Length);
            NewHash = Hash;
        Exit:
            return Status;
        }
        
        static ulong CxPlatHashCompute(CXPLAT_HASH Hash, QUIC_SSBuffer Input, ref QUIC_SSBuffer Output)
        {
            return QUIC_STATUS_SUCCESS;
        }

        static ulong CxPlatTlsDeriveInitialSecrets(QUIC_SSBuffer Salt, QUIC_SSBuffer CID, ref CXPLAT_SECRET ClientInitial, ref CXPLAT_SECRET ServerInitial)
        {
            ClientInitial = new CXPLAT_SECRET();
            ClientInitial.Hash = CXPLAT_HASH_TYPE.CXPLAT_HASH_SHA256;
            ClientInitial.Aead = CXPLAT_AEAD_TYPE.CXPLAT_AEAD_AES_128_GCM;
            ClientInitial.Secret = ClientInitial.Secret.Slice(0, CXPLAT_HASH_SHA256_SIZE);
            CxPlatRandom.Random(ClientInitial.Secret);

            ServerInitial = new CXPLAT_SECRET();
            ServerInitial.Hash = CXPLAT_HASH_TYPE.CXPLAT_HASH_SHA256;
            ServerInitial.Aead = CXPLAT_AEAD_TYPE.CXPLAT_AEAD_AES_128_GCM;
            ServerInitial.Secret = ServerInitial.Secret.Slice(0, CXPLAT_HASH_SHA256_SIZE);
            CxPlatRandom.Random(ServerInitial.Secret);

            return QUIC_STATUS_SUCCESS;
        }

        static ulong CxPlatHkdfExpandLabel(CXPLAT_HASH Hash, string Label, int KeyLength, ref QUIC_SSBuffer Output)
        {
            QUIC_SSBuffer LabelBuffer = new byte[64];
            NetLog.Assert(Label.Length <= 23);
            CxPlatHkdfFormatLabel(Label, KeyLength, ref LabelBuffer);

            return CxPlatHashCompute(
                    Hash,
                    LabelBuffer,
                    ref Output);
        }

        static void CxPlatHkdfFormatLabel(string Label, int HashLength, ref QUIC_SSBuffer Data)
        {
            NetLog.Assert(Label.Length <= byte.MaxValue - CXPLAT_HKDF_PREFIX_LEN);
            int LabelLength = Label.Length;

            Data[0] = (byte)(HashLength >> 8);
            Data[1] = (byte)(HashLength & 0xff);
            Data[2] = (byte)(CXPLAT_HKDF_PREFIX_LEN + LabelLength);

            EndianBitConverter.SetBytes(Data.Buffer, 3, CXPLAT_HKDF_PREFIX);

            Data[3 + CXPLAT_HKDF_PREFIX_LEN + LabelLength] = 0;
            Data.Length = 3 + CXPLAT_HKDF_PREFIX_LEN + LabelLength + 1;
            Data[Data.Length] = 0x1;
            Data.Length += 1;
        }

        //这里派生，但没有相关的API，就不派生了
        static ulong QuicPacketKeyDerive(QUIC_PACKET_KEY_TYPE KeyType, QUIC_HKDF_LABELS HkdfLabels, CXPLAT_SECRET Secret,
                string SecretName, bool CreateHpKey, ref QUIC_PACKET_KEY NewKey)
        {
            ulong Status = QUIC_STATUS_SUCCESS;
            int SecretLength = CxPlatHashLength(Secret.Hash);
            int KeyLength = CxPlatKeyLength(Secret.Aead);

            NetLog.Assert(SecretLength >= KeyLength);
            NetLog.Assert(SecretLength >= CXPLAT_IV_LENGTH);
            NetLog.Assert(SecretLength <= CXPLAT_HASH_MAX_SIZE);

            QUIC_PACKET_KEY Key = new QUIC_PACKET_KEY();
            Key.Type = KeyType;
            CxPlatRandom.Random(Key.Iv);

            Status = CxPlatKeyCreate(Secret.Aead, Secret.Secret, ref Key.PacketKey);
            if (QUIC_FAILED(Status))
            {
                goto Exit;
            }

            if (CreateHpKey)
            {
                Status = CxPlatHpKeyCreate(Secret.Aead, Secret.Secret, ref Key.HeaderKey);
                if (QUIC_FAILED(Status))
                {
                    goto Exit;
                }
            }

            if (KeyType == QUIC_PACKET_KEY_TYPE.QUIC_PACKET_KEY_1_RTT)
            {
                Key.TrafficSecret = Secret;
            }

            NewKey = Key;
        Exit:
            return Status;
        }

        static ulong QuicPacketKeyCreateInitial(bool IsServer, QUIC_HKDF_LABELS HkdfLabels, QUIC_SSBuffer Salt, QUIC_SSBuffer CID,
            ref QUIC_PACKET_KEY NewReadKey, ref QUIC_PACKET_KEY NewWriteKey)
        {
            CXPLAT_SECRET ClientInitial = new CXPLAT_SECRET();
            CXPLAT_SECRET ServerInitial = new CXPLAT_SECRET();
            QUIC_PACKET_KEY ReadKey = null;
            QUIC_PACKET_KEY WriteKey = null;

            ulong Status = CxPlatTlsDeriveInitialSecrets(Salt, CID, ref ClientInitial, ref ServerInitial);
            if (QUIC_FAILED(Status))
            {
                goto Error;
            }

            if (true)
            {
                Status = QuicPacketKeyDerive(
                         QUIC_PACKET_KEY_TYPE.QUIC_PACKET_KEY_INITIAL,
                        HkdfLabels,
                        IsServer ? ServerInitial : ClientInitial,
                        IsServer ? "srv secret" : "cli secret",
                        true,
                       ref WriteKey);

                if (QUIC_FAILED(Status))
                {
                    goto Error;
                }
            }

            if (true)
            {
                Status = QuicPacketKeyDerive(
                         QUIC_PACKET_KEY_TYPE.QUIC_PACKET_KEY_INITIAL,
                        HkdfLabels,
                        IsServer ? ClientInitial : ServerInitial,
                        IsServer ? "cli secret" : "srv secret",
                        true,
                        ref ReadKey);

                if (QUIC_FAILED(Status))
                {
                    goto Error;
                }
            }

            NewWriteKey = WriteKey;
            NewReadKey = ReadKey;
        Error:
            return Status;
        }

        static void QuicPacketKeyFree(QUIC_PACKET_KEY Key)
        {
            if (Key != null)
            {
                Key.PacketKey = null;
                Key.HeaderKey = null;
                if (Key.Type >=  QUIC_PACKET_KEY_TYPE.QUIC_PACKET_KEY_1_RTT)
                {
                    
                }
            }
        }

        static ulong QuicPacketKeyUpdate(QUIC_HKDF_LABELS HkdfLabels, QUIC_PACKET_KEY OldKey, ref QUIC_PACKET_KEY NewKey)
        {
            if (OldKey == null || OldKey.Type != QUIC_PACKET_KEY_TYPE.QUIC_PACKET_KEY_1_RTT)
            {
                return QUIC_STATUS_INVALID_STATE;
            }

            CXPLAT_HASH Hash = null;
            CXPLAT_SECRET NewTrafficSecret = new CXPLAT_SECRET();
            int SecretLength = CxPlatHashLength(OldKey.TrafficSecret.Hash);
            ulong Status = CxPlatHashCreate(OldKey.TrafficSecret.Hash, OldKey.TrafficSecret.Secret, ref Hash);
            if (QUIC_FAILED(Status))
            {
                goto Error;
            }

            QUIC_SSBuffer mSecret = NewTrafficSecret.Secret;
            Status = CxPlatHkdfExpandLabel(Hash, HkdfLabels.KuLabel, SecretLength,ref mSecret);
            if (QUIC_FAILED(Status))
            {
                goto Error;
            }

            NewTrafficSecret.Hash = OldKey.TrafficSecret.Hash;
            NewTrafficSecret.Aead = OldKey.TrafficSecret.Aead;

            Status = QuicPacketKeyDerive(QUIC_PACKET_KEY_TYPE.QUIC_PACKET_KEY_1_RTT,
                    HkdfLabels,
                    NewTrafficSecret,
                    "update traffic secret",
                    false,
                    ref NewKey);
        Error:
            return Status;
        }

        static ulong QuicPacketKeyDeriveOffload(QUIC_HKDF_LABELS HkdfLabels, QUIC_PACKET_KEY PacketKey, string SecretName, CXPLAT_QEO_CONNECTION Offload)
        {
            CXPLAT_SECRET Secret = PacketKey.TrafficSecret;
            int SecretLength = CxPlatHashLength(Secret.Hash);
            int KeyLength = CxPlatKeyLength(Secret.Aead);

            NetLog.Assert(SecretLength >= KeyLength);
            NetLog.Assert(SecretLength >= CXPLAT_IV_LENGTH);
            NetLog.Assert(SecretLength <= CXPLAT_HASH_MAX_SIZE);

            CXPLAT_HASH Hash = null;
            QUIC_SSBuffer Temp = new byte[CXPLAT_HASH_MAX_SIZE];

            ulong Status = CxPlatHashCreate(Secret.Hash, Secret.Secret, ref Hash);
            if (QUIC_FAILED(Status))
            {
                goto Error;
            }

            PacketKey.Iv.Slice(0, CXPLAT_IV_LENGTH).CopyTo(Offload.PayloadIv);
            Status = CxPlatHkdfExpandLabel(Hash, HkdfLabels.KeyLabel, SecretLength, ref Temp);
            if (QUIC_FAILED(Status))
            {
                goto Error;
            }

            Temp.GetSpan().Slice(0, KeyLength).CopyTo(Offload.PayloadKey);
            Status = CxPlatHkdfExpandLabel(Hash, HkdfLabels.HpLabel, SecretLength, ref Temp);
            if (QUIC_FAILED(Status))
            {
                goto Error;
            }

            Temp.GetSpan().Slice(0, KeyLength).CopyTo(Offload.HeaderKey);
        Error:
            Temp.GetSpan().Clear();
            return Status;
        }

    }
}
