using System;
using System.Runtime.InteropServices;

namespace AKNet.MSQuicWrapper;

public static unsafe partial class MSQuicWrapperFunc
{
    [DllImport("C:\Users\14261\.nuget\packages\microsoft.native.quic.msquic.openssl\2.4.10\build\native\lib\x64", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
    [return: NativeTypeName("HRESULT")]
    public static extern IntPtr MsQuicOpenVersion([NativeTypeName("uint32_t")] uint Version, [NativeTypeName("const void **")] void** QuicApi);

    [DllImport("C:\Users\14261\.nuget\packages\microsoft.native.quic.msquic.openssl\2.4.10\build\native\lib\x64", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
    public static extern void MsQuicClose([NativeTypeName("const void *")] void* QuicApi);

    [return: NativeTypeName("HRESULT")]
    public static IntPtr MsQuicOpen2([NativeTypeName("const QUIC_API_TABLE **")] QUIC_API_TABLE** QuicApi)
    {
        return MsQuicOpenVersion(2, unchecked((void**)(QuicApi)));
    }

    [NativeTypeName("#define QUIC_UINT62_MAX ((1ULL << 62U) - 1)")]
    public const ulong QUIC_UINT62_MAX = unchecked((1UL << 62U) - 1);

    [NativeTypeName("#define QUIC_MAX_ALPN_LENGTH 255")]
    public const int QUIC_MAX_ALPN_LENGTH = 255;

    [NativeTypeName("#define QUIC_MAX_SNI_LENGTH 65535")]
    public const int QUIC_MAX_SNI_LENGTH = 65535;

    [NativeTypeName("#define QUIC_MAX_RESUMPTION_APP_DATA_LENGTH 1000")]
    public const int QUIC_MAX_RESUMPTION_APP_DATA_LENGTH = 1000;

    [NativeTypeName("#define QUIC_STATELESS_RESET_KEY_LENGTH 32")]
    public const int QUIC_STATELESS_RESET_KEY_LENGTH = 32;

    [NativeTypeName("#define QUIC_EXECUTION_CONFIG_MIN_SIZE (uint32_t)FIELD_OFFSET(QUIC_EXECUTION_CONFIG, ProcessorList)")]
    public static uint QUIC_EXECUTION_CONFIG_MIN_SIZE => unchecked((uint)((nint)(Marshal.OffsetOf<QUIC_EXECUTION_CONFIG>("ProcessorList"))));

    [NativeTypeName("#define QUIC_MAX_TICKET_KEY_COUNT 16")]
    public const int QUIC_MAX_TICKET_KEY_COUNT = 16;

    [NativeTypeName("#define QUIC_STATISTICS_V2_SIZE_1 QUIC_STRUCT_SIZE_THRU_FIELD(QUIC_STATISTICS_V2, KeyUpdateCount)")]
    public static ulong QUIC_STATISTICS_V2_SIZE_1 => unchecked(((nint)(Marshal.OffsetOf<QUIC_STATISTICS_V2>("KeyUpdateCount"))) + 4);

    [NativeTypeName("#define QUIC_STATISTICS_V2_SIZE_2 QUIC_STRUCT_SIZE_THRU_FIELD(QUIC_STATISTICS_V2, DestCidUpdateCount)")]
    public static ulong QUIC_STATISTICS_V2_SIZE_2 => unchecked(((nint)(Marshal.OffsetOf<QUIC_STATISTICS_V2>("DestCidUpdateCount"))) + 4);

    [NativeTypeName("#define QUIC_STATISTICS_V2_SIZE_3 QUIC_STRUCT_SIZE_THRU_FIELD(QUIC_STATISTICS_V2, SendEcnCongestionCount)")]
    public static ulong QUIC_STATISTICS_V2_SIZE_3 => unchecked(((nint)(Marshal.OffsetOf<QUIC_STATISTICS_V2>("SendEcnCongestionCount"))) + 4);

    [NativeTypeName("#define QUIC_TLS_SECRETS_MAX_SECRET_LEN 64")]
    public const int QUIC_TLS_SECRETS_MAX_SECRET_LEN = 64;

    [NativeTypeName("#define QUIC_PARAM_PREFIX_GLOBAL 0x01000000")]
    public const int QUIC_PARAM_PREFIX_GLOBAL = 0x01000000;

    [NativeTypeName("#define QUIC_PARAM_PREFIX_REGISTRATION 0x02000000")]
    public const int QUIC_PARAM_PREFIX_REGISTRATION = 0x02000000;

    [NativeTypeName("#define QUIC_PARAM_PREFIX_CONFIGURATION 0x03000000")]
    public const int QUIC_PARAM_PREFIX_CONFIGURATION = 0x03000000;

    [NativeTypeName("#define QUIC_PARAM_PREFIX_LISTENER 0x04000000")]
    public const int QUIC_PARAM_PREFIX_LISTENER = 0x04000000;

    [NativeTypeName("#define QUIC_PARAM_PREFIX_CONNECTION 0x05000000")]
    public const int QUIC_PARAM_PREFIX_CONNECTION = 0x05000000;

    [NativeTypeName("#define QUIC_PARAM_PREFIX_TLS 0x06000000")]
    public const int QUIC_PARAM_PREFIX_TLS = 0x06000000;

    [NativeTypeName("#define QUIC_PARAM_PREFIX_TLS_SCHANNEL 0x07000000")]
    public const int QUIC_PARAM_PREFIX_TLS_SCHANNEL = 0x07000000;

    [NativeTypeName("#define QUIC_PARAM_PREFIX_STREAM 0x08000000")]
    public const int QUIC_PARAM_PREFIX_STREAM = 0x08000000;

    [NativeTypeName("#define QUIC_PARAM_HIGH_PRIORITY 0x40000000")]
    public const int QUIC_PARAM_HIGH_PRIORITY = 0x40000000;

    [NativeTypeName("#define QUIC_PARAM_GLOBAL_RETRY_MEMORY_PERCENT 0x01000000")]
    public const int QUIC_PARAM_GLOBAL_RETRY_MEMORY_PERCENT = 0x01000000;

    [NativeTypeName("#define QUIC_PARAM_GLOBAL_SUPPORTED_VERSIONS 0x01000001")]
    public const int QUIC_PARAM_GLOBAL_SUPPORTED_VERSIONS = 0x01000001;

    [NativeTypeName("#define QUIC_PARAM_GLOBAL_LOAD_BALACING_MODE 0x01000002")]
    public const int QUIC_PARAM_GLOBAL_LOAD_BALACING_MODE = 0x01000002;

    [NativeTypeName("#define QUIC_PARAM_GLOBAL_PERF_COUNTERS 0x01000003")]
    public const int QUIC_PARAM_GLOBAL_PERF_COUNTERS = 0x01000003;

    [NativeTypeName("#define QUIC_PARAM_GLOBAL_LIBRARY_VERSION 0x01000004")]
    public const int QUIC_PARAM_GLOBAL_LIBRARY_VERSION = 0x01000004;

    [NativeTypeName("#define QUIC_PARAM_GLOBAL_SETTINGS 0x01000005")]
    public const int QUIC_PARAM_GLOBAL_SETTINGS = 0x01000005;

    [NativeTypeName("#define QUIC_PARAM_GLOBAL_GLOBAL_SETTINGS 0x01000006")]
    public const int QUIC_PARAM_GLOBAL_GLOBAL_SETTINGS = 0x01000006;

    [NativeTypeName("#define QUIC_PARAM_GLOBAL_LIBRARY_GIT_HASH 0x01000008")]
    public const int QUIC_PARAM_GLOBAL_LIBRARY_GIT_HASH = 0x01000008;

    [NativeTypeName("#define QUIC_PARAM_GLOBAL_TLS_PROVIDER 0x0100000A")]
    public const int QUIC_PARAM_GLOBAL_TLS_PROVIDER = 0x0100000A;

    [NativeTypeName("#define QUIC_PARAM_GLOBAL_STATELESS_RESET_KEY 0x0100000B")]
    public const int QUIC_PARAM_GLOBAL_STATELESS_RESET_KEY = 0x0100000B;

    [NativeTypeName("#define QUIC_PARAM_CONFIGURATION_SETTINGS 0x03000000")]
    public const int QUIC_PARAM_CONFIGURATION_SETTINGS = 0x03000000;

    [NativeTypeName("#define QUIC_PARAM_CONFIGURATION_TICKET_KEYS 0x03000001")]
    public const int QUIC_PARAM_CONFIGURATION_TICKET_KEYS = 0x03000001;

    [NativeTypeName("#define QUIC_PARAM_CONFIGURATION_SCHANNEL_CREDENTIAL_ATTRIBUTE_W 0x03000003")]
    public const int QUIC_PARAM_CONFIGURATION_SCHANNEL_CREDENTIAL_ATTRIBUTE_W = 0x03000003;

    [NativeTypeName("#define QUIC_PARAM_LISTENER_LOCAL_ADDRESS 0x04000000")]
    public const int QUIC_PARAM_LISTENER_LOCAL_ADDRESS = 0x04000000;

    [NativeTypeName("#define QUIC_PARAM_LISTENER_STATS 0x04000001")]
    public const int QUIC_PARAM_LISTENER_STATS = 0x04000001;

    [NativeTypeName("#define QUIC_PARAM_CONN_QUIC_VERSION 0x05000000")]
    public const int QUIC_PARAM_CONN_QUIC_VERSION = 0x05000000;

    [NativeTypeName("#define QUIC_PARAM_CONN_LOCAL_ADDRESS 0x05000001")]
    public const int QUIC_PARAM_CONN_LOCAL_ADDRESS = 0x05000001;

    [NativeTypeName("#define QUIC_PARAM_CONN_REMOTE_ADDRESS 0x05000002")]
    public const int QUIC_PARAM_CONN_REMOTE_ADDRESS = 0x05000002;

    [NativeTypeName("#define QUIC_PARAM_CONN_IDEAL_PROCESSOR 0x05000003")]
    public const int QUIC_PARAM_CONN_IDEAL_PROCESSOR = 0x05000003;

    [NativeTypeName("#define QUIC_PARAM_CONN_SETTINGS 0x05000004")]
    public const int QUIC_PARAM_CONN_SETTINGS = 0x05000004;

    [NativeTypeName("#define QUIC_PARAM_CONN_STATISTICS 0x05000005")]
    public const int QUIC_PARAM_CONN_STATISTICS = 0x05000005;

    [NativeTypeName("#define QUIC_PARAM_CONN_STATISTICS_PLAT 0x05000006")]
    public const int QUIC_PARAM_CONN_STATISTICS_PLAT = 0x05000006;

    [NativeTypeName("#define QUIC_PARAM_CONN_SHARE_UDP_BINDING 0x05000007")]
    public const int QUIC_PARAM_CONN_SHARE_UDP_BINDING = 0x05000007;

    [NativeTypeName("#define QUIC_PARAM_CONN_LOCAL_BIDI_STREAM_COUNT 0x05000008")]
    public const int QUIC_PARAM_CONN_LOCAL_BIDI_STREAM_COUNT = 0x05000008;

    [NativeTypeName("#define QUIC_PARAM_CONN_LOCAL_UNIDI_STREAM_COUNT 0x05000009")]
    public const int QUIC_PARAM_CONN_LOCAL_UNIDI_STREAM_COUNT = 0x05000009;

    [NativeTypeName("#define QUIC_PARAM_CONN_MAX_STREAM_IDS 0x0500000A")]
    public const int QUIC_PARAM_CONN_MAX_STREAM_IDS = 0x0500000A;

    [NativeTypeName("#define QUIC_PARAM_CONN_CLOSE_REASON_PHRASE 0x0500000B")]
    public const int QUIC_PARAM_CONN_CLOSE_REASON_PHRASE = 0x0500000B;

    [NativeTypeName("#define QUIC_PARAM_CONN_STREAM_SCHEDULING_SCHEME 0x0500000C")]
    public const int QUIC_PARAM_CONN_STREAM_SCHEDULING_SCHEME = 0x0500000C;

    [NativeTypeName("#define QUIC_PARAM_CONN_DATAGRAM_RECEIVE_ENABLED 0x0500000D")]
    public const int QUIC_PARAM_CONN_DATAGRAM_RECEIVE_ENABLED = 0x0500000D;

    [NativeTypeName("#define QUIC_PARAM_CONN_DATAGRAM_SEND_ENABLED 0x0500000E")]
    public const int QUIC_PARAM_CONN_DATAGRAM_SEND_ENABLED = 0x0500000E;

    [NativeTypeName("#define QUIC_PARAM_CONN_RESUMPTION_TICKET 0x05000010")]
    public const int QUIC_PARAM_CONN_RESUMPTION_TICKET = 0x05000010;

    [NativeTypeName("#define QUIC_PARAM_CONN_PEER_CERTIFICATE_VALID 0x05000011")]
    public const int QUIC_PARAM_CONN_PEER_CERTIFICATE_VALID = 0x05000011;

    [NativeTypeName("#define QUIC_PARAM_CONN_LOCAL_INTERFACE 0x05000012")]
    public const int QUIC_PARAM_CONN_LOCAL_INTERFACE = 0x05000012;

    [NativeTypeName("#define QUIC_PARAM_CONN_TLS_SECRETS 0x05000013")]
    public const int QUIC_PARAM_CONN_TLS_SECRETS = 0x05000013;

    [NativeTypeName("#define QUIC_PARAM_CONN_STATISTICS_V2 0x05000016")]
    public const int QUIC_PARAM_CONN_STATISTICS_V2 = 0x05000016;

    [NativeTypeName("#define QUIC_PARAM_CONN_STATISTICS_V2_PLAT 0x05000017")]
    public const int QUIC_PARAM_CONN_STATISTICS_V2_PLAT = 0x05000017;

    [NativeTypeName("#define QUIC_PARAM_CONN_ORIG_DEST_CID 0x05000018")]
    public const int QUIC_PARAM_CONN_ORIG_DEST_CID = 0x05000018;

    [NativeTypeName("#define QUIC_PARAM_TLS_HANDSHAKE_INFO 0x06000000")]
    public const int QUIC_PARAM_TLS_HANDSHAKE_INFO = 0x06000000;

    [NativeTypeName("#define QUIC_PARAM_TLS_NEGOTIATED_ALPN 0x06000001")]
    public const int QUIC_PARAM_TLS_NEGOTIATED_ALPN = 0x06000001;

    [NativeTypeName("#define QUIC_PARAM_TLS_SCHANNEL_CONTEXT_ATTRIBUTE_W 0x07000000")]
    public const int QUIC_PARAM_TLS_SCHANNEL_CONTEXT_ATTRIBUTE_W = 0x07000000;

    [NativeTypeName("#define QUIC_PARAM_TLS_SCHANNEL_CONTEXT_ATTRIBUTE_EX_W 0x07000001")]
    public const int QUIC_PARAM_TLS_SCHANNEL_CONTEXT_ATTRIBUTE_EX_W = 0x07000001;

    [NativeTypeName("#define QUIC_PARAM_TLS_SCHANNEL_SECURITY_CONTEXT_TOKEN 0x07000002")]
    public const int QUIC_PARAM_TLS_SCHANNEL_SECURITY_CONTEXT_TOKEN = 0x07000002;

    [NativeTypeName("#define QUIC_PARAM_STREAM_ID 0x08000000")]
    public const int QUIC_PARAM_STREAM_ID = 0x08000000;

    [NativeTypeName("#define QUIC_PARAM_STREAM_0RTT_LENGTH 0x08000001")]
    public const int QUIC_PARAM_STREAM_0RTT_LENGTH = 0x08000001;

    [NativeTypeName("#define QUIC_PARAM_STREAM_IDEAL_SEND_BUFFER_SIZE 0x08000002")]
    public const int QUIC_PARAM_STREAM_IDEAL_SEND_BUFFER_SIZE = 0x08000002;

    [NativeTypeName("#define QUIC_PARAM_STREAM_PRIORITY 0x08000003")]
    public const int QUIC_PARAM_STREAM_PRIORITY = 0x08000003;

    [NativeTypeName("#define QUIC_PARAM_STREAM_STATISTICS 0X08000004")]
    public const int QUIC_PARAM_STREAM_STATISTICS = 0X08000004;

    [NativeTypeName("#define QUIC_API_VERSION_1 1")]
    public const int QUIC_API_VERSION_1 = 1;

    [NativeTypeName("#define QUIC_API_VERSION_2 2")]
    public const int QUIC_API_VERSION_2 = 2;
}
