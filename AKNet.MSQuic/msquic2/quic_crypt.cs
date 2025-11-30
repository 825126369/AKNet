/************************************Copyright*****************************************
*        ProjectName:AKNet
*        Web:https://github.com/825126369/AKNet
*        Description:C#游戏网络库
*        Author:许珂
*        StartTime:2024/11/01 00:00:00
*        ModifyTime:2025/11/30 19:43:19
*        Copyright:MIT软件许可证
************************************Copyright*****************************************/
using AKNet.Common;

namespace MSQuic2
{
    //用于标识当前 QUIC 连接使用的加密密钥类型的枚举，它反映了 QUIC 握手和数据传输过程中不同的安全阶段。
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
        public readonly QUIC_BUFFER Key = null;

        public CXPLAT_KEY(CXPLAT_AEAD_TYPE nType, QUIC_BUFFER Key)
        {
            this.nType = nType;
            this.Key = Key;
        }
    }
    
    internal class CXPLAT_HP_KEY
    {
        public CXPLAT_AEAD_TYPE Aead;
        public byte[] Key = null;

        public CXPLAT_HP_KEY(CXPLAT_AEAD_TYPE aead, byte[] key)
        {
            Aead = aead;
            Key = key;
        }
    }

    //这个是用来产生Hash值的，这个类，只是提供原料
    internal class CXPLAT_SECRET
    {
        public CXPLAT_HASH_TYPE Hash;
        public CXPLAT_AEAD_TYPE Aead;
        public readonly QUIC_BUFFER Secret = new byte[MSQuicFunc.CXPLAT_HASH_MAX_SIZE];

        public void CopyFrom(CXPLAT_SECRET other)
        {
            this.Hash = other.Hash;
            this.Aead = other.Aead;
            other.Secret.CopyTo(this.Secret);
            this.Secret.Offset = other.Secret.Offset;
            this.Secret.Length = other.Secret.Length;
        }

        public void Clear()
        {
           
        }
    }

    internal class QUIC_PACKET_KEY
    {
        public const int sizeof_QUIC_PACKET_KEY = 10;
        public QUIC_PACKET_KEY_TYPE Type;
        public CXPLAT_KEY PacketKey;
        public CXPLAT_HP_KEY HeaderKey;
        public QUIC_BUFFER Iv = new byte[MSQuicFunc.CXPLAT_MAX_IV_LENGTH];
        public CXPLAT_SECRET TrafficSecret = null;

        public QUIC_PACKET_KEY(CXPLAT_SECRET TrafficSecret = null)
        {
            this.TrafficSecret = TrafficSecret;
        }
    }

    internal static partial class MSQuicFunc
    {
        public const int CXPLAT_VERSION_SALT_LENGTH = 20;
        public const int CXPLAT_IV_LENGTH = 12;
        public const int CXPLAT_MAX_IV_LENGTH = CXPLAT_IV_LENGTH;

        public const string CXPLAT_HKDF_PREFIX = "tls13";
        public static readonly int CXPLAT_HKDF_PREFIX_LEN = CXPLAT_HKDF_PREFIX.Length;
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
