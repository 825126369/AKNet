namespace AKNet.Udp5Quic.Common
{
    internal class QUIC_TRANSPORT_PARAMETERS
    {
        public uint Flags; // Set of QUIC_TP_FLAG_*
        public long IdleTimeout;
        public ulong InitialMaxStreamDataBidiLocal;
        public ulong InitialMaxStreamDataBidiRemote;
        public ulong InitialMaxStreamDataUni;
        public ulong InitialMaxData;
        public ulong InitialMaxBidiStreams;
        public ulong InitialMaxUniStreams;
        public ulong MaxUdpPayloadSize;

        [_Field_range_(0, MSQuicFunc.QUIC_TP_ACK_DELAY_EXPONENT_MAX)]
        public long AckDelayExponent;

        //
        // Indicates the maximum amount of time in milliseconds by which it will
        // delay sending of acknowledgments. If this value is absent, a default of
        // 25 milliseconds is assumed.
        //
        [_Field_range_(0, MSQuicFunc.QUIC_TP_MAX_ACK_DELAY_MAX)]
        public long MaxAckDelay;

        //
        // A variable-length integer representing the minimum amount of time in
        // microseconds by which the endpoint can delay an acknowledgment. Values
        // of 2^24 or greater are invalid.
        //
        // The presence of the parameter also advertises support of the ACK
        // Frequency extension.
        //
        _Field_range_(0, QUIC_TP_MIN_ACK_DELAY_MAX)
        QUIC_VAR_INT MinAckDelay;

        //
        // The maximum number connection IDs from the peer that an endpoint is
        // willing to store. This value includes only connection IDs sent in
        // NEW_CONNECTION_ID frames. If this parameter is absent, a default of 2 is
        // assumed.
        //
        _Field_range_(QUIC_TP_ACTIVE_CONNECTION_ID_LIMIT_MIN, QUIC_VAR_INT_MAX)
        QUIC_VAR_INT ActiveConnectionIdLimit;

        //
        // The maximum size of a DATAGRAM frame (including the frame type, length,
        // and payload) the endpoint is willing to receive, in bytes.
        //
        QUIC_VAR_INT MaxDatagramFrameSize;

        //
        // The value that the endpoint included in the Source Connection ID field
        // of the first Initial packet it sends for the connection.
        //
        uint8_t InitialSourceConnectionID[QUIC_MAX_CONNECTION_ID_LENGTH_V1];
        _Field_range_(0, QUIC_MAX_CONNECTION_ID_LENGTH_V1)
        uint8_t InitialSourceConnectionIDLength;

        //
        // The offset and length of the well-known CIBIR idenfitier.
        //
        QUIC_VAR_INT CibirLength;
        QUIC_VAR_INT CibirOffset;

        //
        // Server specific.
        //

        //
        // Used in verifying the stateless reset scenario.
        //
        uint8_t StatelessResetToken[QUIC_STATELESS_RESET_TOKEN_LENGTH];

        //
        // The server's preferred address.
        //
        QUIC_ADDR PreferredAddress;

        //
        // The value of the Destination Connection ID field from the first Initial
        // packet sent by the client. This transport parameter is only sent by a
        // server.
        //
        uint8_t OriginalDestinationConnectionID[QUIC_MAX_CONNECTION_ID_LENGTH_V1];
        _Field_range_(0, QUIC_MAX_CONNECTION_ID_LENGTH_V1)
        uint8_t OriginalDestinationConnectionIDLength;

        //
        // The value that the server included in the Source Connection ID field
        // of a Retry packet.
        //
        uint8_t RetrySourceConnectionID[QUIC_MAX_CONNECTION_ID_LENGTH_V1];
        _Field_range_(0, QUIC_MAX_CONNECTION_ID_LENGTH_V1)
        uint8_t RetrySourceConnectionIDLength;

        //
        // The version_information transport parameter opaque blob.
        //
        uint32_t VersionInfoLength;
        const uint8_t* VersionInfo;

    }
}
