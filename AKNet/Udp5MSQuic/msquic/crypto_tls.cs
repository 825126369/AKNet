using AKNet.Common;
using System;
using System.Text;

namespace AKNet.Udp5MSQuic.Common
{
    internal static partial class MSQuicFunc
    {
        public const int TLS1_PROTOCOL_VERSION = 0x0301;
        public const int TLS_MESSAGE_HEADER_LENGTH = 4;
        public const int TLS_RANDOM_LENGTH = 32;
        public const int TLS_SESSION_ID_LENGTH = 32;

        public const byte TlsExt_ServerName = 0x00;
        public const byte TlsExt_AppProtocolNegotiation = 0x10;
        public const byte TlsExt_SessionTicket = 0x23;

        public const byte TlsHandshake_ClientHello = 0x01;
        public const byte TlsExt_Sni_NameType_HostName = 0;

        public const ulong QUIC_TP_ID_ORIGINAL_DESTINATION_CONNECTION_ID = 0;   // uint8_t[]
        public const ulong QUIC_TP_ID_IDLE_TIMEOUT = 1;   // varint
        public const ulong QUIC_TP_ID_STATELESS_RESET_TOKEN = 2;   // uint8_t[16]
        public const ulong QUIC_TP_ID_MAX_UDP_PAYLOAD_SIZE = 3;   // varint
        public const ulong QUIC_TP_ID_INITIAL_MAX_DATA = 4;   // varint
        public const ulong QUIC_TP_ID_INITIAL_MAX_STREAM_DATA_BIDI_LOCAL = 5;   // varint
        public const ulong QUIC_TP_ID_INITIAL_MAX_STREAM_DATA_BIDI_REMOTE = 6;   // varint
        public const ulong QUIC_TP_ID_INITIAL_MAX_STREAM_DATA_UNI = 7;   // varint
        public const ulong QUIC_TP_ID_INITIAL_MAX_STREAMS_BIDI = 8;   // varint
        public const ulong QUIC_TP_ID_INITIAL_MAX_STREAMS_UNI = 9;   // varint
        public const ulong QUIC_TP_ID_ACK_DELAY_EXPONENT = 10;  // varint
        public const ulong QUIC_TP_ID_MAX_ACK_DELAY = 11;  // varint
        public const ulong QUIC_TP_ID_DISABLE_ACTIVE_MIGRATION = 12;  // N/A
        public const ulong QUIC_TP_ID_PREFERRED_ADDRESS = 13;  // PreferredAddress
        public const ulong QUIC_TP_ID_ACTIVE_CONNECTION_ID_LIMIT = 14;  // varint
        public const ulong QUIC_TP_ID_INITIAL_SOURCE_CONNECTION_ID = 15;  // uint8_t[]
        public const ulong QUIC_TP_ID_RETRY_SOURCE_CONNECTION_ID = 16;  // uint8_t[]
        public const ulong QUIC_TP_ID_MAX_DATAGRAM_FRAME_SIZE = 32;             // varint
        public const ulong QUIC_TP_ID_DISABLE_1RTT_ENCRYPTION = 0xBAAD;         // N/A
        public const ulong QUIC_TP_ID_VERSION_NEGOTIATION_EXT = 0x11;          // Blob
        public const ulong QUIC_TP_ID_MIN_ACK_DELAY = 0xFF03DE1A;   // varint
        public const ulong QUIC_TP_ID_CIBIR_ENCODING = 0x1000;         // {varint, varint}
        public const ulong QUIC_TP_ID_GREASE_QUIC_BIT = 0x2AB2;          // N/A
        public const ulong QUIC_TP_ID_RELIABLE_RESET_ENABLED = 0x17f7586d2cb570;   // varint
        public const ulong QUIC_TP_ID_ENABLE_TIMESTAMP = 0x7158;         // varint

        static ushort TlsReadUint16(QUIC_SSBuffer Buffer)
        {
            return (ushort)((Buffer[0] << 8) + Buffer[1]);
        }

        static uint TlsReadUint24(QUIC_SSBuffer Buffer)
        {
            return
                (((uint)Buffer[0] << 16) +
                 ((uint)Buffer[1] << 8) +
                  (uint)Buffer[2]);
        }

        static int TlsTransportParamLength(ulong Id, int Length)
        {
            return QuicVarIntSize(Id) + QuicVarIntSize(Length) + Length;
        }

        static ulong QuicCryptoTlsReadInitial(QUIC_CONNECTION Connection, QUIC_SSBuffer Buffer, QUIC_NEW_CONNECTION_INFO Info)
        {
            do
            {
                if (Buffer.Length < TLS_MESSAGE_HEADER_LENGTH)
                {
                    return QUIC_STATUS_PENDING;
                }

                if (Buffer[0] != TlsHandshake_ClientHello)
                {
                    return QUIC_STATUS_INVALID_PARAMETER;
                }

                int MessageLength = (int)TlsReadUint24(Buffer.Slice(1));
                if (Buffer.Length < TLS_MESSAGE_HEADER_LENGTH + MessageLength)
                {
                    return QUIC_STATUS_PENDING;
                }

                ulong Status = QuicCryptoTlsReadClientHello(Connection, Buffer.Slice(TLS_MESSAGE_HEADER_LENGTH, MessageLength), Info);
                if (QUIC_FAILED(Status))
                {
                    return Status;
                }

                Buffer = Buffer.Slice(TLS_MESSAGE_HEADER_LENGTH);
            } while (Buffer.Length > 0);

            if (Info.ClientAlpnList == null)
            {
                return QUIC_STATUS_INVALID_PARAMETER;
            }
            return QUIC_STATUS_SUCCESS;
        }

        static ulong QuicCryptoTlsReadClientHello(QUIC_CONNECTION Connection, QUIC_SSBuffer Buffer, QUIC_NEW_CONNECTION_INFO Info)
        {
            if (Buffer.Length < sizeof(ushort) || TlsReadUint16(Buffer) < TLS1_PROTOCOL_VERSION)
            {
                return QUIC_STATUS_INVALID_PARAMETER;
            }

            Buffer += sizeof(ushort);

            if (Buffer.Length < TLS_RANDOM_LENGTH)
            {
                return QUIC_STATUS_INVALID_PARAMETER;
            }

            Buffer += TLS_RANDOM_LENGTH;

            if (Buffer.Length < sizeof(byte) || Buffer[0] > TLS_SESSION_ID_LENGTH || Buffer.Length < sizeof(byte) + Buffer[0])
            {
                return QUIC_STATUS_INVALID_PARAMETER;
            }

            Buffer += sizeof(byte) + Buffer[0];

            if (Buffer.Length < sizeof(ushort))
            {
                return QUIC_STATUS_INVALID_PARAMETER;
            }

            int Len = TlsReadUint16(Buffer);
            if ((Len % 2 != 0) || Buffer.Length < (sizeof(ushort) + Len))
            {
                return QUIC_STATUS_INVALID_PARAMETER;
            }

            Buffer += sizeof(ushort) + Len;

            if (Buffer.Length < sizeof(byte) || Buffer[0] < 1 || Buffer.Length < sizeof(byte) + Buffer[0])
            {
                return QUIC_STATUS_INVALID_PARAMETER;
            }

            Buffer += sizeof(byte) + Buffer[0];
            if (Buffer.Length < sizeof(ushort))
            {
                return QUIC_STATUS_SUCCESS;
            }

            Len = TlsReadUint16(Buffer);
            if (Buffer.Length < (sizeof(ushort) + Len))
            {
                return QUIC_STATUS_INVALID_PARAMETER;
            }

            Buffer += sizeof(ushort);
            return QuicCryptoTlsReadExtensions(Connection, Buffer.Slice(0, Len), Info);
        }

        static ulong QuicCryptoTlsReadExtensions(QUIC_CONNECTION Connection, QUIC_SSBuffer Buffer, QUIC_NEW_CONNECTION_INFO Info)
        {
            bool FoundSNI = false;
            bool FoundALPN = false;
            bool FoundTransportParameters = false;
            while (Buffer.Length > 0)
            {
                if (Buffer.Length < 2 * sizeof(ushort))
                {
                    return QUIC_STATUS_INVALID_PARAMETER;
                }

                ushort ExtType = TlsReadUint16(Buffer);
                ushort ExtLen = TlsReadUint16(Buffer.Slice(sizeof(ushort)));

                Buffer = Buffer.Slice(2 * sizeof(ushort));

                if (Buffer.Length < ExtLen)
                {
                    return QUIC_STATUS_INVALID_PARAMETER;
                }

                if (ExtType == TlsExt_ServerName)
                {
                    if (FoundSNI)
                    {
                        return QUIC_STATUS_INVALID_PARAMETER;
                    }
                    ulong Status = QuicCryptoTlsReadSniExtension(Connection, Buffer.Slice(0, ExtLen), Info);
                    if (QUIC_FAILED(Status))
                    {
                        return Status;
                    }
                    FoundSNI = true;

                }
                else if (ExtType == TlsExt_AppProtocolNegotiation)
                {
                    if (FoundALPN)
                    {
                        return QUIC_STATUS_INVALID_PARAMETER;
                    }
                    ulong Status = QuicCryptoTlsReadAlpnExtension(Connection, Buffer.Slice(0, ExtLen), Info);
                    if (QUIC_FAILED(Status))
                    {
                        return Status;
                    }
                    FoundALPN = true;
                }
                else
                {
                    if (ExtType == TLS_EXTENSION_TYPE_QUIC_TRANSPORT_PARAMETERS)
                    {
                        if (FoundTransportParameters)
                        {
                            return QUIC_STATUS_INVALID_PARAMETER;
                        }
                        if (!QuicCryptoTlsDecodeTransportParameters(
                                Connection,
                                false,
                                Buffer.Slice(0, ExtLen),
                                Connection.PeerTransportParams))
                        {
                            return QUIC_STATUS_INVALID_PARAMETER;
                        }
                        FoundTransportParameters = true;
                    }
                }
                Buffer = Buffer.Slice(ExtLen);
            }

            if (!FoundTransportParameters)
            {
                return QUIC_STATUS_INVALID_PARAMETER;
            }
            return QUIC_STATUS_SUCCESS;
        }

        static ulong QuicCryptoTlsReadSniExtension(QUIC_CONNECTION Connection, QUIC_SSBuffer Buffer, QUIC_NEW_CONNECTION_INFO Info)
        {
            if (Buffer.Length < sizeof(ushort))
            {
                return QUIC_STATUS_INVALID_PARAMETER;
            }

            if (TlsReadUint16(Buffer) < 3)
            {
                return QUIC_STATUS_INVALID_PARAMETER;
            }

            Buffer = Buffer.Slice(sizeof(ushort));

            bool Found = false;
            while (Buffer.Length > 0)
            {

                byte NameType = Buffer[0];
                Buffer = Buffer.Slice(sizeof(byte));

                if (Buffer.Length < sizeof(ushort))
                {
                    return QUIC_STATUS_INVALID_PARAMETER;
                }

                int NameLen = TlsReadUint16(Buffer);
                Buffer = Buffer.Slice(sizeof(ushort));
                if (Buffer.Length < NameLen)
                {
                    return QUIC_STATUS_INVALID_PARAMETER;
                }

                if (NameType == TlsExt_Sni_NameType_HostName && !Found)
                {
                    Info.ServerName = Encoding.UTF8.GetString(Buffer.GetSpan());
                    Found = true;
                }
                Buffer = Buffer.Slice(NameLen);
            }

            return QUIC_STATUS_SUCCESS;
        }

        static ulong QuicCryptoTlsReadAlpnExtension(QUIC_CONNECTION Connection, QUIC_SSBuffer Buffer, QUIC_NEW_CONNECTION_INFO Info)
        {
            if (Buffer.Length < sizeof(ushort) + 2 * sizeof(byte))
            {
                return QUIC_STATUS_INVALID_PARAMETER;
            }

            if (Buffer.Length != TlsReadUint16(Buffer) + sizeof(ushort))
            {
                return QUIC_STATUS_INVALID_PARAMETER;
            }

            Buffer = Buffer.Slice(sizeof(ushort));

            Info.ClientAlpnList = Buffer;

            while (Buffer.Length > 0)
            {
                int Len = Buffer[0];
                Buffer = Buffer.Slice(1);

                if (Buffer.Length < 1 || Buffer.Length < Len)
                {
                    return QUIC_STATUS_INVALID_PARAMETER;
                }

                Buffer = Buffer.Slice(Len);
            }

            return QUIC_STATUS_SUCCESS;
        }
        static bool QuicCryptoTlsDecodeTransportParameters(QUIC_CONNECTION Connection, bool IsServerTP, QUIC_SSBuffer TPBuf, QUIC_TRANSPORT_PARAMETERS TransportParams)
        {
            return QuicCryptoTlsDecodeTransportParameters(Connection, IsServerTP, TPBuf.GetSpan(), TransportParams);
        }

        static bool QuicCryptoTlsDecodeTransportParameters(QUIC_CONNECTION Connection, bool IsServerTP, ReadOnlySpan<byte> TPBuf, QUIC_TRANSPORT_PARAMETERS TransportParams)
        {
            NetLogHelper.PrintByteArray("LocalTPBuffer", TPBuf);

            bool Result = false;
            ulong ParamsPresent = 0;

            TransportParams.Reset();
            TransportParams.MaxUdpPayloadSize = QUIC_TP_MAX_PACKET_SIZE_DEFAULT;
            TransportParams.AckDelayExponent = QUIC_TP_ACK_DELAY_EXPONENT_DEFAULT;
            TransportParams.MaxAckDelay = QUIC_TP_MAX_ACK_DELAY_DEFAULT;
            TransportParams.ActiveConnectionIdLimit = QUIC_TP_ACTIVE_CONNECTION_ID_LIMIT_DEFAULT;

            while (TPBuf.Length > 0)
            {
                ulong Id = 0;
                if (!QuicVarIntDecode(ref TPBuf, ref Id))
                {
                    goto Exit;
                }

                if (Id < (8 * sizeof(ulong)))
                {
                    if (BoolOk(ParamsPresent & (ulong)(1UL << (int)Id)))
                    {
                        goto Exit;
                    }

                    ParamsPresent |= (ulong)(1UL << (int)Id);
                }

                int ParamLength = 0;
                if (!QuicVarIntDecode(ref TPBuf, ref ParamLength))
                {
                    goto Exit;
                }
                else if (ParamLength > TPBuf.Length)
                {
                    goto Exit;
                }

                int Length = (int)ParamLength;
                switch (Id)
                {
                    case QUIC_TP_ID_ORIGINAL_DESTINATION_CONNECTION_ID:
                        if (Length > QUIC_MAX_CONNECTION_ID_LENGTH_V1)
                        {
                            goto Exit;
                        }
                        else if (!IsServerTP)
                        {
                            goto Exit;
                        }
                        TransportParams.Flags |= QUIC_TP_FLAG_ORIGINAL_DESTINATION_CONNECTION_ID;
                        TransportParams.OriginalDestinationConnectionID.Length = (byte)Length;
                        TPBuf.Slice(0, Length).CopyTo(TransportParams.OriginalDestinationConnectionID.GetSpan());
                        break;

                    case QUIC_TP_ID_IDLE_TIMEOUT:
                        if (!QuicVarIntDecode(ref TPBuf, ref TransportParams.IdleTimeout))
                        {
                            goto Exit;
                        }
                        TransportParams.Flags |= QUIC_TP_FLAG_IDLE_TIMEOUT;
                        break;

                    case QUIC_TP_ID_STATELESS_RESET_TOKEN:
                        if (Length != QUIC_STATELESS_RESET_TOKEN_LENGTH)
                        {
                            goto Exit;
                        }
                        else if (!IsServerTP)
                        {
                            goto Exit;
                        }
                        TransportParams.Flags |= QUIC_TP_FLAG_STATELESS_RESET_TOKEN;
                        TPBuf.Slice(0, QUIC_STATELESS_RESET_TOKEN_LENGTH).CopyTo(TransportParams.StatelessResetToken);

                        break;
                    case QUIC_TP_ID_MAX_UDP_PAYLOAD_SIZE:
                        if (!QuicVarIntDecode(ref TPBuf, ref TransportParams.MaxUdpPayloadSize))
                        {
                            goto Exit;
                        }

                        if (TransportParams.MaxUdpPayloadSize < QUIC_TP_MAX_UDP_PAYLOAD_SIZE_MIN)
                        {
                            goto Exit;
                        }

                        if (TransportParams.MaxUdpPayloadSize > QUIC_TP_MAX_UDP_PAYLOAD_SIZE_MAX)
                        {
                            goto Exit;
                        }

                        TransportParams.Flags |= QUIC_TP_FLAG_MAX_UDP_PAYLOAD_SIZE;
                        break;

                    case QUIC_TP_ID_INITIAL_MAX_DATA:
                        if (!QuicVarIntDecode(ref TPBuf, ref TransportParams.InitialMaxData))
                        {
                            goto Exit;
                        }
                        TransportParams.Flags |= QUIC_TP_FLAG_INITIAL_MAX_DATA;
                        break;

                    case QUIC_TP_ID_INITIAL_MAX_STREAM_DATA_BIDI_LOCAL:
                        if (!QuicVarIntDecode(ref TPBuf, ref TransportParams.InitialMaxStreamDataBidiLocal))
                        {
                            goto Exit;
                        }
                        TransportParams.Flags |= QUIC_TP_FLAG_INITIAL_MAX_STRM_DATA_BIDI_LOCAL;
                        break;

                    case QUIC_TP_ID_INITIAL_MAX_STREAM_DATA_BIDI_REMOTE:
                        if (!QuicVarIntDecode(ref TPBuf, ref TransportParams.InitialMaxStreamDataBidiRemote))
                        {
                            goto Exit;
                        }
                        TransportParams.Flags |= QUIC_TP_FLAG_INITIAL_MAX_STRM_DATA_BIDI_REMOTE;
                        break;

                    case QUIC_TP_ID_INITIAL_MAX_STREAM_DATA_UNI:
                        if (!QuicVarIntDecode(ref TPBuf, ref TransportParams.InitialMaxStreamDataUni))
                        {
                            goto Exit;
                        }
                        TransportParams.Flags |= QUIC_TP_FLAG_INITIAL_MAX_STRM_DATA_UNI;
                        break;

                    case QUIC_TP_ID_INITIAL_MAX_STREAMS_BIDI:
                        if (!QuicVarIntDecode(ref TPBuf, ref TransportParams.InitialMaxBidiStreams))
                        {
                            goto Exit;
                        }
                        if (TransportParams.InitialMaxBidiStreams > QUIC_TP_MAX_STREAMS_MAX)
                        {
                            goto Exit;
                        }
                        if (TransportParams.InitialMaxBidiStreams > QUIC_TP_MAX_STREAMS_MAX)
                        {
                            goto Exit;
                        }
                        TransportParams.Flags |= QUIC_TP_FLAG_INITIAL_MAX_STRMS_BIDI;
                        break;

                    case QUIC_TP_ID_INITIAL_MAX_STREAMS_UNI:
                        if (!QuicVarIntDecode(ref TPBuf, ref TransportParams.InitialMaxUniStreams))
                        {
                            goto Exit;
                        }
                        if (TransportParams.InitialMaxUniStreams > QUIC_TP_MAX_STREAMS_MAX)
                        {
                            goto Exit;
                        }
                        TransportParams.Flags |= QUIC_TP_FLAG_INITIAL_MAX_STRMS_UNI;
                        break;

                    case QUIC_TP_ID_ACK_DELAY_EXPONENT:
                        if (!QuicVarIntDecode(ref TPBuf, ref TransportParams.AckDelayExponent))
                        {
                            goto Exit;
                        }

                        if (TransportParams.AckDelayExponent > QUIC_TP_ACK_DELAY_EXPONENT_MAX)
                        {
                            goto Exit;
                        }

                        TransportParams.Flags |= QUIC_TP_FLAG_ACK_DELAY_EXPONENT;
                        break;

                    case QUIC_TP_ID_MAX_ACK_DELAY:
                        if (!QuicVarIntDecode(ref TPBuf, ref TransportParams.MaxAckDelay))
                        {
                            goto Exit;
                        }
                        if (TransportParams.MaxAckDelay > QUIC_TP_MAX_ACK_DELAY_MAX)
                        {
                            goto Exit;
                        }
                        TransportParams.Flags |= QUIC_TP_FLAG_MAX_ACK_DELAY;
                        break;

                    case QUIC_TP_ID_DISABLE_ACTIVE_MIGRATION:
                        if (Length != 0)
                        {
                            goto Exit;
                        }
                        TransportParams.Flags |= QUIC_TP_FLAG_DISABLE_ACTIVE_MIGRATION;
                        break;

                    case QUIC_TP_ID_PREFERRED_ADDRESS:
                        if (!IsServerTP)
                        {
                            goto Exit;
                        }
                        break;

                    case QUIC_TP_ID_ACTIVE_CONNECTION_ID_LIMIT:
                        if (!QuicVarIntDecode(ref TPBuf, ref TransportParams.ActiveConnectionIdLimit))
                        {
                            goto Exit;
                        }

                        if (TransportParams.ActiveConnectionIdLimit < QUIC_TP_ACTIVE_CONNECTION_ID_LIMIT_MIN)
                        {
                            goto Exit;
                        }
                        TransportParams.Flags |= QUIC_TP_FLAG_ACTIVE_CONNECTION_ID_LIMIT;
                        break;

                    case QUIC_TP_ID_INITIAL_SOURCE_CONNECTION_ID:
                        if (Length > QUIC_MAX_CONNECTION_ID_LENGTH_V1)
                        {
                            goto Exit;
                        }
                        TransportParams.Flags |= QUIC_TP_FLAG_INITIAL_SOURCE_CONNECTION_ID;
                        TransportParams.InitialSourceConnectionID.Length = (byte)Length;
                        TPBuf.Slice(0, Length).CopyTo(TransportParams.InitialSourceConnectionID.GetSpan());
                        break;

                    case QUIC_TP_ID_RETRY_SOURCE_CONNECTION_ID:
                        if (Length > QUIC_MAX_CONNECTION_ID_LENGTH_V1)
                        {
                            goto Exit;
                        }
                        else if (!IsServerTP)
                        {
                            goto Exit;
                        }
                        TransportParams.Flags |= QUIC_TP_FLAG_RETRY_SOURCE_CONNECTION_ID;
                        TransportParams.RetrySourceConnectionID.Length = (byte)Length;
                        TPBuf.Slice(0, Length).CopyTo(TransportParams.RetrySourceConnectionID.GetSpan());
                        break;

                    case QUIC_TP_ID_MAX_DATAGRAM_FRAME_SIZE:
                        if (!QuicVarIntDecode(ref TPBuf, ref TransportParams.MaxDatagramFrameSize))
                        {
                            goto Exit;
                        }
                        TransportParams.Flags |= QUIC_TP_FLAG_MAX_DATAGRAM_FRAME_SIZE;
                        break;

                    case QUIC_TP_ID_CIBIR_ENCODING:
                        if (!QuicVarIntDecode(ref TPBuf, ref TransportParams.CibirLength) ||
                            TransportParams.CibirLength < 1 ||
                            TransportParams.CibirLength > QUIC_MAX_CONNECTION_ID_LENGTH_INVARIANT ||
                            !QuicVarIntDecode(ref TPBuf, ref TransportParams.CibirOffset) ||
                            TransportParams.CibirOffset > QUIC_MAX_CONNECTION_ID_LENGTH_INVARIANT ||
                            TransportParams.CibirLength + TransportParams.CibirOffset > QUIC_MAX_CONNECTION_ID_LENGTH_INVARIANT)
                        {
                            goto Exit;
                        }
                        TransportParams.Flags |= QUIC_TP_FLAG_CIBIR_ENCODING;
                        break;

                    case QUIC_TP_ID_DISABLE_1RTT_ENCRYPTION:
                        if (Length != 0)
                        {
                            goto Exit;
                        }
                        TransportParams.Flags |= QUIC_TP_FLAG_DISABLE_1RTT_ENCRYPTION;
                        break;

                    case QUIC_TP_ID_VERSION_NEGOTIATION_EXT:
                        if (Length > 0)
                        {
                            TPBuf.Slice(0, Length).CopyTo(TransportParams.VersionInfo.GetSpan());
                        }
                        TransportParams.Flags |= QUIC_TP_FLAG_VERSION_NEGOTIATION;
                        TransportParams.VersionInfo.Length = (int)Length;
                        break;

                    case QUIC_TP_ID_MIN_ACK_DELAY:
                        if (!QuicVarIntDecode(ref TPBuf, ref TransportParams.MinAckDelay))
                        {
                            goto Exit;
                        }
                        if (TransportParams.MinAckDelay > QUIC_TP_MIN_ACK_DELAY_MAX)
                        {
                            goto Exit;
                        }
                        TransportParams.Flags |= QUIC_TP_FLAG_MIN_ACK_DELAY;
                        break;

                    case QUIC_TP_ID_GREASE_QUIC_BIT:
                        if (Length != 0)
                        {
                            goto Exit;
                        }
                        TransportParams.Flags |= QUIC_TP_FLAG_GREASE_QUIC_BIT;
                        break;

                    case QUIC_TP_ID_RELIABLE_RESET_ENABLED:
                        if (Length != 0)
                        {
                            goto Exit;
                        }
                        TransportParams.Flags |= QUIC_TP_FLAG_RELIABLE_RESET_ENABLED;
                        break;

                    case QUIC_TP_ID_ENABLE_TIMESTAMP:
                        {
                            ulong value = 0;
                            if (!QuicVarIntDecode(ref TPBuf, ref value))
                            {
                                goto Exit;
                            }
                            if (value > 3)
                            {
                                goto Exit;
                            }
                            value <<= QUIC_TP_FLAG_TIMESTAMP_SHIFT; // Convert to QUIC_TP_FLAG_TIMESTAMP_*
                            TransportParams.Flags |= (uint)value;
                            break;
                        }

                    default:
                        if (QuicTpIdIsReserved(Id))
                        {

                        }
                        else
                        {

                        }
                        break;
                }
                TPBuf = TPBuf.Slice(Length);
            }

            if (BoolOk(TransportParams.Flags & QUIC_TP_FLAG_MIN_ACK_DELAY) && TransportParams.MinAckDelay > TransportParams.MaxAckDelay)
            {
                goto Exit;
            }

            Result = true;
        Exit:
            return Result;
        }

        static int QuicCryptoTlsGetCompleteTlsMessagesLength(QUIC_SSBuffer Buffer)
        {
            int MessagesLength = 0;
            while (Buffer.Length >= TLS_MESSAGE_HEADER_LENGTH)
            {
                int MessageLength = TLS_MESSAGE_HEADER_LENGTH + (int)TlsReadUint24(Buffer.Slice(1));
                if (Buffer.Length < MessageLength)
                {
                    break;
                }
                Buffer = Buffer.Slice(MessageLength);
                MessagesLength += MessageLength;
            }
            return MessagesLength;
        }

        static bool QuicTpIdIsReserved(ulong ID)
        {
            return (ID % 31) == 27;
        }

        static ulong QuicCryptoTlsReadClientRandom(QUIC_SSBuffer Buffer, QUIC_TLS_SECRETS TlsSecrets)
        {
            NetLog.Assert(Buffer.Length >= TLS_MESSAGE_HEADER_LENGTH + sizeof(ushort) + TLS_RANDOM_LENGTH);
            Buffer = Buffer.Slice(TLS_MESSAGE_HEADER_LENGTH + sizeof(ushort));
            Buffer.Slice(TLS_RANDOM_LENGTH).CopyTo(TlsSecrets.ClientRandom);
            TlsSecrets.IsSet.ClientRandom = true;
            return QUIC_STATUS_SUCCESS;
        }

        static void QuicCryptoTlsCleanupTransportParameters(QUIC_TRANSPORT_PARAMETERS TransportParams)
        {
            if (BoolOk(TransportParams.Flags & QUIC_TP_FLAG_VERSION_NEGOTIATION))
            {
                TransportParams.VersionInfo.Length = 0;
                TransportParams.Flags = (uint)(TransportParams.Flags & ~QUIC_TP_FLAG_VERSION_NEGOTIATION);
            }
        }

        static QUIC_SSBuffer QuicCryptoTlsEncodeTransportParameters(QUIC_CONNECTION Connection, bool IsServerTP,
            QUIC_TRANSPORT_PARAMETERS TransportParams, QUIC_PRIVATE_TRANSPORT_PARAMETER TestParam)
        {
            int RequiredTPLen = 0;
            //original_destination_connection_id：客户端首次发送的 Initial 数据包中的目标连接 ID
            if (HasFlag(TransportParams.Flags, QUIC_TP_FLAG_ORIGINAL_DESTINATION_CONNECTION_ID))
            {
                NetLog.Assert(IsServerTP);
                NetLog.Assert(TransportParams.OriginalDestinationConnectionID.Length <= QUIC_MAX_CONNECTION_ID_LENGTH_V1);
                RequiredTPLen += TlsTransportParamLength(QUIC_TP_ID_ORIGINAL_DESTINATION_CONNECTION_ID, TransportParams.OriginalDestinationConnectionID.Length);
            }

            //max_idle_timeout：最大空闲超时时间，以毫秒为单位
            if (HasFlag(TransportParams.Flags, QUIC_TP_FLAG_IDLE_TIMEOUT))
            {
                RequiredTPLen += TlsTransportParamLength(QUIC_TP_ID_IDLE_TIMEOUT, QuicVarIntSize(TransportParams.IdleTimeout));
            }

            //stateless_reset_token：无状态重置令牌，用于验证无状态重置。
            if (HasFlag(TransportParams.Flags, QUIC_TP_FLAG_STATELESS_RESET_TOKEN))
            {
                NetLog.Assert(IsServerTP);
                RequiredTPLen += TlsTransportParamLength(QUIC_TP_ID_STATELESS_RESET_TOKEN, QUIC_STATELESS_RESET_TOKEN_LENGTH);
            }

            //max_udp_payload_size：端点愿意接收的最大 UDP 负载大小。
            if (HasFlag(TransportParams.Flags, QUIC_TP_FLAG_MAX_UDP_PAYLOAD_SIZE))
            {
                RequiredTPLen += TlsTransportParamLength(QUIC_TP_ID_MAX_UDP_PAYLOAD_SIZE, QuicVarIntSize(TransportParams.MaxUdpPayloadSize));
            }

            //initial_max_data：连接上最初可发送的最大数据量
            if (HasFlag(TransportParams.Flags, QUIC_TP_FLAG_INITIAL_MAX_DATA))
            {
                RequiredTPLen += TlsTransportParamLength(QUIC_TP_ID_INITIAL_MAX_DATA, QuicVarIntSize(TransportParams.InitialMaxData));
            }

            //initial_max_stream_data_bidi_local：本地发起的双向流的初始流量控制限制
            if (HasFlag(TransportParams.Flags, QUIC_TP_FLAG_INITIAL_MAX_STRM_DATA_BIDI_LOCAL))
            {
                RequiredTPLen += TlsTransportParamLength(QUIC_TP_ID_INITIAL_MAX_STREAM_DATA_BIDI_LOCAL, QuicVarIntSize(TransportParams.InitialMaxStreamDataBidiLocal));
            }

            //initial_max_stream_data_bidi_remote：对端发起的双向流的初始流量控制限制
            if (HasFlag(TransportParams.Flags, QUIC_TP_FLAG_INITIAL_MAX_STRM_DATA_BIDI_REMOTE))
            {
                RequiredTPLen += TlsTransportParamLength(QUIC_TP_ID_INITIAL_MAX_STREAM_DATA_BIDI_REMOTE, QuicVarIntSize(TransportParams.InitialMaxStreamDataBidiRemote));
            }

            //initial_max_stream_data_uni：单向流的初始流量控制限制。
            if (HasFlag(TransportParams.Flags, QUIC_TP_FLAG_INITIAL_MAX_STRM_DATA_UNI))
            {
                RequiredTPLen += TlsTransportParamLength(QUIC_TP_ID_INITIAL_MAX_STREAM_DATA_UNI, QuicVarIntSize(TransportParams.InitialMaxStreamDataUni));
            }

            //initial_max_streams_bidi：对端可以发起的双向流的最大数量。
            if (HasFlag(TransportParams.Flags, QUIC_TP_FLAG_INITIAL_MAX_STRMS_BIDI))
            {
                RequiredTPLen += TlsTransportParamLength(QUIC_TP_ID_INITIAL_MAX_STREAMS_BIDI, QuicVarIntSize(TransportParams.InitialMaxBidiStreams));
            }

            //initial_max_streams_uni：对端可以发起的单向流的最大数量。
            if (HasFlag(TransportParams.Flags, QUIC_TP_FLAG_INITIAL_MAX_STRMS_UNI))
            {
                RequiredTPLen += TlsTransportParamLength(QUIC_TP_ID_INITIAL_MAX_STREAMS_UNI, QuicVarIntSize(TransportParams.InitialMaxUniStreams));
            }

            //ack_delay_exponent：用于解码 ACK 帧中 ACK 延迟字段的指数。
            if (HasFlag(TransportParams.Flags, QUIC_TP_FLAG_ACK_DELAY_EXPONENT))
            {
                RequiredTPLen += TlsTransportParamLength(QUIC_TP_ID_ACK_DELAY_EXPONENT, QuicVarIntSize(TransportParams.AckDelayExponent));
            }

            //max_ack_delay：端点延迟发送确认的最大时间。
            if (HasFlag(TransportParams.Flags, QUIC_TP_FLAG_MAX_ACK_DELAY))
            {
                RequiredTPLen += TlsTransportParamLength(QUIC_TP_ID_MAX_ACK_DELAY, QuicVarIntSize(TransportParams.MaxAckDelay));
            }

            //disable_active_migration：如果端点不支持在握手期间使用的地址上进行活动连接迁移，则包含此参数。
            if (HasFlag(TransportParams.Flags, QUIC_TP_FLAG_DISABLE_ACTIVE_MIGRATION))
            {
                RequiredTPLen += TlsTransportParamLength(QUIC_TP_ID_DISABLE_ACTIVE_MIGRATION, 0);
            }

            //preferred_address：首选地址，包含 IPv4 和 IPv6 地址、端口、连接 ID 和无状态重置令牌
            if (HasFlag(TransportParams.Flags, QUIC_TP_FLAG_PREFERRED_ADDRESS))
            {
                NetLog.Assert(IsServerTP);
                NetLog.Assert(false);
            }

            //active_connection_id_limit：端点愿意存储的对端连接 ID 的最大数量。
            if (HasFlag(TransportParams.Flags, QUIC_TP_FLAG_ACTIVE_CONNECTION_ID_LIMIT))
            {
                RequiredTPLen += TlsTransportParamLength(QUIC_TP_ID_ACTIVE_CONNECTION_ID_LIMIT, QuicVarIntSize(TransportParams.ActiveConnectionIdLimit));
            }

            //initial_source_connection_id：端点在首次发送的 Initial 数据包中的源连接 ID。
            if (HasFlag(TransportParams.Flags, QUIC_TP_FLAG_INITIAL_SOURCE_CONNECTION_ID))
            {
                NetLog.Assert(TransportParams.InitialSourceConnectionID.Length <= QUIC_MAX_CONNECTION_ID_LENGTH_V1);
                RequiredTPLen += TlsTransportParamLength(QUIC_TP_ID_INITIAL_SOURCE_CONNECTION_ID, TransportParams.InitialSourceConnectionID.Length);
            }

            //retry_source_connection_id：服务器在 Retry 数据包中的源连接 ID。
            if (HasFlag(TransportParams.Flags, QUIC_TP_FLAG_RETRY_SOURCE_CONNECTION_ID))
            {
                NetLog.Assert(IsServerTP);
                NetLog.Assert(TransportParams.RetrySourceConnectionID.Length <= QUIC_MAX_CONNECTION_ID_LENGTH_V1);
                RequiredTPLen += TlsTransportParamLength(QUIC_TP_ID_RETRY_SOURCE_CONNECTION_ID, TransportParams.RetrySourceConnectionID.Length);
            }

            if (HasFlag(TransportParams.Flags, QUIC_TP_FLAG_MAX_DATAGRAM_FRAME_SIZE))
            {
                RequiredTPLen += TlsTransportParamLength(QUIC_TP_ID_MAX_DATAGRAM_FRAME_SIZE, QuicVarIntSize(TransportParams.MaxDatagramFrameSize));
            }
            if (HasFlag(TransportParams.Flags, QUIC_TP_FLAG_DISABLE_1RTT_ENCRYPTION))
            {
                RequiredTPLen += TlsTransportParamLength(QUIC_TP_ID_DISABLE_1RTT_ENCRYPTION, 0);
            }
            if (HasFlag(TransportParams.Flags, QUIC_TP_FLAG_VERSION_NEGOTIATION))
            {
                RequiredTPLen += TlsTransportParamLength(QUIC_TP_ID_VERSION_NEGOTIATION_EXT, TransportParams.VersionInfo.Length);
            }

            if (HasFlag(TransportParams.Flags, QUIC_TP_FLAG_MIN_ACK_DELAY))
            {
                NetLog.Assert(BoolOk(TransportParams.Flags & QUIC_TP_FLAG_MIN_ACK_DELAY) &&
                     (TransportParams.MinAckDelay) <= TransportParams.MaxAckDelay ||
                    !BoolOk(TransportParams.Flags & QUIC_TP_FLAG_MIN_ACK_DELAY) &&
                     TransportParams.MinAckDelay <= QUIC_TP_MAX_ACK_DELAY_DEFAULT);

                RequiredTPLen += TlsTransportParamLength(QUIC_TP_ID_MIN_ACK_DELAY, QuicVarIntSize(TransportParams.MinAckDelay));
            }

            if (HasFlag(TransportParams.Flags, QUIC_TP_FLAG_CIBIR_ENCODING))
            {
                RequiredTPLen += TlsTransportParamLength(QUIC_TP_ID_CIBIR_ENCODING, QuicVarIntSize(TransportParams.CibirLength) + QuicVarIntSize(TransportParams.CibirOffset));
            }

            if (HasFlag(TransportParams.Flags, QUIC_TP_FLAG_GREASE_QUIC_BIT))
            {
                RequiredTPLen += TlsTransportParamLength(QUIC_TP_ID_GREASE_QUIC_BIT, 0);
            }

            if (HasFlag(TransportParams.Flags, QUIC_TP_FLAG_RELIABLE_RESET_ENABLED))
            {
                RequiredTPLen += TlsTransportParamLength(QUIC_TP_ID_RELIABLE_RESET_ENABLED, 0);
            }

            if (HasFlag(TransportParams.Flags, (QUIC_TP_FLAG_TIMESTAMP_SEND_ENABLED | QUIC_TP_FLAG_TIMESTAMP_RECV_ENABLED)))
            {
                uint value = (TransportParams.Flags & (QUIC_TP_FLAG_TIMESTAMP_SEND_ENABLED | QUIC_TP_FLAG_TIMESTAMP_RECV_ENABLED)) >> QUIC_TP_FLAG_TIMESTAMP_SHIFT;
                RequiredTPLen += TlsTransportParamLength(QUIC_TP_ID_ENABLE_TIMESTAMP, QuicVarIntSize(value));
            }

            if (TestParam != null)
            {
                RequiredTPLen += TlsTransportParamLength(TestParam.Type, TestParam.Buffer.Length);
            }

            NetLog.Assert(RequiredTPLen <= ushort.MaxValue);
            if (RequiredTPLen > ushort.MaxValue)
            {
                return QUIC_SSBuffer.Empty;
            }

            //上面计算好了 Length，下面复制实际数据
            int TPLen = CxPlatTlsTPHeaderSize + RequiredTPLen;
            QUIC_SSBuffer TPBufBase = new byte[TPLen];
            QUIC_SSBuffer TPBuf = TPBufBase.Slice(CxPlatTlsTPHeaderSize);

            //original_destination_connection_id：客户端首次发送的 Initial 数据包中的目标连接 ID
            if (HasFlag(TransportParams.Flags, QUIC_TP_FLAG_ORIGINAL_DESTINATION_CONNECTION_ID))
            {
                NetLog.Assert(IsServerTP);
                NetLog.Assert(TransportParams.OriginalDestinationConnectionID.Length <= QUIC_MAX_CONNECTION_ID_LENGTH_V1);
                TPBuf = TlsWriteTransportParam(QUIC_TP_ID_ORIGINAL_DESTINATION_CONNECTION_ID, TransportParams.OriginalDestinationConnectionID, TPBuf);
            }

            //max_idle_timeout：最大空闲超时时间，以毫秒为单位
            if (HasFlag(TransportParams.Flags, QUIC_TP_FLAG_IDLE_TIMEOUT))
            {
                TPBuf = TlsWriteTransportParamVarInt(QUIC_TP_ID_IDLE_TIMEOUT, (ulong)TransportParams.IdleTimeout, TPBuf);
            }

            //stateless_reset_token：无状态重置令牌，用于验证无状态重置。
            if (HasFlag(TransportParams.Flags, QUIC_TP_FLAG_STATELESS_RESET_TOKEN))
            {
                NetLog.Assert(IsServerTP);
                TPBuf = TlsWriteTransportParam(QUIC_TP_ID_STATELESS_RESET_TOKEN, new QUIC_SSBuffer(TransportParams.StatelessResetToken, 0, QUIC_STATELESS_RESET_TOKEN_LENGTH), TPBuf);
            }

            //max_udp_payload_size：端点愿意接收的最大 UDP 负载大小。
            if (HasFlag(TransportParams.Flags, QUIC_TP_FLAG_MAX_UDP_PAYLOAD_SIZE))
            {
                TPBuf = TlsWriteTransportParamVarInt(QUIC_TP_ID_MAX_UDP_PAYLOAD_SIZE, (ulong)TransportParams.MaxUdpPayloadSize, TPBuf);
            }

            //initial_max_data：连接上最初可发送的最大数据量
            if (HasFlag(TransportParams.Flags, QUIC_TP_FLAG_INITIAL_MAX_DATA))
            {
                TPBuf = TlsWriteTransportParamVarInt(QUIC_TP_ID_INITIAL_MAX_DATA, (ulong)TransportParams.InitialMaxData, TPBuf);
            }

            //initial_max_stream_data_bidi_local：本地发起的双向流的初始流量控制限制
            if (HasFlag(TransportParams.Flags, QUIC_TP_FLAG_INITIAL_MAX_STRM_DATA_BIDI_LOCAL))
            {
                TPBuf = TlsWriteTransportParamVarInt(QUIC_TP_ID_INITIAL_MAX_STREAM_DATA_BIDI_LOCAL, (ulong)TransportParams.InitialMaxStreamDataBidiLocal, TPBuf);
            }

            //initial_max_stream_data_bidi_remote：对端发起的双向流的初始流量控制限制
            if (HasFlag(TransportParams.Flags, QUIC_TP_FLAG_INITIAL_MAX_STRM_DATA_BIDI_REMOTE))
            {
                TPBuf = TlsWriteTransportParamVarInt(QUIC_TP_ID_INITIAL_MAX_STREAM_DATA_BIDI_REMOTE, (ulong)TransportParams.InitialMaxStreamDataBidiRemote, TPBuf);
            }

            //initial_max_stream_data_uni：单向流的初始流量控制限制。
            if (HasFlag(TransportParams.Flags, QUIC_TP_FLAG_INITIAL_MAX_STRM_DATA_UNI))
            {
                TPBuf = TlsWriteTransportParamVarInt(QUIC_TP_ID_INITIAL_MAX_STREAM_DATA_UNI, (ulong)TransportParams.InitialMaxStreamDataUni, TPBuf);
            }

            //initial_max_streams_bidi：对端可以发起的双向流的最大数量。
            if (HasFlag(TransportParams.Flags, QUIC_TP_FLAG_INITIAL_MAX_STRMS_BIDI))
            {
                TPBuf = TlsWriteTransportParamVarInt(QUIC_TP_ID_INITIAL_MAX_STREAMS_BIDI, (ulong)TransportParams.InitialMaxBidiStreams, TPBuf);
            }

            //initial_max_streams_uni：对端可以发起的单向流的最大数量。
            if (HasFlag(TransportParams.Flags, QUIC_TP_FLAG_INITIAL_MAX_STRMS_UNI))
            {
                TPBuf = TlsWriteTransportParamVarInt(QUIC_TP_ID_INITIAL_MAX_STREAMS_UNI, (ulong)TransportParams.InitialMaxUniStreams, TPBuf);
            }

            //ack_delay_exponent：用于解码 ACK 帧中 ACK 延迟字段的指数。
            if (HasFlag(TransportParams.Flags, QUIC_TP_FLAG_ACK_DELAY_EXPONENT))
            {
                TPBuf = TlsWriteTransportParamVarInt(QUIC_TP_ID_ACK_DELAY_EXPONENT, (ulong)TransportParams.AckDelayExponent, TPBuf);
            }

            //max_ack_delay：端点延迟发送确认的最大时间。
            if (HasFlag(TransportParams.Flags, QUIC_TP_FLAG_MAX_ACK_DELAY))
            {
                TPBuf = TlsWriteTransportParamVarInt(QUIC_TP_ID_MAX_ACK_DELAY, (ulong)TransportParams.MaxAckDelay, TPBuf);
            }

            //disable_active_migration：如果端点不支持在握手期间使用的地址上进行活动连接迁移，则包含此参数。
            if (HasFlag(TransportParams.Flags, QUIC_TP_FLAG_DISABLE_ACTIVE_MIGRATION))
            {
                TPBuf = TlsWriteTransportParam(QUIC_TP_ID_DISABLE_ACTIVE_MIGRATION, QUIC_SSBuffer.Empty, TPBuf);
            }

            //preferred_address：首选地址，包含 IPv4 和 IPv6 地址、端口、连接 ID 和无状态重置令牌
            if (HasFlag(TransportParams.Flags, QUIC_TP_FLAG_PREFERRED_ADDRESS))
            {
                NetLog.Assert(IsServerTP);
                NetLog.Assert(false);
            }

            //active_connection_id_limit：端点愿意存储的对端连接 ID 的最大数量。
            if (HasFlag(TransportParams.Flags, QUIC_TP_FLAG_ACTIVE_CONNECTION_ID_LIMIT))
            {
                NetLog.Assert(TransportParams.ActiveConnectionIdLimit >= QUIC_TP_ACTIVE_CONNECTION_ID_LIMIT_MIN);
                TPBuf = TlsWriteTransportParamVarInt(QUIC_TP_ID_ACTIVE_CONNECTION_ID_LIMIT, (ulong)TransportParams.ActiveConnectionIdLimit, TPBuf);
            }

            //initial_source_connection_id：端点在首次发送的 Initial 数据包中的源连接 ID。
            if (HasFlag(TransportParams.Flags, QUIC_TP_FLAG_INITIAL_SOURCE_CONNECTION_ID))
            {
                TPBuf = TlsWriteTransportParam(QUIC_TP_ID_INITIAL_SOURCE_CONNECTION_ID, TransportParams.InitialSourceConnectionID, TPBuf);
            }

            if (HasFlag(TransportParams.Flags, QUIC_TP_FLAG_RETRY_SOURCE_CONNECTION_ID))
            {
                NetLog.Assert(IsServerTP);
                TPBuf = TlsWriteTransportParam(QUIC_TP_ID_RETRY_SOURCE_CONNECTION_ID, TransportParams.RetrySourceConnectionID, TPBuf);
            }

            if (HasFlag(TransportParams.Flags, QUIC_TP_FLAG_MAX_DATAGRAM_FRAME_SIZE))
            {
                TPBuf = TlsWriteTransportParamVarInt(QUIC_TP_ID_MAX_DATAGRAM_FRAME_SIZE, (ulong)TransportParams.MaxDatagramFrameSize, TPBuf);
            }

            if (HasFlag(TransportParams.Flags, QUIC_TP_FLAG_DISABLE_1RTT_ENCRYPTION))
            {
                TPBuf = TlsWriteTransportParam(QUIC_TP_ID_DISABLE_1RTT_ENCRYPTION, QUIC_SSBuffer.Empty, TPBuf);
            }

            if (HasFlag(TransportParams.Flags, QUIC_TP_FLAG_VERSION_NEGOTIATION))
            {
                TPBuf = TlsWriteTransportParam(QUIC_TP_ID_VERSION_NEGOTIATION_EXT, TransportParams.VersionInfo, TPBuf);
            }

            if (HasFlag(TransportParams.Flags, QUIC_TP_FLAG_MIN_ACK_DELAY))
            {
                TPBuf = TlsWriteTransportParamVarInt(QUIC_TP_ID_MIN_ACK_DELAY, (ulong)TransportParams.MinAckDelay, TPBuf);
            }

            if (HasFlag(TransportParams.Flags, QUIC_TP_FLAG_CIBIR_ENCODING))
            {
                int TPLength = QuicVarIntSize(TransportParams.CibirLength) + QuicVarIntSize(TransportParams.CibirOffset);
                TPBuf = QuicVarIntEncode(QUIC_TP_ID_CIBIR_ENCODING, TPBuf);
                TPBuf = QuicVarIntEncode(TPLength, TPBuf);
                TPBuf = QuicVarIntEncode(TransportParams.CibirLength, TPBuf);
                TPBuf = QuicVarIntEncode(TransportParams.CibirOffset, TPBuf);
            }
            if (HasFlag(TransportParams.Flags, QUIC_TP_FLAG_GREASE_QUIC_BIT))
            {
                TPBuf = TlsWriteTransportParam(QUIC_TP_ID_GREASE_QUIC_BIT, QUIC_SSBuffer.Empty, TPBuf);
            }
            if (HasFlag(TransportParams.Flags, QUIC_TP_FLAG_RELIABLE_RESET_ENABLED))
            {
                TPBuf = TlsWriteTransportParam(QUIC_TP_ID_RELIABLE_RESET_ENABLED, QUIC_SSBuffer.Empty, TPBuf);
            }

            if (HasFlag(TransportParams.Flags, (QUIC_TP_FLAG_TIMESTAMP_SEND_ENABLED | QUIC_TP_FLAG_TIMESTAMP_RECV_ENABLED)))
            {
                uint value = (TransportParams.Flags & (QUIC_TP_FLAG_TIMESTAMP_SEND_ENABLED | QUIC_TP_FLAG_TIMESTAMP_RECV_ENABLED)) >> QUIC_TP_FLAG_TIMESTAMP_SHIFT;
                TPBuf = TlsWriteTransportParamVarInt(QUIC_TP_ID_ENABLE_TIMESTAMP, value, TPBuf);
            }

            if (TestParam != null)
            {
                TPBuf = TlsWriteTransportParam(TestParam.Type, TestParam.Buffer, TPBuf);
            }

            NetLog.Assert(TPBuf.Length == 0);
            if (TPBuf.Length != 0)
            {
                NetLog.LogError("QuicCryptoTlsEncodeTransportParameters Error");
                return QUIC_SSBuffer.Empty;
            }
            return TPBufBase;
        }

        static QUIC_SSBuffer TlsWriteTransportParam(ulong Id, QUIC_SSBuffer Param, QUIC_SSBuffer Buffer)
        {
            Buffer = QuicVarIntEncode(Id, Buffer);
            Buffer = QuicVarIntEncode(Param.Length, Buffer);
            if (!Param.IsEmpty)
            {
                Param.CopyTo(Buffer);
                Buffer += Param.Length;
            }
            return Buffer;
        }

        static QUIC_SSBuffer TlsWriteTransportParamVarInt(ulong Id, ulong Value, QUIC_SSBuffer Buffer)
        {
            int Length = QuicVarIntSize(Value);
            Buffer = QuicVarIntEncode(Id, Buffer);
            Buffer = QuicVarIntEncode(Length, Buffer);
            Buffer = QuicVarIntEncode(Value, Buffer);
            return Buffer;
        }

        static ulong QuicCryptoTlsCopyTransportParameters(QUIC_TRANSPORT_PARAMETERS Source, QUIC_TRANSPORT_PARAMETERS Destination)
        {
            Destination = Source;
            if (BoolOk(Source.Flags & QUIC_TP_FLAG_VERSION_NEGOTIATION))
            {
                Destination.Flags |= QUIC_TP_FLAG_VERSION_NEGOTIATION;
                Source.VersionInfo.GetSpan().CopyTo(Destination.VersionInfo.GetSpan());
                Destination.VersionInfo.Length = Source.VersionInfo.Length;
            }
            return QUIC_STATUS_SUCCESS;
        }

    }
}
