using AKNet.Udp4LinuxTcp.Common;
using System.IO;

namespace AKNet.Udp5Quic.Common
{
    internal static partial class MSQuicFunc
    {
        static void QuicStreamRecvShutdown(QUIC_STREAM Stream, bool Silent, ulong ErrorCode)
        {
            if (Silent)
            {
                Stream.Flags.SentStopSending = true;
                Stream.Flags.RemoteCloseAcked = true;
                Stream.Flags.ReceiveEnabled = false;
                Stream.Flags.ReceiveDataPending = false;
                goto Exit;
            }

            if (Stream.Flags.RemoteCloseAcked || Stream.Flags.RemoteCloseFin || Stream.Flags.RemoteCloseReset)
            {
                goto Exit;
            }

            if (Stream.Flags.SentStopSending)
            {
                goto Exit;
            }
            
            Stream.Flags.ReceiveEnabled = false;
            Stream.Flags.ReceiveDataPending = false;
            Stream.RecvShutdownErrorCode = ErrorCode;
            Stream.Flags.SentStopSending = true;

            if (Stream.RecvMaxLength != long.MaxValue)
            {
                QuicStreamProcessResetFrame(Stream, Stream.RecvMaxLength, 0);
                Silent = true;
                goto Exit;
            }

            QuicSendSetStreamSendFlag(Stream.Connection.Send, Stream, QUIC_STREAM_SEND_FLAG_RECV_ABORT, false);
            QuicSendClearStreamSendFlag(Stream.Connection.Send, Stream, QUIC_STREAM_SEND_FLAG_MAX_DATA);
        Exit:
            if (Silent)
            {
                QuicStreamTryCompleteShutdown(Stream);
            }
        }

        static void QuicStreamProcessResetFrame(QUIC_STREAM Stream, long FinalSize,ulong ErrorCode)
        {
            Stream.Flags.RemoteCloseReset = true;

            if (!Stream.Flags.RemoteCloseAcked)
            {
                Stream.Flags.RemoteCloseAcked = true;
                Stream.Flags.ReceiveEnabled = false;
                Stream.Flags.ReceiveDataPending = false;

                long TotalRecvLength = QuicRecvBufferGetTotalLength(Stream.RecvBuffer);
                if (TotalRecvLength > FinalSize)
                {
                    QuicConnTransportError(Stream.Connection, QUIC_ERROR_FINAL_SIZE_ERROR);
                    return;
                }

                if (TotalRecvLength < FinalSize)
                {
                    long FlowControlIncrease = FinalSize - TotalRecvLength;
                    Stream.Connection.Send.OrderedStreamBytesReceived += FlowControlIncrease;
                    if (Stream.Connection.Send.OrderedStreamBytesReceived < FlowControlIncrease ||
                        Stream.Connection.Send.OrderedStreamBytesReceived > Stream.Connection.Send.MaxData)
                    {
                        QuicConnTransportError(Stream.Connection, QUIC_ERROR_FINAL_SIZE_ERROR);
                        return;
                    }
                }

                long TotalReadLength = Stream.RecvBuffer.BaseOffset;
                if (TotalReadLength < FinalSize)
                {
                    long FlowControlIncrease = FinalSize - TotalReadLength;
                    Stream.Connection.Send.MaxData += FlowControlIncrease;
                    QuicSendSetSendFlag(Stream.Connection.Send, QUIC_CONN_SEND_FLAG_MAX_DATA);
                }

                if (!Stream.Flags.SentStopSending)
                {
                    QuicStreamIndicatePeerSendAbortedEvent(Stream, ErrorCode);
                }

                QuicSendClearStreamSendFlag(Stream.Connection.Send, Stream, QUIC_STREAM_SEND_FLAG_MAX_DATA | QUIC_STREAM_SEND_FLAG_RECV_ABORT);
                QuicStreamTryCompleteShutdown(Stream);
            }
        }
    }
}
