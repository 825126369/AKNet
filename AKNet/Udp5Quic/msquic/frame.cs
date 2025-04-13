using AKNet.Common;
using AKNet.Udp4LinuxTcp.Common;
using System;
using System.Drawing;
using System.Text;

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
        public int Offset;
        public int Length;
        public Memory<byte> Data;
    }

    internal class QUIC_TIMESTAMP_EX
    {
        public ulong Timestamp;
    }

    internal class QUIC_ACK_BLOCK_EX
    {
        public ulong Gap;
        public ulong AckBlock;

    }

    internal class QUIC_CONNECTION_CLOSE_EX
    {
        public bool ApplicationClosed;
        public ulong ErrorCode;
        public byte  FrameType;
        public int ReasonPhraseLength;
        public string ReasonPhrase;     // UTF-8 string.
    }

    internal class QUIC_PATH_CHALLENGE_EX
    {
        public readonly byte[] Data = new byte[8];
    }

    internal class QUIC_DATA_BLOCKED_EX
    {
        public ulong DataLimit;
    }

    internal class QUIC_MAX_DATA_EX
    {
        public ulong MaximumData;
    }

    internal class QUIC_MAX_STREAMS_EX
    {
        public bool BidirectionalStreams;
        public long MaximumStreams;
    }

    internal class QUIC_STREAMS_BLOCKED_EX
    {
        public bool BidirectionalStreams;
        public long StreamLimit;

    }

    internal class QUIC_NEW_CONNECTION_ID_EX
    {
        public int Length;
        public ulong Sequence;
        public ulong RetirePriorTo;
        public byte[] Buffer = new byte[MSQuicFunc.QUIC_MAX_CONNECTION_ID_LENGTH_V1 + MSQuicFunc.QUIC_STATELESS_RESET_TOKEN_LENGTH];
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

        static bool QuicTimestampFrameEncode(QUIC_TIMESTAMP_EX Frame, ref int Offset, int BufferLength, Span<byte> Buffer)
        {
            int RequiredLength = QuicVarIntSize((ulong)QUIC_FRAME_TYPE.QUIC_FRAME_TIMESTAMP) + QuicVarIntSize((ulong)Frame.Timestamp);
            if (BufferLength < Offset + RequiredLength) 
            {
                return false;
            }

            Buffer = Buffer.Slice(Offset);
            Buffer = QuicVarIntEncode((ulong)QUIC_FRAME_TYPE.QUIC_FRAME_TIMESTAMP, Buffer);
            QuicVarIntEncode(Frame.Timestamp, Buffer);
            Offset += RequiredLength;
            return true;
        }

        static bool QuicAckHeaderEncode(QUIC_ACK_EX Frame, QUIC_ACK_ECN_EX Ecn, ref int Offset, int BufferLength, Span<byte> Buffer)
        {
            int RequiredLength = sizeof(byte) +
                QuicVarIntSize(Frame.LargestAcknowledged) +
                QuicVarIntSize(Frame.AckDelay) +
                QuicVarIntSize(Frame.AdditionalAckBlockCount) +
                QuicVarIntSize(Frame.FirstAckBlock);

            if (BufferLength < Offset + RequiredLength)
            {
                return false;
            }

            Buffer = Buffer.Slice(Offset);
            Buffer = QuicUint8Encode(Ecn == null ? (byte)QUIC_FRAME_TYPE.QUIC_FRAME_ACK : (byte)QUIC_FRAME_TYPE.QUIC_FRAME_ACK_1, Buffer);
            Buffer = QuicVarIntEncode(Frame.LargestAcknowledged, Buffer);
            Buffer = QuicVarIntEncode(Frame.AckDelay, Buffer);
            Buffer = QuicVarIntEncode(Frame.AdditionalAckBlockCount, Buffer);
            QuicVarIntEncode(Frame.FirstAckBlock, Buffer);
            Offset += RequiredLength;
            return true;
        }

        static bool QuicAckBlockEncode(QUIC_ACK_BLOCK_EX Block,ref int Offset, int BufferLength, Span<byte> Buffer)
        {
            int RequiredLength = QuicVarIntSize(Block.Gap) + QuicVarIntSize(Block.AckBlock);
            if (BufferLength< Offset + RequiredLength) 
            {
                return false;
            }

            Buffer = Buffer.Slice(Offset);
            Buffer = QuicVarIntEncode(Block.Gap, Buffer);
            QuicVarIntEncode(Block.AckBlock, Buffer);
            Offset += RequiredLength;
            return true;
        }

        static bool QuicAckEcnEncode(QUIC_ACK_ECN_EX Ecn, ref int Offset, int BufferLength, Span<byte> Buffer)
        {
            int RequiredLength = QuicVarIntSize(Ecn.ECT_0_Count) + QuicVarIntSize(Ecn.ECT_1_Count) + QuicVarIntSize(Ecn.CE_Count);
            if (BufferLength < Offset + RequiredLength)
            {
                return false;
            }

            Buffer = Buffer.Slice(Offset);
            Buffer = QuicVarIntEncode(Ecn.ECT_0_Count, Buffer);
            Buffer = QuicVarIntEncode(Ecn.ECT_1_Count, Buffer);
            QuicVarIntEncode(Ecn.CE_Count, Buffer);
            Offset += RequiredLength;
            return true;
        }

        static bool QuicAckFrameEncode(QUIC_RANGE AckBlocks, ulong AckDelay, QUIC_ACK_ECN_EX Ecn, ref int Offset, int BufferLength, Span<byte> Buffer)
        {
            int i = QuicRangeSize(AckBlocks) - 1;

            QUIC_SUBRANGE LastSub = QuicRangeGet(AckBlocks, i);
            ulong Largest = QuicRangeGetHigh(LastSub);
            ulong Count = LastSub.Count;

            QUIC_ACK_EX Frame = new QUIC_ACK_EX()
            {
                LargestAcknowledged = Largest,                // LargestAcknowledged
                AckDelay = AckDelay,               // AckDelay
                AdditionalAckBlockCount = (ulong)i,                      // AdditionalAckBlockCount
                FirstAckBlock = Count - 1               // FirstAckBlock
            };

            if (!QuicAckHeaderEncode(Frame, Ecn, ref Offset, BufferLength, Buffer))
            {
                return false;
            }
            
            while (i != 0)
            {
                NetLog.Assert(Largest >= Count);
                Largest -= Count;

                QUIC_SUBRANGE Next = QuicRangeGet(AckBlocks, i - 1);
                ulong NextLargest = QuicRangeGetHigh(Next);
                Count = Next.Count;

                NetLog.Assert(Largest > NextLargest);
                NetLog.Assert(Count > 0);

                QUIC_ACK_BLOCK_EX Block = new QUIC_ACK_BLOCK_EX()
                {
                    Gap = (Largest - NextLargest) - 1,
                    AckBlock = Count - 1
                };

                if (!QuicAckBlockEncode(Block, ref Offset, BufferLength, Buffer))
                {
                    NetLog.Assert(false);
                }

                Largest = NextLargest;
                i--;
            }

            if (Ecn != null)
            {
                if (!QuicAckEcnEncode(Ecn, ref Offset, BufferLength, Buffer))
                {
                    return false;
                }
            }

            return true;
        }

        static bool QuicConnCloseFrameEncode(QUIC_CONNECTION_CLOSE_EX Frame, ref int Offset, int BufferLength, Span<byte> Buffer)
        {
            int RequiredLength =
                sizeof(byte) +     // Type
                QuicVarIntSize(Frame.ErrorCode) +
                (Frame.ApplicationClosed ? 0 : QuicVarIntSize(Frame.FrameType)) +
                QuicVarIntSize((ulong)Frame.ReasonPhraseLength) +
                (int)Frame.ReasonPhraseLength;

            if (BufferLength < Offset + RequiredLength)
            {
                return false;
            }

            Buffer = Buffer.Slice(Offset);
            Buffer = QuicUint8Encode(Frame.ApplicationClosed ? (byte)QUIC_FRAME_TYPE.QUIC_FRAME_CONNECTION_CLOSE_1 : (byte)QUIC_FRAME_TYPE.QUIC_FRAME_CONNECTION_CLOSE, Buffer);
            Buffer = QuicVarIntEncode(Frame.ErrorCode, Buffer);
            if (!Frame.ApplicationClosed)
            {
                Buffer = QuicVarIntEncode(Frame.FrameType, Buffer);
            }
            Buffer = QuicVarIntEncode((ulong)Frame.ReasonPhraseLength, Buffer);
            if (Frame.ReasonPhraseLength != 0)
            {
                Encoding.ASCII.GetBytes(Frame.ReasonPhrase).CopyTo(Buffer);
            }
            Offset += RequiredLength;
            return true;
        }

        static bool QuicPathChallengeFrameEncode(QUIC_FRAME_TYPE FrameType, QUIC_PATH_CHALLENGE_EX Frame, ref int Offset, int BufferLength, Span<byte> Buffer)
        {
            int RequiredLength = sizeof(byte) + Frame.Data.Length;

            if (BufferLength < Offset + RequiredLength)
            {
                return false;
            }

            Buffer = Buffer.Slice(Offset);
            Buffer = QuicUint8Encode((byte)FrameType, Buffer);
            Frame.Data.CopyTo(Buffer);
            Offset += RequiredLength;
            return true;
        }

        static bool QuicDataBlockedFrameEncode(QUIC_DATA_BLOCKED_EX Frame, ref int Offset, int BufferLength, Span<byte> Buffer)
        {
            int RequiredLength = sizeof(byte) + QuicVarIntSize(Frame.DataLimit);
            if (BufferLength < Offset + RequiredLength)
            {
                return false;
            }

            Buffer = Buffer.Slice(Offset);
            Buffer = QuicUint8Encode((byte)QUIC_FRAME_TYPE.QUIC_FRAME_DATA_BLOCKED, Buffer);
            QuicVarIntEncode(Frame.DataLimit, Buffer);
            Offset += RequiredLength;
            return true;
        }

        static bool QuicMaxDataFrameEncode(QUIC_MAX_DATA_EX Frame, ref int Offset, int BufferLength, Span<byte> Buffer)
        {
            int RequiredLength = sizeof(byte) + QuicVarIntSize(Frame.MaximumData);
            if (BufferLength < Offset + RequiredLength)
            {
                return false;
            }

            Buffer = Buffer.Slice(Offset);
            Buffer = QuicUint8Encode((byte)QUIC_FRAME_TYPE.QUIC_FRAME_MAX_DATA, Buffer);
            QuicVarIntEncode(Frame.MaximumData, Buffer);
            Offset += RequiredLength;
            return true;
        }

        static bool QuicMaxStreamsFrameEncode(QUIC_MAX_STREAMS_EX Frame, ref int Offset,int BufferLength, Span<byte> Buffer)
        {
            int RequiredLength = sizeof(byte) +  QuicVarIntSize((ulong)Frame.MaximumStreams);

            if (BufferLength < Offset + RequiredLength)
            {
                return false;
            }

            Buffer = Buffer.Slice(Offset);
            Buffer = QuicUint8Encode(Frame.BidirectionalStreams ? (byte)QUIC_FRAME_TYPE.QUIC_FRAME_MAX_STREAMS : (byte)QUIC_FRAME_TYPE.QUIC_FRAME_MAX_STREAMS_1, Buffer);
            QuicVarIntEncode((ulong)Frame.MaximumStreams, Buffer);
            Offset += RequiredLength;
            return true;
        }

        static bool QuicStreamsBlockedFrameEncode(QUIC_STREAMS_BLOCKED_EX Frame, ref int Offset, int BufferLength, Span<byte> Buffer)
        {
            int RequiredLength = sizeof(byte) + QuicVarIntSize((ulong)Frame.StreamLimit);

            if (BufferLength < Offset + RequiredLength)
            {
                return false;
            }

            Buffer = Buffer.Slice(Offset);
            Buffer = QuicUint8Encode(Frame.BidirectionalStreams ? (byte)QUIC_FRAME_TYPE.QUIC_FRAME_STREAMS_BLOCKED : (byte)QUIC_FRAME_TYPE.QUIC_FRAME_STREAMS_BLOCKED_1, Buffer);
            QuicVarIntEncode((ulong)Frame.StreamLimit, Buffer);
            Offset += RequiredLength;
            return true;
        }

    }
}
