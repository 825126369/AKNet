using System;

namespace AKNet.Udp5Quic.Common
{
    internal class QUIC_ACK_ECN_EX
    {
        public ulong ECT_0_Count;
        public ulong ECT_1_Count;
        public ulong CE_Count;
    }

    internal class QUIC_ACK_EX
    {
        public ulong LargestAcknowledged;
        public ulong AckDelay;
        public ulong AdditionalAckBlockCount;
        public ulong FirstAckBlock;
    }

    internal enum QUIC_FRAME_TYPE
    {
        QUIC_FRAME_PADDING = 0x0,
        QUIC_FRAME_PING = 0x1,
        QUIC_FRAME_ACK = 0x2,
        QUIC_FRAME_ACK_1 = 0x3,
        QUIC_FRAME_RESET_STREAM = 0x4,
        QUIC_FRAME_STOP_SENDING = 0x5,
        QUIC_FRAME_CRYPTO = 0x6,
        QUIC_FRAME_NEW_TOKEN = 0x7,
        QUIC_FRAME_STREAM = 0x8,
        QUIC_FRAME_STREAM_1 = 0x9,
        QUIC_FRAME_STREAM_2 = 0xa,
        QUIC_FRAME_STREAM_3 = 0xb,
        QUIC_FRAME_STREAM_4 = 0xc,
        QUIC_FRAME_STREAM_5 = 0xd,
        QUIC_FRAME_STREAM_6 = 0xe,
        QUIC_FRAME_STREAM_7 = 0xf,
        QUIC_FRAME_MAX_DATA = 0x10,
        QUIC_FRAME_MAX_STREAM_DATA = 0x11,
        QUIC_FRAME_MAX_STREAMS = 0x12, // to 0x13
        QUIC_FRAME_MAX_STREAMS_1 = 0x13,
        QUIC_FRAME_DATA_BLOCKED = 0x14,
        QUIC_FRAME_STREAM_DATA_BLOCKED = 0x15,
        QUIC_FRAME_STREAMS_BLOCKED = 0x16, // to 0x17
        QUIC_FRAME_STREAMS_BLOCKED_1 = 0x17,
        QUIC_FRAME_NEW_CONNECTION_ID = 0x18,
        QUIC_FRAME_RETIRE_CONNECTION_ID = 0x19,
        QUIC_FRAME_PATH_CHALLENGE = 0x1a,
        QUIC_FRAME_PATH_RESPONSE = 0x1b,
        QUIC_FRAME_CONNECTION_CLOSE = 0x1c, // to 0x1d
        QUIC_FRAME_CONNECTION_CLOSE_1 = 0x1d,
        QUIC_FRAME_HANDSHAKE_DONE = 0x1e,
        QUIC_FRAME_RELIABLE_RESET_STREAM = 0x21,
        QUIC_FRAME_DATAGRAM = 0x30,
        QUIC_FRAME_DATAGRAM_1 = 0x31,
        QUIC_FRAME_ACK_FREQUENCY = 0xaf,
        QUIC_FRAME_IMMEDIATE_ACK = 0xac,
        QUIC_FRAME_TIMESTAMP = 0x2f5,
        QUIC_FRAME_MAX_SUPPORTED
    }

    internal static partial class MSQuicFunc
    {
        static bool QuicErrorIsProtocolError(ulong ErrorCode)
        {
            return ErrorCode >= QUIC_ERROR_FLOW_CONTROL_ERROR && ErrorCode <= QUIC_ERROR_AEAD_LIMIT_REACHED;
        }

        static ArraySegment<byte> QuicUint8Encode(byte Value, byte[] Buffer)
        {
            Buffer[0] = Value;
            return Buffer.AsSpan().Slice(1);
        }

        static bool QuicUint8tDecode(int BufferLength, byte[] Buffer, ref int Offset, ref byte Value)
        {
            if (BufferLength < 1 + Offset)
            {
                return false;
            }

            Value = Buffer[Offset];
            Offset += 1;
            return true;
        }

        static bool QuicAckHeaderEncode(QUIC_ACK_EX Frame, QUIC_ACK_ECN_EX Ecn, int Offset, int BufferLength, byte[] Buffer)
        {
            int RequiredLength =
                1 +
                QuicVarIntSize(Frame.LargestAcknowledged) +
                QuicVarIntSize(Frame.AckDelay) +
                QuicVarIntSize(Frame.AdditionalAckBlockCount) +
                QuicVarIntSize(Frame.FirstAckBlock);

            if (BufferLength < Offset + RequiredLength)
            {
                return false;
            }

            Buffer = Buffer + Offset;
            Buffer = QuicUint8Encode(Ecn == null ? (byte)QUIC_FRAME_TYPE.QUIC_FRAME_ACK : (byte)(QUIC_FRAME_TYPE.QUIC_FRAME_ACK + 1), Buffer);
            Buffer = QuicVarIntEncode(Frame.LargestAcknowledged, Buffer);
            Buffer = QuicVarIntEncode(Frame.AckDelay, Buffer);
            Buffer = QuicVarIntEncode(Frame.AdditionalAckBlockCount, Buffer);
            QuicVarIntEncode(Frame.FirstAckBlock, Buffer);
            Offset += RequiredLength;

            return true;
        }
    }
}
