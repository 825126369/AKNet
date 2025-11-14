/************************************Copyright*****************************************
*        ProjectName:AKNet
*        Web:https://github.com/825126369/AKNet
*        Description:C#游戏网络库
*        Author:许珂
*        StartTime:2024/11/01 00:00:00
*        ModifyTime:2025/11/14 8:56:47
*        Copyright:MIT软件许可证
************************************Copyright*****************************************/
using AKNet.Common;

namespace MSQuic1
{
    internal class QUIC_SEND_BUFFER
    {
        public int PostedBytes;
        public int BufferedBytes;
        public int IdealBytes;
    }

    internal static partial class MSQuicFunc
    {
        static void QuicSendBufferInitialize(QUIC_SEND_BUFFER SendBuffer)
        {
            SendBuffer.IdealBytes = QUIC_DEFAULT_IDEAL_SEND_BUFFER_SIZE;
        }

        static void QuicSendBufferUninitialize(QUIC_SEND_BUFFER SendBuffer)
        {
           
        }

        static bool QuicSendBufferHasSpace(QUIC_SEND_BUFFER SendBuffer)
        {
            return SendBuffer.BufferedBytes < SendBuffer.IdealBytes;
        }

        static void QuicSendBufferFill(QUIC_CONNECTION Connection)
        {
            NetLog.Assert(Connection.Settings.SendBufferingEnabled);
            CXPLAT_LIST_ENTRY Entry = Connection.Send.SendStreams.Next;
            while (QuicSendBufferHasSpace(Connection.SendBuffer) && Entry != Connection.Send.SendStreams)
            {
                QUIC_STREAM Stream = CXPLAT_CONTAINING_RECORD<QUIC_STREAM>(Entry);
                Entry = Entry.Next;
                QUIC_SEND_REQUEST Req = Stream.SendBufferBookmark;
                while (Req != null && QuicSendBufferHasSpace(Connection.SendBuffer))
                {
                    if (QUIC_FAILED(QuicStreamSendBufferRequest(Stream, Req)))
                    {
                        return;
                    }
                    Req = Req.Next;
                }
            }
        }

        static void QuicSendBufferConnectionAdjust(QUIC_CONNECTION Connection)
        {
            if (Connection.SendBuffer.IdealBytes == QUIC_MAX_IDEAL_SEND_BUFFER_SIZE || Connection.Streams.StreamTable == null)
            {
                return;
            }

            int NewIdealBytes = QuicGetNextIdealBytes(QuicCongestionControlGetBytesInFlightMax(Connection.CongestionControl));
            if (NewIdealBytes > Connection.SendBuffer.IdealBytes)
            {
                Connection.SendBuffer.IdealBytes = NewIdealBytes;
                foreach (var v in Connection.Streams.StreamTable)
                {
                    QUIC_STREAM Stream = v.Value;
                    if (Stream.Flags.SendEnabled)
                    {
                        QuicSendBufferStreamAdjust(Stream);
                    }
                }

                if (Connection.Settings.SendBufferingEnabled)
                {
                    QuicSendBufferFill(Connection);
                }
            }
        }

        static void QuicSendBufferFree(QUIC_SEND_BUFFER SendBuffer, byte[] Buf, int Size)
        {
            SendBuffer.BufferedBytes -= Size;
        }

        static int QuicGetNextIdealBytes(int BaseValue)
        {
            int Threshold = QUIC_DEFAULT_IDEAL_SEND_BUFFER_SIZE;
            while (Threshold <= BaseValue)
            {
                int NextThreshold = Threshold + (Threshold / 2); // 1.5x growth
                if (NextThreshold > QUIC_MAX_IDEAL_SEND_BUFFER_SIZE)
                {
                    Threshold = QUIC_MAX_IDEAL_SEND_BUFFER_SIZE;
                    break;
                }
                Threshold = NextThreshold;
            }

            return Threshold;
        }

        static void QuicSendBufferStreamAdjust(QUIC_STREAM Stream)
        {
            int ByteCount = Stream.Connection.SendBuffer.IdealBytes;
            if (Stream.SendWindow < ByteCount)
            {
                int SendWindowIdealBytes = QuicGetNextIdealBytes(Stream.SendWindow);
                if (SendWindowIdealBytes < ByteCount)
                {
                    ByteCount = SendWindowIdealBytes;
                }
            }

            if (Stream.LastIdealSendBuffer != ByteCount)
            {
                Stream.LastIdealSendBuffer = ByteCount;
                QUIC_STREAM_EVENT Event = new QUIC_STREAM_EVENT();
                Event.Type =  QUIC_STREAM_EVENT_TYPE.QUIC_STREAM_EVENT_IDEAL_SEND_BUFFER_SIZE;
                Event.IDEAL_SEND_BUFFER_SIZE.ByteCount = ByteCount;
                QuicStreamIndicateEvent(Stream, ref Event);
            }
        }

        static byte[] QuicSendBufferAlloc(QUIC_SEND_BUFFER SendBuffer, int Size)
        {
            byte[] Buf = new byte[Size];
            if (Buf != null)
            {
                SendBuffer.BufferedBytes += Size;
            }
            return Buf;
        }

    }
}
