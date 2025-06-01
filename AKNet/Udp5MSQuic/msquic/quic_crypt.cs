using AKNet.Common;

namespace AKNet.Udp5MSQuic.Common
{
    internal enum QUIC_PACKET_KEY_TYPE
    {
        QUIC_PACKET_KEY_INITIAL,
        QUIC_PACKET_KEY_0_RTT,
        QUIC_PACKET_KEY_HANDSHAKE,
        QUIC_PACKET_KEY_1_RTT,
        QUIC_PACKET_KEY_1_RTT_OLD,
        QUIC_PACKET_KEY_1_RTT_NEW,
        QUIC_PACKET_KEY_COUNT
    }



    internal enum CXPLAT_HASH_TYPE
    {
        CXPLAT_HASH_SHA256 = 0,    // 32 bytes
        CXPLAT_HASH_SHA384 = 1,    // 48 bytes
        CXPLAT_HASH_SHA512 = 2     // 64 bytes
    }

    internal enum CXPLAT_AEAD_TYPE
    {
        CXPLAT_AEAD_AES_128_GCM = 0,    // 16 byte key
        CXPLAT_AEAD_AES_256_GCM = 1,    // 32 byte key
        CXPLAT_AEAD_CHACHA20_POLY1305 = 2     // 32 byte key
    }

    internal enum CXPLAT_AEAD_TYPE_SIZE
    {
        CXPLAT_AEAD_AES_128_GCM_SIZE = 16,
        CXPLAT_AEAD_AES_256_GCM_SIZE = 32,
        CXPLAT_AEAD_CHACHA20_POLY1305_SIZE = 32
    }

    internal class CXPLAT_KEY
    {
        public readonly CXPLAT_AEAD_TYPE nType;
        public readonly byte[] Key;

        public CXPLAT_KEY(CXPLAT_AEAD_TYPE nType, CXPLAT_AEAD_TYPE_SIZE nKeyLength)
        {
            this.nType = nType;
            this.Key = new byte[(int)nKeyLength];
            CxPlatRandom.Random(Key);
        }
    }

    internal class CXPLAT_HP_KEY
    {
        
    }

    internal class CXPLAT_SECRET
    {
        public CXPLAT_HASH_TYPE Hash;
        public CXPLAT_AEAD_TYPE Aead;
        public byte[] Secret = new byte[MSQuicFunc.CXPLAT_HASH_MAX_SIZE];
    }

    internal class QUIC_PACKET_KEY
    {
        public QUIC_PACKET_KEY_TYPE Type;
        public CXPLAT_KEY PacketKey;
        public CXPLAT_HP_KEY HeaderKey;
        public byte[] Iv = new byte[MSQuicFunc.CXPLAT_IV_LENGTH];
        public CXPLAT_SECRET TrafficSecret;
    }

    internal static partial class MSQuicFunc
    {
        public const int CXPLAT_VERSION_SALT_LENGTH = 20;
        public const int CXPLAT_IV_LENGTH = 12;
        public const int CXPLAT_MAX_IV_LENGTH = CXPLAT_IV_LENGTH;

        public const string CXPLAT_HKDF_PREFIX = "tls13 ";
        public static readonly int CXPLAT_HKDF_PREFIX_LEN = CXPLAT_HKDF_PREFIX.Length - 1;
        public const int CXPLAT_ENCRYPTION_OVERHEAD = 16;

        static int CxPlatKeyLength(CXPLAT_AEAD_TYPE Type)
        {
            switch (Type)
            {
                case CXPLAT_AEAD_TYPE.CXPLAT_AEAD_AES_128_GCM: return 16;
                case CXPLAT_AEAD_TYPE.CXPLAT_AEAD_AES_256_GCM:
                case CXPLAT_AEAD_TYPE.CXPLAT_AEAD_CHACHA20_POLY1305: return 32;
                default:
                    NetLog.Assert(false);
                    return 0;
            }
        }
    }
}
