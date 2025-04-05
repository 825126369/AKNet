using AKNet.Common;
using AKNet.Udp4LinuxTcp.Common;
using System;
using System.IO;
using static System.Net.WebRequestMethods;

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

    internal class QUIC_RESET_STREAM_EX
    {
        public ulong StreamID;
        public ulong ErrorCode;
        public ulong FinalSize;
    }

    internal class QUIC_RELIABLE_RESET_STREAM_EX
    {
        public ulong StreamID;
        public ulong ErrorCode;
        public ulong FinalSize;
        public ulong ReliableSize;
    }

    internal class QUIC_STOP_SENDING_EX
    {
        public ulong StreamID;
        public ulong ErrorCode;
    }

    internal class QUIC_CRYPTO_EX
    {
        public ulong Offset;
        public ulong Length;
        public byte[] Data;
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

        static Span<byte> QuicUint8Encode(byte Value, Span<byte> Buffer)
        {
            Buffer[0] = (byte)Value;
            return Buffer.Slice(1);
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

        static bool QuicAckHeaderEncode(QUIC_ACK_EX Frame, QUIC_ACK_ECN_EX Ecn, Span<byte> Buffer)
        {
            int RequiredLength =
                1 +
                QuicVarIntSize(Frame.LargestAcknowledged) +
                QuicVarIntSize(Frame.AckDelay) +
                QuicVarIntSize(Frame.AdditionalAckBlockCount) +
                QuicVarIntSize(Frame.FirstAckBlock);

            if (Buffer.Length < RequiredLength)
            {
                return false;
            }

            Buffer = QuicUint8Encode(Ecn == null ? (byte)QUIC_FRAME_TYPE.QUIC_FRAME_ACK : (byte)(QUIC_FRAME_TYPE.QUIC_FRAME_ACK + 1), Buffer);
            Buffer = QuicVarIntEncode(Frame.LargestAcknowledged, Buffer);
            Buffer = QuicVarIntEncode(Frame.AckDelay, Buffer);
            Buffer = QuicVarIntEncode(Frame.AdditionalAckBlockCount, Buffer);
            QuicVarIntEncode(Frame.FirstAckBlock, Buffer);
            return true;
        }

        static bool QuicResetStreamFrameEncode(QUIC_RESET_STREAM_EX Frame, ref int Offset, ref int BufferLength, Span<byte> Buffer)
        {
            ushort RequiredLength =
                (byte)(sizeof(byte)) + 
                QuicVarIntSize(Frame.ErrorCode) +
                QuicVarIntSize(Frame.StreamID) +
                QuicVarIntSize(Frame.FinalSize);

            if (Buffer.Length < RequiredLength) 
            {
                return false;
            }

            Buffer = QuicUint8Encode((byte)QUIC_FRAME_TYPE.QUIC_FRAME_RESET_STREAM, Buffer);
            Buffer = QuicVarIntEncode(Frame.StreamID, Buffer);
            Buffer = QuicVarIntEncode(Frame.ErrorCode, Buffer);
            QuicVarIntEncode(Frame.FinalSize, Buffer);
            return true;
        }

        static bool QuicResetStreamFrameDecode(int BufferLength, byte[] Buffer, ref int Offset, QUIC_RESET_STREAM_EX Frame)
        {
            if (!QuicVarIntDecode(BufferLength, Buffer, ref Offset, ref Frame.StreamID) ||
                !QuicVarIntDecode(BufferLength, Buffer, ref Offset, ref Frame.ErrorCode) ||
                !QuicVarIntDecode(BufferLength, Buffer, ref Offset, ref Frame.FinalSize))
            {
                return false;
            }
            return true;
        }

        static bool QuicReliableResetFrameEncode(QUIC_RELIABLE_RESET_STREAM_EX Frame, Span<byte> Buffer)
        {
            int RequiredLength =
                sizeof(byte) +
                QuicVarIntSize(Frame.ErrorCode) +
                QuicVarIntSize(Frame.StreamID) +
                QuicVarIntSize(Frame.FinalSize) +
                QuicVarIntSize(Frame.ReliableSize);

            if (Buffer.Length < RequiredLength)
            {
                return false;
            }

            Buffer = QuicUint8Encode((byte)QUIC_FRAME_TYPE.QUIC_FRAME_RELIABLE_RESET_STREAM, Buffer);
            Buffer = QuicVarIntEncode(Frame.StreamID, Buffer);
            Buffer = QuicVarIntEncode(Frame.ErrorCode, Buffer);
            Buffer = QuicVarIntEncode(Frame.FinalSize, Buffer);
            QuicVarIntEncode(Frame.ReliableSize, Buffer);
            return true;
        }

        static bool QuicReliableResetFrameDecode(int BufferLength, byte[] Buffer, int Offset, QUIC_RELIABLE_RESET_STREAM_EX Frame)
        {
            if (!QuicVarIntDecode(BufferLength, Buffer, ref Offset, ref Frame.StreamID) ||
                !QuicVarIntDecode(BufferLength, Buffer, ref Offset, ref Frame.ErrorCode) ||
                !QuicVarIntDecode(BufferLength, Buffer, ref Offset, ref Frame.FinalSize) ||
                !QuicVarIntDecode(BufferLength, Buffer, ref Offset, ref Frame.ReliableSize))
            {
                return false;
            }
            return true;
        }

        static bool QuicStopSendingFrameEncode(QUIC_STOP_SENDING_EX Frame, Span<byte> Buffer)
        {
            int RequiredLength =
                sizeof(byte) +
                QuicVarIntSize(Frame.StreamID) +
                QuicVarIntSize(Frame.ErrorCode);

            if (Buffer.Length < RequiredLength)
            {
                return false;
            }

            Buffer = QuicUint8Encode((byte)QUIC_FRAME_TYPE.QUIC_FRAME_STOP_SENDING, Buffer);
            Buffer = QuicVarIntEncode(Frame.StreamID, Buffer);
            QuicVarIntEncode(Frame.ErrorCode, Buffer);

            return true;
        }

        static bool QuicStopSendingFrameDecode(int BufferLength, byte[] Buffer, int Offset, QUIC_STOP_SENDING_EX Frame)
        {
            if (!QuicVarIntDecode(BufferLength, Buffer, ref Offset, ref Frame.StreamID) ||
                !QuicVarIntDecode(BufferLength, Buffer, ref Offset, ref Frame.ErrorCode))
            {
                return false;
            }
            return true;
        }

        static bool QuicCryptoFrameEncode(QUIC_CRYPTO_EX Frame,int Offset,int BufferLength, Span<byte> Buffer)
        {
            NetLog.Assert(Frame.Length < ushort.MaxValue);
            int RequiredLength =
                sizeof(byte) +
                QuicVarIntSize(Frame.Offset) +
                QuicVarIntSize(Frame.Length) +
                (int)Frame.Length;

            if (BufferLength< Offset + RequiredLength) 
            {
                return false;
            }

            Buffer = QuicUint8Encode((byte)QUIC_FRAME_TYPE.QUIC_FRAME_CRYPTO, Buffer);
            Buffer = QuicVarIntEncode(Frame.Offset, Buffer);
            Buffer = QuicVarIntEncode(Frame.Length, Buffer);
            Frame.Data.CopyTo(Buffer);
            EndianBitConverter.SetBytes(Buffer, Offset, Frame.Data);
            return true;
        }

        static bool QuicCryptoFrameDecode(Span<byte> Buffer, ref int Offset, QUIC_CRYPTO_EX Frame)
        {
            if (!QuicVarIntDecode(ref Buffer, ref Frame.Offset) ||
                !QuicVarIntDecode(ref Buffer, ref Frame.Length) ||
                Buffer.Length < (int)Frame.Length)
            {
                return false;
            }

            Frame.Data = Buffer.ToArray();
            return true;
        }

    }
}
