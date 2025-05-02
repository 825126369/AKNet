﻿using AKNet.Common;
using System;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using static System.Net.WebRequestMethods;

namespace AKNet.Udp5Quic.Common
{
    internal class QUIC_ACK_ECN_EX
    {
        public ulong ECT_0_Count;
        public ulong ECT_1_Count;
        public ulong CE_Count;
    }

    internal struct QUIC_ACK_EX
    {
        public int LargestAcknowledged;
        public long AckDelay;
        public int AdditionalAckBlockCount;
        public int FirstAckBlock;
    }

    internal struct QUIC_RESET_STREAM_EX
    {
        public ulong StreamID;
        public ulong ErrorCode;
        public int FinalSize;
    }

    internal struct QUIC_RELIABLE_RESET_STREAM_EX
    {
        public ulong StreamID;
        public ulong ErrorCode;
        public int FinalSize;
        public int ReliableSize;
    }

    internal struct QUIC_STOP_SENDING_EX
    {
        public ulong StreamID;
        public ulong ErrorCode;
    }

    internal struct QUIC_CRYPTO_EX
    {
        public QUIC_BUFFER Data;
    }

    internal struct QUIC_TIMESTAMP_EX
    {
        public long Timestamp;
    }

    internal struct QUIC_ACK_BLOCK_EX
    {
        public int Gap;
        public int AckBlock;

    }

    internal struct QUIC_CONNECTION_CLOSE_EX
    {
        public bool ApplicationClosed;
        public ulong ErrorCode;
        public byte FrameType;
        public string ReasonPhrase;     // UTF-8 string.
    }

    internal struct QUIC_PATH_CHALLENGE_EX
    {
        public readonly byte[] Data;
        public QUIC_PATH_CHALLENGE_EX(int _ = 0)
        {
            Data = new byte[8];
        }
    }

    internal struct QUIC_DATA_BLOCKED_EX
    {
        public ulong DataLimit;
    }

    internal struct QUIC_MAX_DATA_EX
    {
        public int MaximumData;
    }

    internal struct QUIC_MAX_STREAMS_EX
    {
        public bool BidirectionalStreams;
        public int MaximumStreams;
    }

    internal struct QUIC_STREAMS_BLOCKED_EX
    {
        public bool BidirectionalStreams;
        public long StreamLimit;

    }

    internal struct QUIC_NEW_CONNECTION_ID_EX
    {
        public ulong Sequence;
        public ulong RetirePriorTo;
        public readonly QUIC_BUFFER Buffer;

        public QUIC_NEW_CONNECTION_ID_EX(int _ = 0)
        {
            Sequence = 0;
            RetirePriorTo = 0;
            Buffer = new QUIC_BUFFER(MSQuicFunc.QUIC_MAX_CONNECTION_ID_LENGTH_V1 + MSQuicFunc.QUIC_STATELESS_RESET_TOKEN_LENGTH);
        }
    }

    internal struct QUIC_RETIRE_CONNECTION_ID_EX
    {
        public ulong Sequence;
    }

    internal class QUIC_ACK_FREQUENCY_EX
    {
        public ulong SequenceNumber;
        public ulong PacketTolerance;
        public long UpdateMaxAckDelay; // In microseconds (us)
        public bool IgnoreOrder;
        public bool IgnoreCE;
    }

    internal struct QUIC_ACK_FREQUENCY_EXTRAS
    {
        public bool IgnoreOrder;
        public bool IgnoreCE;
        public byte Reserved;
        public byte Value;
    }

    internal struct QUIC_DATAGRAM_FRAME_TYPE
    {
        public byte LEN;
        public byte FrameType; // Always 0b0011000
        public QUIC_FRAME_TYPE Type;
    }

    internal struct QUIC_NEW_TOKEN_EX
    {
        public int Offset;
        public int TokenLength;
        public byte[] Token;
    }

    internal struct QUIC_MAX_STREAM_DATA_EX
    {
        public ulong StreamID;
        public int MaximumData;
    }

    internal struct QUIC_STREAM_DATA_BLOCKED_EX
    {
        public ulong StreamID;
        public int StreamDataLimit;
    }

    internal struct QUIC_STREAM_EX
    {
        public bool Fin;
        public bool ExplicitLength;
        public ulong StreamID;
        public QUIC_BUFFER Data;
    }

    internal struct QUIC_STREAM_FRAME_TYPE
    {
        public bool FIN;
        public bool LEN;
        public bool OFF;
        public byte FrameType;
        public byte Type;
    }

    internal struct QUIC_DATAGRAM_EX
    {
        public bool ExplicitLength;
        public QUIC_BUFFER Data;
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

        static bool QUIC_FRAME_IS_KNOWN(QUIC_FRAME_TYPE X)
        {
            return X <= QUIC_FRAME_TYPE.QUIC_FRAME_HANDSHAKE_DONE ||
                X >= QUIC_FRAME_TYPE.QUIC_FRAME_DATAGRAM && X <= QUIC_FRAME_TYPE.QUIC_FRAME_DATAGRAM_1 ||
              X == QUIC_FRAME_TYPE.QUIC_FRAME_ACK_FREQUENCY || X == QUIC_FRAME_TYPE.QUIC_FRAME_IMMEDIATE_ACK ||
              X == QUIC_FRAME_TYPE.QUIC_FRAME_RELIABLE_RESET_STREAM ||
              X == QUIC_FRAME_TYPE.QUIC_FRAME_TIMESTAMP;
        }

        static QUIC_SSBuffer QuicUint8Encode(byte Value, QUIC_SSBuffer Buffer)
        {
            Buffer[0] = (byte)Value;
            return Buffer.Slice(1);
        }

        static bool QuicUint8tDecode(ref QUIC_SSBuffer Buffer, ref byte Value)
        {
            if (Buffer.Length < 1)
            {
                return false;
            }

            Value = Buffer[0];
            Buffer = Buffer.Slice(1);
            return true;
        }

        static bool QuicAckHeaderEncode(QUIC_ACK_EX Frame, QUIC_ACK_ECN_EX Ecn, QUIC_SSBuffer Buffer)
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

        static bool QuicResetStreamFrameEncode(QUIC_RESET_STREAM_EX Frame, QUIC_SSBuffer Buffer)
        {
            int RequiredLength = sizeof(byte) + QuicVarIntSize(Frame.ErrorCode) + QuicVarIntSize(Frame.StreamID) + QuicVarIntSize(Frame.FinalSize);
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

        static bool QuicResetStreamFrameDecode(ref QUIC_SSBuffer Buffer, ref QUIC_RESET_STREAM_EX Frame)
        {
            if (!QuicVarIntDecode(ref Buffer, ref Frame.StreamID) ||
                !QuicVarIntDecode(ref Buffer, ref Frame.ErrorCode) ||
                !QuicVarIntDecode(ref Buffer, ref Frame.FinalSize))
            {
                return false;
            }
            return true;
        }

        static bool QuicReliableResetFrameEncode(QUIC_RELIABLE_RESET_STREAM_EX Frame, QUIC_SSBuffer Buffer)
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
            Buffer = QuicVarIntEncode(Frame.ReliableSize, Buffer);
            return true;
        }

        static bool QuicReliableResetFrameDecode(ref QUIC_SSBuffer Buffer, QUIC_RELIABLE_RESET_STREAM_EX Frame)
        {
            if (!QuicVarIntDecode(ref Buffer, ref Frame.StreamID) || !QuicVarIntDecode(ref Buffer, ref Frame.ErrorCode) ||
                !QuicVarIntDecode(ref Buffer, ref Frame.FinalSize) ||
                !QuicVarIntDecode(ref Buffer, ref Frame.ReliableSize))
            {
                return false;
            }
            return true;
        }

        static bool QuicStopSendingFrameEncode(QUIC_STOP_SENDING_EX Frame, QUIC_SSBuffer Buffer)
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

        static bool QuicStopSendingFrameDecode(ref QUIC_SSBuffer Buffer, ref QUIC_STOP_SENDING_EX Frame)
        {
            if (!QuicVarIntDecode(ref Buffer, ref Frame.StreamID) ||
                !QuicVarIntDecode(ref Buffer, ref Frame.ErrorCode))
            {
                return false;
            }
            return true;
        }

        static bool QuicCryptoFrameEncode(QUIC_CRYPTO_EX Frame, QUIC_SSBuffer Buffer)
        {
            NetLog.Assert(Frame.Data.Length < ushort.MaxValue);
            int RequiredLength =
                sizeof(byte) +
                QuicVarIntSize(Frame.Data.Offset) +
                QuicVarIntSize(Frame.Data.Length) +
                (int)Frame.Data.Length;

            if (Buffer.Length < RequiredLength)
            {
                return false;
            }

            Buffer = QuicUint8Encode((byte)QUIC_FRAME_TYPE.QUIC_FRAME_CRYPTO, Buffer);
            Buffer = QuicVarIntEncode(Frame.Data.Offset, Buffer);
            Buffer = QuicVarIntEncode(Frame.Data.Length, Buffer);
            Frame.Data.GetSpan().CopyTo(Buffer.GetSpan());
            Buffer += Frame.Data.Length;
            return true;
        }

        static bool QuicCryptoFrameDecode(QUIC_SSBuffer Buffer, QUIC_CRYPTO_EX Frame)
        {
            if (!QuicVarIntDecode(ref Buffer, ref Frame.Data.Offset) ||
                !QuicVarIntDecode(ref Buffer, ref Frame.Data.Length) ||
                Buffer.Length < (int)Frame.Data.Length)
            {
                return false;
            }

            Frame.Data.Buffer = Buffer.Buffer;
            Frame.Data.Offset = Buffer.Offset;
            Frame.Data.Length = Buffer.Length;
            Buffer += Frame.Data.Length;
            return true;
        }

        static bool QuicTimestampFrameEncode(QUIC_TIMESTAMP_EX Frame, ref QUIC_SSBuffer Buffer)
        {
            int RequiredLength = QuicVarIntSize((ulong)QUIC_FRAME_TYPE.QUIC_FRAME_TIMESTAMP) + QuicVarIntSize((ulong)Frame.Timestamp);
            if (Buffer.Length < RequiredLength)
            {
                return false;
            }

            Buffer = QuicVarIntEncode((ulong)QUIC_FRAME_TYPE.QUIC_FRAME_TIMESTAMP, Buffer);
            Buffer = QuicVarIntEncode(Frame.Timestamp, Buffer);
            return true;
        }

        static bool QuicAckHeaderEncode(QUIC_ACK_EX Frame, QUIC_ACK_ECN_EX Ecn, ref QUIC_SSBuffer Buffer)
        {
            int RequiredLength = sizeof(byte) +
                QuicVarIntSize(Frame.LargestAcknowledged) +
                QuicVarIntSize(Frame.AckDelay) +
                QuicVarIntSize(Frame.AdditionalAckBlockCount) +
                QuicVarIntSize(Frame.FirstAckBlock);

            if (Buffer.Length < RequiredLength)
            {
                return false;
            }
            
            Buffer = QuicUint8Encode(Ecn == null ? (byte)QUIC_FRAME_TYPE.QUIC_FRAME_ACK : (byte)QUIC_FRAME_TYPE.QUIC_FRAME_ACK_1, Buffer);
            Buffer = QuicVarIntEncode(Frame.LargestAcknowledged, Buffer);
            Buffer = QuicVarIntEncode(Frame.AckDelay, Buffer);
            Buffer = QuicVarIntEncode(Frame.AdditionalAckBlockCount, Buffer);
            QuicVarIntEncode(Frame.FirstAckBlock, Buffer);
            Buffer += RequiredLength;
            return true;
        }

        static bool QuicAckBlockEncode(QUIC_ACK_BLOCK_EX Block, ref QUIC_SSBuffer Buffer)
        {
            int RequiredLength = QuicVarIntSize(Block.Gap) + QuicVarIntSize(Block.AckBlock);
            if (Buffer.Length < RequiredLength)
            {
                return false;
            }
            
            Buffer = QuicVarIntEncode(Block.Gap, Buffer);
            QuicVarIntEncode(Block.AckBlock, Buffer);
            Buffer += RequiredLength;
            return true;
        }

        static bool QuicAckEcnEncode(QUIC_ACK_ECN_EX Ecn, ref QUIC_SSBuffer Buffer)
        {
            int RequiredLength = QuicVarIntSize(Ecn.ECT_0_Count) + QuicVarIntSize(Ecn.ECT_1_Count) + QuicVarIntSize(Ecn.CE_Count);
            if (Buffer.Length < RequiredLength)
            {
                return false;
            }
                 
            Buffer = QuicVarIntEncode(Ecn.ECT_0_Count, Buffer);
            Buffer = QuicVarIntEncode(Ecn.ECT_1_Count, Buffer);
            QuicVarIntEncode(Ecn.CE_Count, Buffer);
            Buffer += RequiredLength;
            return true;
        }

        static bool QuicAckFrameEncode(QUIC_RANGE AckBlocks, long AckDelay, QUIC_ACK_ECN_EX Ecn, ref QUIC_SSBuffer Buffer)
        {
            int i = QuicRangeSize(AckBlocks) - 1;

            QUIC_SUBRANGE LastSub = QuicRangeGet(AckBlocks, i);
            ulong Largest = QuicRangeGetHigh(LastSub);
            ulong Count = (ulong)LastSub.Count;

            QUIC_ACK_EX Frame = new QUIC_ACK_EX()
            {
                LargestAcknowledged = (int)Largest,                // LargestAcknowledged
                AckDelay = AckDelay,               // AckDelay
                AdditionalAckBlockCount = (int)i,                      // AdditionalAckBlockCount
                FirstAckBlock = (int)Count - 1               // FirstAckBlock
            };

            if (!QuicAckHeaderEncode(Frame, Ecn, Buffer))
            {
                return false;
            }

            while (i != 0)
            {
                NetLog.Assert(Largest >= Count);
                Largest -= Count;

                QUIC_SUBRANGE Next = QuicRangeGet(AckBlocks, i - 1);
                ulong NextLargest = QuicRangeGetHigh(Next);
                Count = (ulong)Next.Count;

                NetLog.Assert(Largest > NextLargest);
                NetLog.Assert(Count > 0);

                QUIC_ACK_BLOCK_EX Block = new QUIC_ACK_BLOCK_EX()
                {
                    Gap = (int)(Largest - NextLargest) - 1,
                    AckBlock = (int)Count - 1
                };

                if (!QuicAckBlockEncode(Block, ref Buffer))
                {
                    NetLog.Assert(false);
                }

                Largest = NextLargest;
                i--;
            }

            if (Ecn != null)
            {
                if (!QuicAckEcnEncode(Ecn, ref Buffer))
                {
                    return false;
                }
            }

            return true;
        }

        static bool QuicConnCloseFrameEncode(QUIC_CONNECTION_CLOSE_EX Frame, ref int Offset, int BufferLength, QUIC_SSBuffer Buffer)
        {
            int RequiredLength =
                sizeof(byte) +
                QuicVarIntSize(Frame.ErrorCode) +
                (Frame.ApplicationClosed ? 0 : QuicVarIntSize(Frame.FrameType)) + QuicVarIntSize((ulong)Frame.ReasonPhrase.Length) +
                (int)Frame.ReasonPhrase.Length;

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
            Buffer = QuicVarIntEncode((ulong)Frame.ReasonPhrase.Length, Buffer);
            if (Frame.ReasonPhrase.Length != 0)
            {
                EndianBitConverter.SetBytes(Buffer.Buffer, Buffer.Offset, Frame.ReasonPhrase);
            }
            Offset += RequiredLength;
            return true;
        }

        static bool QuicPathChallengeFrameEncode(QUIC_FRAME_TYPE FrameType, QUIC_PATH_CHALLENGE_EX Frame, ref QUIC_SSBuffer Buffer)
        {
            int RequiredLength = sizeof(byte) + Frame.Data.Length;
            if (Buffer.Length < RequiredLength)
            {
                return false;
            }

            Buffer = QuicUint8Encode((byte)FrameType, Buffer);
            Frame.Data.CopyTo(Buffer.Buffer, 0);
            Buffer += RequiredLength;
            return true;
        }

        static bool QuicDataBlockedFrameEncode(QUIC_DATA_BLOCKED_EX Frame, ref int Offset, int BufferLength, QUIC_SSBuffer Buffer)
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

        static bool QuicMaxDataFrameEncode(QUIC_MAX_DATA_EX Frame, ref int Offset, int BufferLength, QUIC_SSBuffer Buffer)
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

        static bool QuicMaxStreamsFrameEncode(QUIC_MAX_STREAMS_EX Frame, ref int Offset, int BufferLength, QUIC_SSBuffer Buffer)
        {
            int RequiredLength = sizeof(byte) + QuicVarIntSize((ulong)Frame.MaximumStreams);

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

        static bool QuicStreamsBlockedFrameEncode(QUIC_STREAMS_BLOCKED_EX Frame, ref int Offset, int BufferLength, QUIC_SSBuffer Buffer)
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

        static bool QuicNewConnectionIDFrameEncode(QUIC_NEW_CONNECTION_ID_EX Frame, ref QUIC_SSBuffer Buffer)
        {
            int RequiredLength = sizeof(byte) + QuicVarIntSize(Frame.Sequence) + QuicVarIntSize(Frame.RetirePriorTo) + sizeof(byte) + Frame.Buffer.Length + QUIC_STATELESS_RESET_TOKEN_LENGTH;
            if (Buffer.Length < RequiredLength)
            {
                return false;
            }

            Buffer = QuicUint8Encode((byte)QUIC_FRAME_TYPE.QUIC_FRAME_NEW_CONNECTION_ID, Buffer);
            Buffer = QuicVarIntEncode(Frame.Sequence, Buffer);
            Buffer = QuicVarIntEncode(Frame.RetirePriorTo, Buffer);
            Buffer = QuicUint8Encode((byte)Frame.Buffer.Length, Buffer);
            Frame.Buffer.GetSpan().Slice(0, Frame.Buffer.Length + QUIC_STATELESS_RESET_TOKEN_LENGTH).CopyTo(Buffer.GetSpan());

            Buffer += RequiredLength;
            return true;
        }

        static bool QuicRetireConnectionIDFrameEncode(QUIC_RETIRE_CONNECTION_ID_EX Frame, ref int Offset, int BufferLength, QUIC_SSBuffer Buffer)
        {
            int RequiredLength = sizeof(byte) + QuicVarIntSize(Frame.Sequence);

            if (BufferLength < Offset + RequiredLength)
            {
                return false;
            }

            Buffer = Buffer.Slice(Offset);
            Buffer = QuicUint8Encode((byte)QUIC_FRAME_TYPE.QUIC_FRAME_RETIRE_CONNECTION_ID, Buffer);
            QuicVarIntEncode(Frame.Sequence, Buffer);
            Offset += RequiredLength;
            return true;
        }

        static bool QuicAckFrequencyFrameEncode(QUIC_ACK_FREQUENCY_EX Frame, ref int Offset, int BufferLength, QUIC_SSBuffer Buffer)
        {
            int RequiredLength =
                QuicVarIntSize((byte)QUIC_FRAME_TYPE.QUIC_FRAME_ACK_FREQUENCY) +
                QuicVarIntSize(Frame.SequenceNumber) +
                QuicVarIntSize(Frame.PacketTolerance) +
                QuicVarIntSize(Frame.UpdateMaxAckDelay) +
                sizeof(byte);

            if (BufferLength < Offset + RequiredLength)
            {
                return false;
            }

            QUIC_ACK_FREQUENCY_EXTRAS Extras = new QUIC_ACK_FREQUENCY_EXTRAS() { Value = 0 };
            Extras.IgnoreOrder = Frame.IgnoreOrder;
            Extras.IgnoreCE = Frame.IgnoreCE;

            Buffer = Buffer.Slice(Offset);
            Buffer = QuicVarIntEncode((byte)QUIC_FRAME_TYPE.QUIC_FRAME_ACK_FREQUENCY, Buffer);
            Buffer = QuicVarIntEncode(Frame.SequenceNumber, Buffer);
            Buffer = QuicVarIntEncode(Frame.PacketTolerance, Buffer);
            Buffer = QuicVarIntEncode(Frame.UpdateMaxAckDelay, Buffer);
            QuicUint8Encode(Extras.Value, Buffer);
            Offset += RequiredLength;
            return true;
        }

        static bool QuicDatagramFrameEncodeEx(QUIC_BUFFER[] Buffers, int BufferCount, int TotalLength, ref QUIC_SSBuffer Buffer)
        {
            QUIC_DATAGRAM_FRAME_TYPE Type = new QUIC_DATAGRAM_FRAME_TYPE()
            {
                LEN = 1,
                FrameType = 0b0011000
            };

            int RequiredLength = sizeof(byte) + (Type.LEN > 0 ? QuicVarIntSize((ulong)TotalLength) : 0) + TotalLength;
            if (Buffer.Length < RequiredLength)
            {
                return false;
            }
                
            Buffer = QuicUint8Encode((byte)Type.Type, Buffer);
            if (Type.LEN > 0)
            {
                Buffer = QuicVarIntEncode((ulong)TotalLength, Buffer);
            }
            for (int i = 0; i < BufferCount; ++i)
            {
                if (Buffers[i].Length != 0)
                {
                    Buffers[i].Buffer.AsSpan().Slice(0, Buffers[i].Length).CopyTo(Buffer.GetSpan());
                    Buffer = Buffer.Slice(Buffers[i].Length);
                }
            }
            Buffer += RequiredLength;
            return true;
        }

        static bool QuicAckHeaderDecode(ref QUIC_SSBuffer Buffer, ref QUIC_ACK_EX Frame)
        {
            if (!QuicVarIntDecode(ref Buffer, ref Frame.LargestAcknowledged) ||
                !QuicVarIntDecode(ref Buffer, ref Frame.AckDelay) ||
                !QuicVarIntDecode(ref Buffer, ref Frame.AdditionalAckBlockCount) ||
                !QuicVarIntDecode(ref Buffer, ref Frame.FirstAckBlock) ||
                Frame.FirstAckBlock > Frame.LargestAcknowledged)
            {
                return false;
            }
            return true;
        }

        static bool QuicAckFrameDecode(QUIC_FRAME_TYPE FrameType, QUIC_SSBuffer Buffer, ref bool InvalidFrame, ref QUIC_RANGE AckRanges, ref QUIC_ACK_ECN_EX Ecn, ref long AckDelay)
        {
            InvalidFrame = false;
            NetLog.Assert(AckRanges.SubRanges != null);
            QUIC_ACK_EX Frame = new QUIC_ACK_EX();
            if (!QuicAckHeaderDecode(ref Buffer, ref Frame))
            {
                InvalidFrame = true;
                return false;
            }

            int Largest = Frame.LargestAcknowledged;
            int Count = Frame.FirstAckBlock + 1;

            bool DontCare = false;
            if (QuicRangeAddRange(AckRanges, (ulong)(Largest + 1 - Count), Count, ref DontCare) == null)
            {
                return false;
            }

            if (Frame.AdditionalAckBlockCount >= QUIC_MAX_NUMBER_ACK_BLOCKS)
            {
                InvalidFrame = true;
                return false;
            }

            for (int i = 0; i < Frame.AdditionalAckBlockCount; i++)
            {

                if (Count > Largest)
                {
                    InvalidFrame = true;
                    return false;
                }

                Largest -= Count;
                QUIC_ACK_BLOCK_EX Block = new QUIC_ACK_BLOCK_EX();
                if (!QuicAckBlockDecode(Buffer, Block))
                {
                    InvalidFrame = true;
                    return false;
                }

                if (Block.Gap + 1 > Largest)
                {
                    InvalidFrame = true;
                    return false;
                }

                Largest -= (Block.Gap + 1);
                Count = Block.AckBlock + 1;
                if (QuicRangeAddRange(AckRanges, (ulong)(Largest - Count + 1), Count, ref DontCare) == null)
                {
                    return false;
                }
            }

            AckDelay = Frame.AckDelay;

            if (FrameType == QUIC_FRAME_TYPE.QUIC_FRAME_ACK_1)
            {
                if (!QuicAckEcnDecode(Buffer, ref Ecn))
                {
                    return false;
                }
            }

            return true;
        }

        static bool QuicAckEcnDecode(QUIC_SSBuffer Buffer, ref QUIC_ACK_ECN_EX Ecn)
        {
            if (!QuicVarIntDecode(ref Buffer, ref Ecn.ECT_0_Count) || !QuicVarIntDecode(ref Buffer, ref Ecn.ECT_1_Count) ||
                !QuicVarIntDecode(ref Buffer, ref Ecn.CE_Count))
            {
                return false;
            }
            return true;
        }

        static bool QuicAckBlockDecode(QUIC_SSBuffer Buffer, QUIC_ACK_BLOCK_EX Block)
        {
            if (!QuicVarIntDecode(ref Buffer, ref Block.Gap) || !QuicVarIntDecode(ref Buffer, ref Block.AckBlock))
            {
                return false;
            }
            return true;
        }

        static bool QuicMaxStreamsFrameDecode(QUIC_FRAME_TYPE FrameType, ref QUIC_SSBuffer Buffer, QUIC_MAX_STREAMS_EX Frame)
        {
            if (!QuicVarIntDecode(ref Buffer, ref Frame.MaximumStreams))
            {
                return false;
            }
            Frame.BidirectionalStreams = FrameType == QUIC_FRAME_TYPE.QUIC_FRAME_MAX_STREAMS;
            return true;
        }

        static bool QuicNewTokenFrameDecode(ref QUIC_SSBuffer Buffer, QUIC_NEW_TOKEN_EX Frame)
        {
            if (!QuicVarIntDecode(ref Buffer, ref Frame.TokenLength) || Buffer.Length < Frame.TokenLength)
            {
                return false;
            }
            Frame.Token = Buffer.Buffer;
            Frame.Offset = 0;
            return true;
        }

        static bool QuicMaxStreamDataFrameDecode(ref QUIC_SSBuffer Buffer, ref QUIC_MAX_STREAM_DATA_EX Frame)
        {
            if (!QuicVarIntDecode(ref Buffer, ref Frame.StreamID) || !QuicVarIntDecode(ref Buffer, ref Frame.MaximumData))
            {
                return false;
            }
            return true;
        }

        static bool QuicStreamDataBlockedFrameDecode(ref QUIC_SSBuffer Buffer, ref QUIC_STREAM_DATA_BLOCKED_EX Frame)
        {
            if (!QuicVarIntDecode(ref Buffer, ref Frame.StreamID) ||
                !QuicVarIntDecode(ref Buffer, ref Frame.StreamDataLimit))
            {
                return false;
            }
            return true;
        }

        static bool QuicStreamFrameDecode(QUIC_FRAME_TYPE FrameType, ref QUIC_SSBuffer Buffer, ref QUIC_STREAM_EX Frame)
        {
            QUIC_STREAM_FRAME_TYPE Type = new QUIC_STREAM_FRAME_TYPE() { Type = (byte)FrameType };

            if (!QuicVarIntDecode(ref Buffer, ref Frame.StreamID))
            {
                return false;
            }

            if (Type.OFF)
            {
                if (!QuicVarIntDecode(ref Buffer, ref Frame.Data.Offset))
                {
                    return false;
                }
            }
            else
            {
                Frame.Data.Offset = 0;
            }
            if (Type.LEN)
            {
                if (!QuicVarIntDecode(ref Buffer, ref Frame.Data.Length) || Buffer.Length < Frame.Data.Length)
                {
                    return false;
                }
                Frame.ExplicitLength = true;
            }
            else
            {
                NetLog.Assert(Buffer.Length >= 0);
                Frame.Data.Length = Buffer.Length;
            }
            Frame.Fin = Type.FIN;
            Frame.Data.Buffer = Buffer.Buffer;
            Frame.Data.Offset = Buffer.Offset;
            Frame.Data.Length = Buffer.Length;
            return true;
        }

        static bool QuicStreamFramePeekID(ref QUIC_SSBuffer Buffer, ref uint StreamID)
        {
            return QuicVarIntDecode(ref Buffer, ref StreamID);
        }

        static bool QuicMaxDataFrameDecode(ref QUIC_SSBuffer Buffer, ref QUIC_MAX_DATA_EX Frame)
        {
            if (!QuicVarIntDecode(ref Buffer, ref Frame.MaximumData))
            {
                return false;
            }
            return true;
        }

        static bool QuicDataBlockedFrameDecode(ref QUIC_SSBuffer Buffer, ref QUIC_DATA_BLOCKED_EX Frame)
        {
            if (!QuicVarIntDecode(ref Buffer, ref Frame.DataLimit)) 
            {
                return false;
            }
            return true;
        }

        static bool QuicNewConnectionIDFrameDecode(ref QUIC_SSBuffer Buffer, ref QUIC_NEW_CONNECTION_ID_EX Frame)
        {
            if (!QuicVarIntDecode(ref Buffer, ref Frame.Sequence) || !QuicVarIntDecode(ref Buffer, ref Frame.RetirePriorTo) ||
                Frame.RetirePriorTo > Frame.Sequence || Buffer.Length < 1)
            {
                return false;
            }

            Frame.Buffer.Length = Buffer[0];
            Buffer = Buffer.Slice(1);

            if (Frame.Buffer.Length < 1 || Frame.Buffer.Length > QUIC_MAX_CONNECTION_ID_LENGTH_V1 || Buffer.Length < Frame.Buffer.Length + QUIC_STATELESS_RESET_TOKEN_LENGTH)
            {
                return false;
            }

            Buffer.GetSpan().Slice(0, Frame.Buffer.Length + QUIC_STATELESS_RESET_TOKEN_LENGTH).CopyTo(Frame.Buffer.GetSpan());
            Buffer = Buffer.Slice(Frame.Buffer.Length + QUIC_STATELESS_RESET_TOKEN_LENGTH);
            return true;
        }

        static bool QuicRetireConnectionIDFrameDecode(ref QUIC_SSBuffer Buffer, ref QUIC_RETIRE_CONNECTION_ID_EX Frame)
        {
            if (!QuicVarIntDecode(ref Buffer, ref Frame.Sequence))
            {
                return false;
            }
            return true;
        }

        static bool QuicStreamsBlockedFrameDecode(QUIC_FRAME_TYPE FrameType, ref QUIC_SSBuffer Buffer, ref QUIC_STREAMS_BLOCKED_EX Frame)
        {
            if (!QuicVarIntDecode(ref Buffer, ref Frame.StreamLimit))
            {
                return false;
            }
            Frame.BidirectionalStreams = FrameType == QUIC_FRAME_TYPE.QUIC_FRAME_STREAMS_BLOCKED;
            return true;
        }

        static bool QuicPathChallengeFrameDecode(ref QUIC_SSBuffer Buffer, ref QUIC_PATH_CHALLENGE_EX Frame)
        {
            if (Buffer.Length < Frame.Data.Length)
            {
                return false;
            }

            Buffer.Slice(0, Frame.Data.Length).GetSpan().CopyTo(Frame.Data);
            Buffer = Buffer.Slice(Frame.Data.Length);
            return true;
        }

        static bool QuicConnCloseFrameDecode(QUIC_FRAME_TYPE FrameType, ref QUIC_SSBuffer Buffer, ref QUIC_CONNECTION_CLOSE_EX Frame)
        {
            Frame.ApplicationClosed = FrameType == QUIC_FRAME_TYPE.QUIC_FRAME_CONNECTION_CLOSE_1;
            Frame.FrameType = 0;

            int ReasonPhraseLength = 0;
            if (!QuicVarIntDecode(ref Buffer, ref Frame.ErrorCode) ||
                (!Frame.ApplicationClosed && !QuicVarIntDecode(ref Buffer, ref Frame.FrameType)) ||
                !QuicVarIntDecode(ref Buffer, ref ReasonPhraseLength) || Buffer.Length < ReasonPhraseLength)
            {
                return false;
            }

            Frame.ReasonPhrase = Encoding.ASCII.GetString(Buffer.Slice(0, ReasonPhraseLength).GetSpan());
            Buffer = Buffer.Slice(ReasonPhraseLength);
            return true;
        }

        static bool QuicDatagramFrameDecode(QUIC_FRAME_TYPE FrameType, ref QUIC_SSBuffer Buffer, ref QUIC_DATAGRAM_EX Frame)
        {
            QUIC_DATAGRAM_FRAME_TYPE Type = new QUIC_DATAGRAM_FRAME_TYPE() { Type = FrameType };
            if (Type.LEN > 0)
            {
                if (!QuicVarIntDecode(ref Buffer, ref Frame.Data.Length) || Buffer.Length < Frame.Data.Length)
                {
                    return false;
                }
            }
            else
            {
                NetLog.Assert(Buffer.Length >= 0);
                Frame.Data.Length = Buffer.Length;
            }
            Frame.Data.Buffer = Buffer.Buffer;
            Frame.Data.Offset = 0;
            return true;
        }

        static bool QuicAckFrequencyFrameDecode(ref QUIC_SSBuffer Buffer, ref QUIC_ACK_FREQUENCY_EX Frame)
        {
            QUIC_ACK_FREQUENCY_EXTRAS Extras = new QUIC_ACK_FREQUENCY_EXTRAS();
            if (!QuicVarIntDecode(ref Buffer, ref Frame.SequenceNumber) ||
                !QuicVarIntDecode(ref Buffer, ref Frame.PacketTolerance) ||
                !QuicVarIntDecode(ref Buffer, ref Frame.UpdateMaxAckDelay) ||
                !QuicUint8tDecode(ref Buffer, ref Extras.Value))
            {
                return false;
            }
            Frame.IgnoreOrder = Extras.IgnoreOrder;
            Frame.IgnoreCE = Extras.IgnoreCE;
            return true;
        }

        static bool QuicTimestampFrameDecode(ref QUIC_SSBuffer Buffer, ref QUIC_TIMESTAMP_EX Frame)
        {
            if (!QuicVarIntDecode(ref Buffer, ref Frame.Timestamp))
            {
                return false;
            }
            return true;
        }

        static bool QuicStreamFrameSkip(QUIC_FRAME_TYPE FrameType, ref QUIC_SSBuffer Buffer)
        {
            switch (FrameType)
            {
                case  QUIC_FRAME_TYPE.QUIC_FRAME_RESET_STREAM: 
                    {
                        QUIC_RESET_STREAM_EX Frame = new QUIC_RESET_STREAM_EX();
                        return QuicResetStreamFrameDecode(ref Buffer, ref Frame);
                    }
                case  QUIC_FRAME_TYPE.QUIC_FRAME_MAX_STREAM_DATA: 
                    {
                        QUIC_MAX_STREAM_DATA_EX Frame = new QUIC_MAX_STREAM_DATA_EX();
                        return QuicMaxStreamDataFrameDecode(ref Buffer,ref Frame);
                    }
                case  QUIC_FRAME_TYPE.QUIC_FRAME_STREAM_DATA_BLOCKED:
                    {
                        QUIC_STREAM_DATA_BLOCKED_EX Frame = new QUIC_STREAM_DATA_BLOCKED_EX();
                        return QuicStreamDataBlockedFrameDecode(ref Buffer, ref Frame);
                    }
                case  QUIC_FRAME_TYPE.QUIC_FRAME_STOP_SENDING:
                    {
                        QUIC_STOP_SENDING_EX Frame = new QUIC_STOP_SENDING_EX();
                        return QuicStopSendingFrameDecode(ref Buffer, ref Frame);
                    }
                default:
                    {
                        QUIC_STREAM_EX Frame = new QUIC_STREAM_EX();
                        return QuicStreamFrameDecode(FrameType, ref Buffer, ref Frame);
                    }
            }
        }

        static bool QuicMaxStreamDataFrameEncode(QUIC_MAX_STREAM_DATA_EX Frame, QUIC_SSBuffer Buffer)
        {
            int RequiredLength = sizeof(byte) + QuicVarIntSize(Frame.StreamID) + QuicVarIntSize(Frame.MaximumData);
            if (Buffer.Length < RequiredLength)
            {
                return false;
            }

            Buffer = QuicUint8Encode((byte)QUIC_FRAME_TYPE.QUIC_FRAME_MAX_STREAM_DATA, Buffer);
            Buffer = QuicVarIntEncode(Frame.StreamID, Buffer);
            QuicVarIntEncode(Frame.MaximumData, Buffer);
            Buffer = Buffer + RequiredLength;
            return true;
        }

        static int QuicStreamFrameHeaderSize(QUIC_STREAM_EX Frame)
        {
            int Size = sizeof(byte) + QuicVarIntSize(Frame.StreamID);
            if (Frame.Data.Offset != 0)
            {
                Size += QuicVarIntSize(Frame.Data.Offset);
            }
            if (Frame.ExplicitLength)
            {
                Size += 2; // We always use two bytes for the explicit length.
            }
            return Size;
        }

        static bool QuicStreamFrameEncode(QUIC_STREAM_EX Frame, QUIC_SSBuffer Buffer)
        {
            NetLog.Assert(Frame.Data.Length < 0x10000);
            NetLog.Assert(Frame.Data.Length < 0x10000);

            int RequiredLength = QuicStreamFrameHeaderSize(Frame) + Frame.Data.Length;
            if (Buffer.Length < RequiredLength)
            {
                return false;
            }

            QUIC_STREAM_FRAME_TYPE Type = new QUIC_STREAM_FRAME_TYPE()
            {
                FIN = Frame.Fin,
                LEN = Frame.ExplicitLength,
                OFF = BoolOk(Frame.Data.Offset),
                Type = 0b00001
            };

            Buffer = QuicUint8Encode(Type.Type, Buffer);
            Buffer = QuicVarIntEncode(Frame.StreamID, Buffer);
            if (Type.OFF)
            {
                Buffer = QuicVarIntEncode(Frame.Data.Offset, Buffer);
            }

            if (Type.LEN)
            {
                Buffer = QuicVarIntEncode2Bytes((ulong)Frame.Data.Length, Buffer); // We always use two bytes for the explicit length.
            }

            NetLog.Assert(Frame.Data.Length == 0); // Caller already set the data.
            Buffer += RequiredLength;
            return true;
        }

        static bool QuicStreamDataBlockedFrameEncode(QUIC_STREAM_DATA_BLOCKED_EX Frame, QUIC_SSBuffer Buffer)  
        {
            int RequiredLength =
                sizeof(byte) +     // Type
                QuicVarIntSize(Frame.StreamID) +
                QuicVarIntSize(Frame.StreamDataLimit);

            if (Buffer.Length < RequiredLength) 
            {
                return false;
            }
            
            Buffer = QuicUint8Encode((byte)QUIC_FRAME_TYPE.QUIC_FRAME_STREAM_DATA_BLOCKED, Buffer);
            Buffer = QuicVarIntEncode(Frame.StreamID, Buffer);
            QuicVarIntEncode(Frame.StreamDataLimit, Buffer);
            Buffer += RequiredLength;
            return true;
        }

    }
}
