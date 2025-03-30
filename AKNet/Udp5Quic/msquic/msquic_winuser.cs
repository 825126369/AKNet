using System.Net;
using System.Net.Sockets;

namespace AKNet.Udp5Quic.Common
{
    internal static partial class MSQuicFunc
    {
        public const ulong QUIC_STATUS_SUCCESS = 0;                                            // 0x0
        public const ulong QUIC_STATUS_PENDING = 1;   // 0x703e5
        public const ulong QUIC_STATUS_CONTINUE = 2;      // 0x704de
        public const ulong QUIC_STATUS_OUT_OF_MEMORY = 3;
        public const ulong QUIC_STATUS_INVALID_PARAMETER = 4;
        public const ulong QUIC_STATUS_INVALID_STATE = 5;
        public const ulong QUIC_STATUS_NOT_SUPPORTED = 6;
        public const ulong QUIC_STATUS_NOT_FOUND = 7;
        public const ulong QUIC_STATUS_FILE_NOT_FOUND = 8;
        public const ulong QUIC_STATUS_BUFFER_TOO_SMALL = 9;
        public const ulong QUIC_STATUS_HANDSHAKE_FAILURE = 10;
        public const ulong QUIC_STATUS_ABORTED = 11;
        public const ulong QUIC_STATUS_ADDRESS_IN_USE = 12;
        public const ulong QUIC_STATUS_INVALID_ADDRESS = 13;
        public const ulong QUIC_STATUS_CONNECTION_TIMEOUT = 14;
        public const ulong QUIC_STATUS_CONNECTION_IDLE = 15;
        public const ulong QUIC_STATUS_UNREACHABLE = 16;
        public const ulong QUIC_STATUS_INTERNAL_ERROR = 17;
        public const ulong QUIC_STATUS_CONNECTION_REFUSED = 18;
        public const ulong QUIC_STATUS_PROTOCOL_ERROR = 19;
        public const ulong QUIC_STATUS_VER_NEG_ERROR = 20;
        public const ulong QUIC_STATUS_TLS_ERROR = 21;
        public const ulong QUIC_STATUS_USER_CANCELED = 22;
        public const ulong QUIC_STATUS_ALPN_NEG_FAILURE = 23;
        public const ulong QUIC_STATUS_STREAM_LIMIT_REACHED = 24;
        public const ulong QUIC_STATUS_ALPN_IN_USE = 25;

        public const ulong QUIC_STATUS_CLOSE_NOTIFY = 26;   // Close notify
        public const ulong QUIC_STATUS_BAD_CERTIFICATE = 27;   // Bad Certificate
        public const ulong QUIC_STATUS_UNSUPPORTED_CERTIFICATE = 28;  // Unsupported Certficiate
        public const ulong QUIC_STATUS_REVOKED_CERTIFICATE = 29;  // Revoked Certificate
        public const ulong QUIC_STATUS_EXPIRED_CERTIFICATE = 30;  // Expired Certificate
        public const ulong QUIC_STATUS_UNKNOWN_CERTIFICATE = 31;  // Unknown Certificate
        public const ulong QUIC_STATUS_REQUIRED_CERTIFICATE = 32; // Required Certificate

        public const ulong QUIC_STATUS_CERT_EXPIRED = 33;
        public const ulong QUIC_STATUS_CERT_UNTRUSTED_ROOT = 34;
        public const ulong QUIC_STATUS_CERT_NO_CERT = 35;

        static bool QUIC_FAILED(ulong Status)
        {
            return Status != 0;
        }

        static bool QUIC_SUCCEEDED(ulong Status)
        {
            return  Status == 0;
        }

        static AddressFamily QuicAddrGetFamily(IPAddress Addr)
        {
            return Addr.AddressFamily;
        }
    }
}
