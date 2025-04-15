using AKNet.Common;
using System;
using System.Text;

namespace AKNet.Udp5Quic.Common
{
    internal class CXPLAT_HASH
    {
        public int SaltLength;
        public byte[] Salt;
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

        static ulong CxPlatHashCreate(CXPLAT_HASH_TYPE HashType, byte[] Salt, int SaltLength, ref CXPLAT_HASH NewHash)
        {
            ulong Status = QUIC_STATUS_SUCCESS;
            CXPLAT_HASH Hash = new CXPLAT_HASH();
            if (Hash == null)
            {
                Status = QUIC_STATUS_OUT_OF_MEMORY;
                goto Exit;
            }

            Hash.SaltLength = SaltLength;
            Array.Copy(Salt, 0, Hash.Salt, 0, SaltLength);
            NewHash = Hash;
        Exit:
            return Status;
        }

        static ulong CxPlatHashCompute(CXPLAT_HASH Hash, byte[] Input, int InputLength, int OutputLength, byte[] Output)
        {
            return QUIC_STATUS_SUCCESS;
        }

        static ulong CxPlatTlsDeriveInitialSecrets(byte[] Salt, byte[] CID, int CIDLength, ref CXPLAT_SECRET ClientInitial, ref CXPLAT_SECRET ServerInitial)
        {
            ulong Status;
            CXPLAT_HASH InitialHash = null;
            CXPLAT_HASH DerivedHash = null;
            byte[] InitialSecret = new byte[CXPLAT_HASH_SHA256_SIZE];

            Status = CxPlatHashCreate(CXPLAT_HASH_TYPE.CXPLAT_HASH_SHA256, Salt, CXPLAT_VERSION_SALT_LENGTH, ref InitialHash);
            if (QUIC_FAILED(Status))
            {
                goto Error;
            }

            Status = CxPlatHashCompute(InitialHash, CID, CIDLength, CXPLAT_HASH_SHA256_SIZE, InitialSecret);
            if (QUIC_FAILED(Status))
            {
                goto Error;
            }

            Status = CxPlatHashCreate(CXPLAT_HASH_TYPE.CXPLAT_HASH_SHA256, InitialSecret, CXPLAT_HASH_SHA256_SIZE, ref DerivedHash);
            if (QUIC_FAILED(Status))
            {
                goto Error;
            }

            ClientInitial.Hash = CXPLAT_HASH_TYPE.CXPLAT_HASH_SHA256;
            ClientInitial.Aead = CXPLAT_AEAD_TYPE.CXPLAT_AEAD_AES_128_GCM;
            Status = CxPlatHkdfExpandLabel(DerivedHash, "client in", InitialSecret.Length, CXPLAT_HASH_SHA256_SIZE, ClientInitial.Secret);
            if (QUIC_FAILED(Status))
            {
                goto Error;
            }

            ServerInitial.Hash = CXPLAT_HASH_TYPE.CXPLAT_HASH_SHA256;
            ServerInitial.Aead = CXPLAT_AEAD_TYPE.CXPLAT_AEAD_AES_128_GCM;
            Status = CxPlatHkdfExpandLabel(
                    DerivedHash,
                    "server in",
                    InitialSecret.Length,
                    CXPLAT_HASH_SHA256_SIZE,
                    ServerInitial.Secret);

            if (QUIC_FAILED(Status))
            {
                goto Error;
            }

        Error:
            return Status;
        }

        static ulong CxPlatHkdfExpandLabel(CXPLAT_HASH Hash, string Label, int KeyLength, int OutputLength, byte[] Output)
        {
            byte[] LabelBuffer = new byte[64];
            int LabelLength = LabelBuffer.Length;

            NetLog.Assert(Label.Length <= 23);
            CxPlatHkdfFormatLabel(Label, KeyLength, LabelBuffer, ref LabelLength);

            return
                CxPlatHashCompute(
                    Hash,
                    LabelBuffer,
                    LabelLength,
                    OutputLength,
                    Output);
        }

        static void CxPlatHkdfFormatLabel(string Label, int HashLength, byte[] Data, ref int DataLength)
        {
            NetLog.Assert(Label.Length <= byte.MaxValue - CXPLAT_HKDF_PREFIX_LEN);
            int LabelLength = Label.Length;

            Data[0] = (byte)(HashLength >> 8);
            Data[1] = (byte)(HashLength & 0xff);
            Data[2] = (byte)(CXPLAT_HKDF_PREFIX_LEN + LabelLength);
            Encoding.ASCII.GetBytes(CXPLAT_HKDF_PREFIX).CopyTo(Data, 3);

            Data[3 + CXPLAT_HKDF_PREFIX_LEN + LabelLength] = 0;
            DataLength = 3 + CXPLAT_HKDF_PREFIX_LEN + LabelLength + 1;
            Data[DataLength] = 0x1;
            DataLength += 1;
        }

        static ulong QuicPacketKeyDerive(QUIC_PACKET_KEY_TYPE KeyType, QUIC_HKDF_LABELS HkdfLabels, CXPLAT_SECRET Secret,
                string SecretName, bool CreateHpKey, ref QUIC_PACKET_KEY NewKey)
        {
            int SecretLength = CxPlatHashLength(Secret.Hash);
            int KeyLength = CxPlatKeyLength(Secret.Aead);

            NetLog.Assert(SecretLength >= KeyLength);
            NetLog.Assert(SecretLength >= CXPLAT_IV_LENGTH);
            NetLog.Assert(SecretLength <= CXPLAT_HASH_MAX_SIZE);

            QUIC_PACKET_KEY Key = new QUIC_PACKET_KEY();
            if (Key == null)
            {
                return QUIC_STATUS_OUT_OF_MEMORY;
            }
            Key.Type = KeyType;

            CXPLAT_HASH Hash = null;
            byte[] Temp = new byte[CXPLAT_HASH_MAX_SIZE];

            ulong Status = CxPlatHashCreate(Secret.Hash, Secret.Secret, SecretLength, ref Hash);
            if (QUIC_FAILED(Status))
            {
                goto Error;
            }

            Status = CxPlatHkdfExpandLabel(
                    Hash,
                    HkdfLabels.IvLabel,
                    CXPLAT_IV_LENGTH,
                    SecretLength,
                    Temp);

            if (QUIC_FAILED(Status))
            {
                goto Error;
            }

            Temp.CopyTo(Key.Iv, CXPLAT_IV_LENGTH);
            Status = CxPlatHkdfExpandLabel(Hash, HkdfLabels.KeyLabel, KeyLength, SecretLength, Temp);
            if (QUIC_FAILED(Status))
            {
                goto Error;
            }

            Status = CxPlatKeyCreate(Secret.Aead, Temp, ref Key.PacketKey);
            if (QUIC_FAILED(Status))
            {
                goto Error;
            }

            if (CreateHpKey)
            {
                Status = CxPlatHkdfExpandLabel(Hash, HkdfLabels.HpLabel, KeyLength, SecretLength, Temp);
                if (QUIC_FAILED(Status))
                {
                    goto Error;
                }

                Status = CxPlatHpKeyCreate(Secret.Aead, Temp, ref Key.HeaderKey);
                if (QUIC_FAILED(Status))
                {
                    goto Error;
                }
            }

            if (KeyType == QUIC_PACKET_KEY_TYPE.QUIC_PACKET_KEY_1_RTT)
            {
                Key.TrafficSecret = Secret;
            }

            NewKey = Key;
            Key = null;
        Error:
            return Status;
        }

        static ulong QuicPacketKeyCreateInitial(bool IsServer, QUIC_HKDF_LABELS HkdfLabels, byte[] Salt, int CIDLength, byte[] CID,
            ref QUIC_PACKET_KEY NewReadKey, ref QUIC_PACKET_KEY NewWriteKey)
        {
            ulong Status;
            CXPLAT_SECRET ClientInitial, ServerInitial;
            QUIC_PACKET_KEY ReadKey = null;
            QUIC_PACKET_KEY WriteKey = null;

            Status = CxPlatTlsDeriveInitialSecrets(
                    Salt,
                    CID,
                    CIDLength,
                    ref ClientInitial,
                    ref ServerInitial);
            if (QUIC_FAILED(Status))
            {
                goto Error;
            }

            if (NewWriteKey != null)
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

            if (NewReadKey != null)
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

            if (NewWriteKey != null)
            {
                NewWriteKey = WriteKey;
                WriteKey = null;
            }

            if (NewReadKey != null)
            {
                NewReadKey = ReadKey;
                ReadKey = null;
            }

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
            CXPLAT_SECRET NewTrafficSecret;
            int SecretLength = CxPlatHashLength(OldKey.TrafficSecret.Hash);
            ulong Status = CxPlatHashCreate(OldKey.TrafficSecret.Hash, OldKey.TrafficSecret.Secret, SecretLength, ref Hash);
            if (QUIC_FAILED(Status))
            {
                goto Error;
            }

            Status = CxPlatHkdfExpandLabel(Hash, HkdfLabels.KuLabel, SecretLength, SecretLength, NewTrafficSecret.Secret);
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
    }
}
