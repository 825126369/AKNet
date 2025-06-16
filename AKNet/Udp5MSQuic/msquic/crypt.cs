using AKNet.Common;
using System;
using System.Net.Http.Headers;
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

        static ulong CxPlatHashCreate(CXPLAT_HASH_TYPE HashType, QUIC_SSBuffer Salt, out CXPLAT_HASH NewHash)
        {
            /*在密码学和安全领域，盐（Salt） 和 哈希（Hash） 是两个非常重要的概念，它们通常一起使用来增强密码的安全性。以下是对这两个概念的详细解释：
                1. 盐（Salt）
                盐 是一个随机生成的值，通常用于密码哈希过程中，以防止彩虹表攻击（Rainbow Table Attack）和预计算攻击。
                作用
                增加唯一性：即使两个用户使用相同的密码，由于盐的不同，生成的哈希值也会不同。
                防止彩虹表攻击：彩虹表是一种预计算的哈希值表，用于快速查找密码。通过添加盐，可以使得彩虹表攻击变得不可行。
            */

            NewHash = null;
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

        static ulong CxPlatTlsDeriveInitialSecrets(QUIC_SSBuffer Salt, QUIC_SSBuffer CID, ref CXPLAT_SECRET ClientInitial, ref CXPLAT_SECRET ServerInitial)
        {
            ClientInitial = new CXPLAT_SECRET();
            ClientInitial.Hash = CXPLAT_HASH_TYPE.CXPLAT_HASH_SHA256;
            ClientInitial.Aead = CXPLAT_AEAD_TYPE.CXPLAT_AEAD_AES_128_GCM;
            ClientInitial.Secret.Length = CXPLAT_HASH_SHA256_SIZE;
            CxPlatRandom.Random(ClientInitial.Secret);

            ServerInitial = new CXPLAT_SECRET();
            ServerInitial.Hash = CXPLAT_HASH_TYPE.CXPLAT_HASH_SHA256;
            ServerInitial.Aead = CXPLAT_AEAD_TYPE.CXPLAT_AEAD_AES_128_GCM;
            ServerInitial.Secret.Length = CXPLAT_HASH_SHA256_SIZE;
            CxPlatRandom.Random(ServerInitial.Secret);

            return QUIC_STATUS_SUCCESS;
        }

        static ulong CxPlatHkdfExpandLabel(CXPLAT_HASH Hash, string Label, int KeyLength, QUIC_SSBuffer Output)
        {
            var password = CxPlatHkdfFormatLabel(Label, KeyLength);
            return CxPlatHashCompute(Hash, password, Output);
        }

        static ulong CxPlatHashCompute(CXPLAT_HASH Hash, QUIC_SSBuffer Input, QUIC_SSBuffer Output)
        {
            byte[] password = Input.Buffer;
            byte[] salt = Hash.Salt.Buffer;

            int iterations = 100; // 迭代次数，建议使用较高的值以增加安全性
            using (Rfc2898DeriveBytes pbkdf2 = new Rfc2898DeriveBytes(password, salt, iterations, HashAlgorithmName.SHA256))
            {
                var tt = pbkdf2.GetBytes(Output.Length);
                NetLog.Assert(tt.Length == Output.Length);
                tt.AsSpan().CopyTo(Output.GetSpan());
            }
            return QUIC_STATUS_SUCCESS;
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
        static ulong QuicPacketKeyDerive(QUIC_PACKET_KEY_TYPE KeyType, QUIC_HKDF_LABELS HkdfLabels, CXPLAT_SECRET Secret,
                string SecretName, bool CreateHpKey, ref QUIC_PACKET_KEY NewKey)
        {
            ulong Status = QUIC_STATUS_SUCCESS;
            int SecretLength = CxPlatHashLength(Secret.Hash);
            int KeyLength = CxPlatKeyLength(Secret.Aead);

            NetLog.Assert(SecretLength >= Secret.Secret.Length);
            NetLog.Assert(SecretLength >= KeyLength);
            NetLog.Assert(SecretLength >= CXPLAT_IV_LENGTH);
            NetLog.Assert(SecretLength <= CXPLAT_HASH_MAX_SIZE);

            QUIC_PACKET_KEY Key = new QUIC_PACKET_KEY();
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
            ulong Status = CxPlatHashCreate(OldKey.TrafficSecret.Hash, OldKey.TrafficSecret.Secret, out Hash);
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

            ulong Status = CxPlatHashCreate(Secret.Hash, Secret.Secret, out Hash);
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
