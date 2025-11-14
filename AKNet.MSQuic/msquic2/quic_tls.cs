/************************************Copyright*****************************************
*        ProjectName:AKNet
*        Web:https://github.com/825126369/AKNet
*        Description:C#游戏网络库
*        Author:许珂
*        ModifyTime:2025/11/14 8:44:35
*        Copyright:MIT软件许可证
************************************Copyright*****************************************/
using AKNet.Common;
using System;

namespace MSQuic2
{
    internal enum CXPLAT_TLS_EARLY_DATA_STATE
    {
        CXPLAT_TLS_EARLY_DATA_UNKNOWN,
        CXPLAT_TLS_EARLY_DATA_UNSUPPORTED,
        CXPLAT_TLS_EARLY_DATA_REJECTED,
        CXPLAT_TLS_EARLY_DATA_ACCEPTED
    }

    internal enum CXPLAT_TLS_CREDENTIAL_FLAGS
    {
        CXPLAT_TLS_CREDENTIAL_FLAG_NONE = 0x0000,
        CXPLAT_TLS_CREDENTIAL_FLAG_DISABLE_RESUMPTION = 0x0001,   // Server only
    }

    internal enum CXPLAT_TLS_RESULT_FLAGS
    {
        CXPLAT_TLS_RESULT_CONTINUE = 0x0001, // Needs immediate call again. (Used internally to schannel)
        CXPLAT_TLS_RESULT_DATA = 0x0002, // Data ready to be sent.
        CXPLAT_TLS_RESULT_READ_KEY_UPDATED = 0x0004, // ReadKey variable has been updated.
        CXPLAT_TLS_RESULT_WRITE_KEY_UPDATED = 0x0008, // WriteKey variable has been updated.
        CXPLAT_TLS_RESULT_EARLY_DATA_ACCEPT = 0x0010, // The server accepted the early (0-RTT) data.
        CXPLAT_TLS_RESULT_EARLY_DATA_REJECT = 0x0020, // The server rejected the early (0-RTT) data.
        CXPLAT_TLS_RESULT_HANDSHAKE_COMPLETE = 0x0040, // Handshake complete.
        CXPLAT_TLS_RESULT_ERROR = 0x8000  // An error occured.

    }
    
    internal class CXPLAT_TLS_PROCESS_STATE
    {
        public bool HandshakeComplete;//表示 TLS 握手是否已经完成。
        public bool SessionResumed;//表示当前 TLS 会话是否是从之前的会话中恢复的
        public CXPLAT_TLS_EARLY_DATA_STATE EarlyDataState;//表示 0-RTT 数据的支持状态（例如 NotAttempted, Accepted, Rejected 等）。

        public readonly QUIC_PACKET_KEY[] ReadKeys = new QUIC_PACKET_KEY[(int)QUIC_PACKET_KEY_TYPE.QUIC_PACKET_KEY_COUNT];
        public readonly QUIC_PACKET_KEY[] WriteKeys = new QUIC_PACKET_KEY[(int)QUIC_PACKET_KEY_TYPE.QUIC_PACKET_KEY_COUNT];
        public QUIC_PACKET_KEY_TYPE ReadKey;//当前用于解密,收到的数据包的密钥类型（如 Initial, 0-RTT, Handshake, 1-RTT 等）。
        public QUIC_PACKET_KEY_TYPE WriteKey;//当前用于加密，收到的数据包的密钥类型（如 Initial, 0-RTT, Handshake, 1-RTT 等）。
        public CXPLAT_TLS_ALERT_CODES AlertCode;//握手失败相关字段

        public int BufferLength;//当前 Buffer 中实际使用的字节数。
        public int BufferAllocLength;//当前 Buffer 已分配的总长度（单位是字节）。
        public int BufferTotalLength;//所有曾经写入到 Buffer 的总字节数（可能超过当前已分配长度，触发重新分配）。
        public int BufferOffsetHandshake;//握手阶段数据在 Buffer 中的起始偏移量。若为 0，表示尚未设置。
        public int BufferOffset1Rtt;//1-RTT 阶段数据在 Buffer 中的起始偏移量。若为 0，表示尚未设置。
        public byte[] Buffer;//存储待发送的 TLS 数据

        public QUIC_ALPN_BUFFER ClientAlpnList;//客户端提供的 ALPN 列表（TLS 格式）
        public byte[] SmallAlpnBuffer = new byte[MSQuicFunc.TLS_SMALL_ALPN_BUFFER_SIZE];//小型缓存，用于存放协商后的 ALPN 协议名称。如果 ALPN 名称较短，就存储在这里。
        public QUIC_ALPN_BUFFER NegotiatedAlpn;//指向最终协商成功的 ALPN 协议名。如果 ALPN 较长，则指向动态分配的内存；否则指向 SmallAlpnBuffer。
    }

    internal class CXPLAT_TLS_CONFIG
    {
        public bool IsServer;
        public QUIC_CONNECTION Connection;
        public QUIC_HKDF_LABELS HkdfLabels;
        public CXPLAT_SEC_CONFIG SecConfig;
        public QUIC_ALPN_BUFFER AlpnBuffer;
        public uint TPType;
        public string ServerName;
        public QUIC_BUFFER ResumptionTicketBuffer;
        public QUIC_BUFFER LocalTPBuffer;
        public QUIC_TLS_SECRETS TlsSecrets;
    }

    internal enum CXPLAT_TLS_DATA_TYPE
    {
        CXPLAT_TLS_CRYPTO_DATA,
        CXPLAT_TLS_TICKET_DATA

    }
    
    internal enum CXPLAT_TLS_ALERT_CODES
    {
        CXPLAT_TLS_ALERT_CODE_HANDSHAKE_FAILURE = 40,
        CXPLAT_TLS_ALERT_CODE_BAD_CERTIFICATE = 42,
        CXPLAT_TLS_ALERT_CODE_CERTIFICATE_EXPIRED = 45,
        CXPLAT_TLS_ALERT_CODE_UNKNOWN_CA = 48,
        CXPLAT_TLS_ALERT_CODE_INTERNAL_ERROR = 80,
        CXPLAT_TLS_ALERT_CODE_USER_CANCELED = 90,
        CXPLAT_TLS_ALERT_CODE_REQUIRED_CERTIFICATE = 116,
        CXPLAT_TLS_ALERT_CODE_NO_APPLICATION_PROTOCOL = 120,
    }
    
    internal static partial class MSQuicFunc
    {
        public const int TLS_SMALL_ALPN_BUFFER_SIZE = 16;
        public const uint TLS_EXTENSION_TYPE_APPLICATION_LAYER_PROTOCOL_NEGOTIATION = 0x0010;  // Host Byte Order
        public const uint TLS_EXTENSION_TYPE_QUIC_TRANSPORT_PARAMETERS_DRAFT = 0xffa5; // Host Byte Order
        public const uint TLS_EXTENSION_TYPE_QUIC_TRANSPORT_PARAMETERS = 0x0039;  // Host Byte Order

        public const uint CXPLAT_TLS_RESULT_CONTINUE = 0x0001; // Needs immediate call again. (Used internally to schannel)
        public const uint CXPLAT_TLS_RESULT_DATA = 0x0002; // Data ready to be sent.
        public const uint CXPLAT_TLS_RESULT_READ_KEY_UPDATED = 0x0004; // ReadKey variable has been updated.
        public const uint CXPLAT_TLS_RESULT_WRITE_KEY_UPDATED = 0x0008; // WriteKey variable has been updated.
        public const uint CXPLAT_TLS_RESULT_EARLY_DATA_ACCEPT = 0x0010; // The server accepted the early (0-RTT) data.
        public const uint CXPLAT_TLS_RESULT_EARLY_DATA_REJECT = 0x0020; // The server rejected the early (0-RTT) data.
        public const uint CXPLAT_TLS_RESULT_HANDSHAKE_COMPLETE = 0x0040; // Handshake complete.
        public const uint CXPLAT_TLS_RESULT_ERROR = 0x8000;  // An error occured.

        static QUIC_SSBuffer CxPlatTlsAlpnFindInList(QUIC_SSBuffer AlpnList, QUIC_SSBuffer FindAlpn)
        {
            return CxPlatTlsAlpnFindInList(AlpnList.GetSpan(), FindAlpn.GetSpan());
        }

        static QUIC_SSBuffer CxPlatTlsAlpnFindInList(ReadOnlySpan<byte> AlpnList, ReadOnlySpan<byte> FindAlpn)
        {
            while (AlpnList.Length > 0)
            {
                NetLog.Assert(AlpnList[0] + 1 <= AlpnList.Length);
                if (AlpnList[0] == FindAlpn.Length && orBufferEqual(AlpnList.Slice(1), FindAlpn))
                {
                    return AlpnList.ToArray();
                }
                AlpnList = AlpnList.Slice(AlpnList[0] + 1);
            }
            return QUIC_SSBuffer.Empty;
        }

    }
}
