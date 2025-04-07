using AKNet.Common;
using System;

namespace AKNet.Udp5Quic.Common
{
    internal static partial class MSQuicFunc
    {
        public const int TLS1_PROTOCOL_VERSION = 0x0301;
        public const int TLS_MESSAGE_HEADER_LENGTH = 4;
        public const int TLS_RANDOM_LENGTH = 32;
        public const int TLS_SESSION_ID_LENGTH = 32;

        public const byte TlsExt_ServerName = 0x00;
        public const byte TlsExt_AppProtocolNegotiation   = 0x10;
        public const byte TlsExt_SessionTicket            = 0x23;

        public const byte TlsHandshake_ClientHello = 0x01;

        static ushort TlsReadUint16(ReadOnlySpan<byte> Buffer)
        {
            return (ushort)((Buffer[0] << 8) + Buffer[1]);
        }

        static uint TlsReadUint24(ReadOnlySpan<byte> Buffer)
        {
            return
                (((uint)Buffer[0] << 16) +
                 ((uint)Buffer[1] << 8) +
                  (uint)Buffer[2]);
        }

        static ulong QuicCryptoTlsReadInitial(QUIC_CONNECTION Connection, ReadOnlySpan<byte> Buffer, QUIC_NEW_CONNECTION_INFO Info)
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

        static ulong QuicCryptoTlsReadClientHello(QUIC_CONNECTION Connection, ReadOnlySpan<byte> Buffer, QUIC_NEW_CONNECTION_INFO Info)
        {
            if (Buffer.Length < sizeof(ushort) || TlsReadUint16(Buffer) < TLS1_PROTOCOL_VERSION)
            {
                return QUIC_STATUS_INVALID_PARAMETER;
            }
            
            Buffer = Buffer.Slice(sizeof(ushort));

            if (Buffer.Length < TLS_RANDOM_LENGTH)
            {
                return QUIC_STATUS_INVALID_PARAMETER;
            }

            Buffer = Buffer.Slice(TLS_RANDOM_LENGTH);

            if (Buffer.Length < sizeof(byte) || Buffer[0] > TLS_SESSION_ID_LENGTH ||  Buffer.Length < sizeof(byte) + Buffer[0])
            {
                return QUIC_STATUS_INVALID_PARAMETER;
            }

            Buffer = Buffer.Slice(sizeof(byte) + Buffer[0]);
                
            if (Buffer.Length < sizeof(ushort))
            {
                return QUIC_STATUS_INVALID_PARAMETER;
            }

            int Len = TlsReadUint16(Buffer);
            if ((Len % 2 != 0) || Buffer.Length < (sizeof(ushort) + Len))
            {
                return QUIC_STATUS_INVALID_PARAMETER;
            }

            Buffer = Buffer.Slice(sizeof(ushort) + Len);

            if (Buffer.Length < sizeof(byte) || Buffer[0] < 1 || Buffer.Length < sizeof(byte) + Buffer[0])
            {
                return QUIC_STATUS_INVALID_PARAMETER;
            }

            Buffer = Buffer.Slice(sizeof(byte) + Buffer[0]);
            
            if (Buffer.Length < sizeof(ushort))
            {
                return QUIC_STATUS_SUCCESS;
            }
            Len = TlsReadUint16(Buffer);
            if (Buffer.Length < (sizeof(ushort) + Len))
            {
                return QUIC_STATUS_INVALID_PARAMETER;
            }

            return QuicCryptoTlsReadExtensions(Connection, Buffer.Slice(sizeof(ushort), Len), Info);
        }

        static ulong QuicCryptoTlsReadExtensions(QUIC_CONNECTION Connection, ReadOnlySpan<byte> Buffer, QUIC_NEW_CONNECTION_INFO Info)
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
                    ulong Status = QuicCryptoTlsReadSniExtension(Connection, Buffer, ExtLen, Info);
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
                    ulong Status = QuicCryptoTlsReadAlpnExtension(Connection, Buffer, ExtLen, Info);
                    if (QUIC_FAILED(Status))
                    {
                        return Status;
                    }
                    FoundALPN = true;

                }
                else if (Connection.Stats.QuicVersion != QUIC_VERSION_DRAFT_29)
                {
                    if (ExtType == TLS_EXTENSION_TYPE_QUIC_TRANSPORT_PARAMETERS)
                    {
                        if (FoundTransportParameters)
                        {
                            return QUIC_STATUS_INVALID_PARAMETER;
                        }
                        if (!QuicCryptoTlsDecodeTransportParameters(
                                Connection,
                                FALSE,
                                Buffer,
                                ExtLen,
                                &Connection->PeerTransportParams))
                        {
                            return QUIC_STATUS_INVALID_PARAMETER;
                        }
                        FoundTransportParameters = TRUE;
                    }

                }
                else
                {
                    if (ExtType == TLS_EXTENSION_TYPE_QUIC_TRANSPORT_PARAMETERS_DRAFT)
                    {
                        if (FoundTransportParameters)
                        {
                            QuicTraceEvent(
                                ConnError,
                                "[conn][%p] ERROR, %s.",
                                Connection,
                                "Duplicate QUIC (draft) TP extension present");
                            return QUIC_STATUS_INVALID_PARAMETER;
                        }
                        if (!QuicCryptoTlsDecodeTransportParameters(
                                Connection,
                                FALSE,
                                Buffer,
                                ExtLen,
                                &Connection->PeerTransportParams))
                        {
                            return QUIC_STATUS_INVALID_PARAMETER;
                        }
                        FoundTransportParameters = TRUE;
                    }
                }

                BufferLength -= ExtLen;
                Buffer += ExtLen;
            }

            if (!FoundTransportParameters)
            {
                QuicTraceEvent(
                    ConnError,
                    "[conn][%p] ERROR, %s.",
                    Connection,
                    "No QUIC TP extension present");
                return QUIC_STATUS_INVALID_PARAMETER;
            }

            return QUIC_STATUS_SUCCESS;
        }

        static ulong QuicCryptoTlsReadAlpnExtension(QUIC_CONNECTION Connection, ReadOnlySpan<byte> Buffer, QUIC_NEW_CONNECTION_INFO Info)
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
            Info.ClientAlpnListLength = Buffer.Length;

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

    }
}
