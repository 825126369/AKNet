using AKNet.Common;
using System.Security.Cryptography;

namespace AKNet.Udp1MSQuic.Common
{
    internal class CXPLAT_HASH
    {
        public HMAC mHashAlgorithm = null;
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

        static int CxPlatTlsDeriveInitialSecrets(QUIC_SSBuffer Salt, QUIC_SSBuffer CID, ref CXPLAT_SECRET ClientInitial, ref CXPLAT_SECRET ServerInitial)
        {
            int Status;
            CXPLAT_HASH InitialHash = null;
            CXPLAT_HASH DerivedHash = null;
            byte[] InitialSecret = new byte[CXPLAT_HASH_SHA256_SIZE];

            Status = CxPlatHashCreate(CXPLAT_HASH_TYPE.CXPLAT_HASH_SHA256, Salt, out InitialHash);
            if (QUIC_FAILED(Status))
            {
                goto Error;
            }

            Status = CxPlatHashCompute(InitialHash, CID, InitialSecret);
            if (QUIC_FAILED(Status))
            {
                goto Error;
            }

            Status = CxPlatHashCreate(CXPLAT_HASH_TYPE.CXPLAT_HASH_SHA256, InitialSecret, out DerivedHash);
            if (QUIC_FAILED(Status))
            {
                goto Error;
            }

            ClientInitial.Hash = CXPLAT_HASH_TYPE.CXPLAT_HASH_SHA256;
            ClientInitial.Aead = CXPLAT_AEAD_TYPE.CXPLAT_AEAD_AES_128_GCM;
            Status = CxPlatHkdfExpandLabel(DerivedHash, "client in", CXPLAT_HASH_SHA256_SIZE, ClientInitial.Secret);
            if (QUIC_FAILED(Status))
            {
                goto Error;
            }

            ServerInitial.Hash =  CXPLAT_HASH_TYPE.CXPLAT_HASH_SHA256;
            ServerInitial.Aead =  CXPLAT_AEAD_TYPE.CXPLAT_AEAD_AES_128_GCM;
            Status = CxPlatHkdfExpandLabel(DerivedHash, "server in", CXPLAT_HASH_SHA256_SIZE, ServerInitial.Secret);
            if (QUIC_FAILED(Status))
            {
                goto Error;
            }
        Error:
            return Status;
        }

        static int CxPlatHkdfExpandLabel(CXPLAT_HASH Hash, string Label, int KeyLength, QUIC_SSBuffer Output)
        {
            var password = CxPlatHkdfFormatLabel(Label, KeyLength);
            return CxPlatHashCompute(Hash, password, Output);
        }

        //这里KeyLength 和 Label 都是已知的常量，这个返回值 可以看作是密码
        static byte[] CxPlatHkdfFormatLabel(string Label, int HashLength)
        {
            NetLog.Assert(Label.Length <= byte.MaxValue - CXPLAT_HKDF_PREFIX_LEN);
            int Data_Length = 3 + CXPLAT_HKDF_PREFIX_LEN + Label.Length + 1 + 1; // 3字节长度 + 前缀长度 + 标签长度 + 1字节的0 + 1字节的版本
            byte[] Data = new byte[Data_Length];
            
            int LabelLength = Label.Length;
            int nOffset = 0;
            Data[nOffset++] = (byte)(HashLength >> 8);
            Data[nOffset++] = (byte)(HashLength & 0xff);
            Data[nOffset++] = (byte)(CXPLAT_HKDF_PREFIX_LEN + LabelLength);
            EndianBitConverter.SetBytes(Data, nOffset, CXPLAT_HKDF_PREFIX);
            nOffset += CXPLAT_HKDF_PREFIX_LEN;
            EndianBitConverter.SetBytes(Data, nOffset, Label);
            nOffset += LabelLength;
            Data[nOffset++] = 0;
            Data[nOffset] = 0x1;
            NetLog.Assert(nOffset + 1 == Data_Length);
            return Data;
        }

        //这里派生，但没有相关的API，就不派生了
        static int QuicPacketKeyDerive(QUIC_PACKET_KEY_TYPE KeyType, QUIC_HKDF_LABELS HkdfLabels, CXPLAT_SECRET Secret,
                string SecretName, bool CreateHpKey, ref QUIC_PACKET_KEY NewKey)
        {
            int Status = QUIC_STATUS_SUCCESS;
            int SecretLength = CxPlatHashLength(Secret.Hash);
            int KeyLength = CxPlatKeyLength(Secret.Aead);
            
            NetLog.Assert(SecretLength >= KeyLength);
            NetLog.Assert(SecretLength >= CXPLAT_IV_LENGTH);
            NetLog.Assert(SecretLength <= CXPLAT_HASH_MAX_SIZE);

            CXPLAT_SECRET Key_CXPLAT_SECRET = (KeyType == QUIC_PACKET_KEY_TYPE.QUIC_PACKET_KEY_1_RTT ? new CXPLAT_SECRET() : null);
            QUIC_PACKET_KEY Key = new QUIC_PACKET_KEY(Key_CXPLAT_SECRET);
            Key.Type = KeyType;

            CXPLAT_HASH Hash = null;
            CxPlatHashCreate(Secret.Hash, Secret.Secret, out Hash);

            byte[] Temp = new byte[CXPLAT_HASH_MAX_SIZE];
            QUIC_SSBuffer Temp2 = new QUIC_SSBuffer(Temp, 0, SecretLength);

            CxPlatHkdfExpandLabel(Hash, HkdfLabels.IvLabel, CXPLAT_IV_LENGTH, Temp2);
            Temp2.Slice(0, CXPLAT_IV_LENGTH).CopyTo(Key.Iv);

            CxPlatHkdfExpandLabel(Hash, HkdfLabels.KeyLabel, KeyLength, Temp2);
            CxPlatKeyCreate(Secret.Aead, Temp2, ref Key.PacketKey);
            if (CreateHpKey)
            {
                CxPlatHkdfExpandLabel(Hash, HkdfLabels.HpLabel, KeyLength, Temp2);
                CxPlatHpKeyCreate(Secret.Aead, Temp, ref Key.HeaderKey);
            }

            if (KeyType == QUIC_PACKET_KEY_TYPE.QUIC_PACKET_KEY_1_RTT)
            {
                Key.TrafficSecret.CopyFrom(Secret);
            }

            NewKey = Key;
        Exit:
            return Status;
        }

        static int QuicPacketKeyCreateInitial(bool IsServer, QUIC_HKDF_LABELS HkdfLabels, QUIC_SSBuffer Salt, QUIC_SSBuffer CID,
            ref QUIC_PACKET_KEY NewReadKey, ref QUIC_PACKET_KEY NewWriteKey)
        {
            CXPLAT_SECRET ClientInitial = new CXPLAT_SECRET();
            CXPLAT_SECRET ServerInitial = new CXPLAT_SECRET();
            QUIC_PACKET_KEY ReadKey = null;
            QUIC_PACKET_KEY WriteKey = null;

            int Status = CxPlatTlsDeriveInitialSecrets(Salt, CID, ref ClientInitial, ref ServerInitial);
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
                Key.TrafficSecret = null;
            }
        }

        static int QuicPacketKeyUpdate(QUIC_HKDF_LABELS HkdfLabels, QUIC_PACKET_KEY OldKey, ref QUIC_PACKET_KEY NewKey)
        {
            if (OldKey == null || OldKey.Type != QUIC_PACKET_KEY_TYPE.QUIC_PACKET_KEY_1_RTT)
            {
                return QUIC_STATUS_INVALID_STATE;
            }

            CXPLAT_HASH Hash = null;
            CXPLAT_SECRET NewTrafficSecret = new CXPLAT_SECRET();
            int SecretLength = CxPlatHashLength(OldKey.TrafficSecret.Hash);
            int Status = CxPlatHashCreate(OldKey.TrafficSecret.Hash, OldKey.TrafficSecret.Secret, out Hash);
            if (QUIC_FAILED(Status))
            {
                goto Error;
            }

            QUIC_SSBuffer mSecret = NewTrafficSecret.Secret;
            Status = CxPlatHkdfExpandLabel(Hash, HkdfLabels.KuLabel, SecretLength, mSecret);
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

        static int QuicPacketKeyDeriveOffload(QUIC_HKDF_LABELS HkdfLabels, QUIC_PACKET_KEY PacketKey, string SecretName, CXPLAT_QEO_CONNECTION Offload)
        {
            CXPLAT_SECRET Secret = PacketKey.TrafficSecret;
            int SecretLength = CxPlatHashLength(Secret.Hash);
            int KeyLength = CxPlatKeyLength(Secret.Aead);

            NetLog.Assert(SecretLength >= KeyLength);
            NetLog.Assert(SecretLength >= CXPLAT_IV_LENGTH);
            NetLog.Assert(SecretLength <= CXPLAT_HASH_MAX_SIZE);

            CXPLAT_HASH Hash = null;
            QUIC_SSBuffer Temp = new byte[CXPLAT_HASH_MAX_SIZE];

            int Status = CxPlatHashCreate(Secret.Hash, Secret.Secret, out Hash);
            if (QUIC_FAILED(Status))
            {
                goto Error;
            }

            PacketKey.Iv.Slice(0, CXPLAT_IV_LENGTH).CopyTo(Offload.PayloadIv);
            Status = CxPlatHkdfExpandLabel(Hash, HkdfLabels.KeyLabel, SecretLength, Temp);
            if (QUIC_FAILED(Status))
            {
                goto Error;
            }

            Temp.GetSpan().Slice(0, KeyLength).CopyTo(Offload.PayloadKey);
            Status = CxPlatHkdfExpandLabel(Hash, HkdfLabels.HpLabel, SecretLength, Temp);
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
