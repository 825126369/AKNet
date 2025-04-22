using AKNet.Common;
using AKNet.Udp4LinuxTcp.Common;
using AKNet.Udp5Quic.Common;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Sockets;
using System.Threading;
using static System.Net.WebRequestMethods;

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

        static void QuicStreamProcessResetFrame(QUIC_STREAM Stream, int FinalSize,ulong ErrorCode)
        {
            Stream.Flags.RemoteCloseReset = true;

            if (!Stream.Flags.RemoteCloseAcked)
            {
                Stream.Flags.RemoteCloseAcked = true;
                Stream.Flags.ReceiveEnabled = false;
                Stream.Flags.ReceiveDataPending = false;

                int TotalRecvLength = QuicRecvBufferGetTotalLength(Stream.RecvBuffer);
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

        static bool QuicStreamReceiveComplete(QUIC_STREAM Stream, int BufferLength)
        {
            if (Stream.Flags.SentStopSending || Stream.Flags.RemoteCloseFin)
            {
                return false;
            }

            NetLog.Assert(BufferLength <= Stream.RecvPendingLength, "App overflowed read buffer!");
            if (Stream.RecvPendingLength == 0 || QuicRecvBufferDrain(Stream.RecvBuffer, BufferLength))
            {
                Stream.Flags.ReceiveDataPending = false; // No more pending data to deliver.
            }

            if (BufferLength != 0)
            {
                Stream.RecvPendingLength -= BufferLength;
                QuicPerfCounterAdd( QUIC_PERFORMANCE_COUNTERS.QUIC_PERF_COUNTER_APP_RECV_BYTES, BufferLength);
                QuicStreamOnBytesDelivered(Stream, BufferLength);
            }

            if (Stream->RecvPendingLength == 0)
            {
                //
                // All data was drained, so additional callbacks can continue to be
                // delivered.
                //
                Stream->Flags.ReceiveEnabled = TRUE;

            }
            else if (!Stream->Flags.ReceiveMultiple)
            {
                //
                // The app didn't drain all the data, so we will need to wait for them
                // to request a new receive.
                //
                Stream->RecvPendingLength = 0;
            }

            if (!Stream->Flags.ReceiveEnabled)
            {
                //
                // The application layer can't drain any more right now. Pause the
                // receive callbacks until the application re-enables them.
                //
                QuicTraceEvent(
                    StreamRecvState,
                    "[strm][%p] Recv State: %hhu",
                    Stream,
                    QuicStreamRecvGetState(Stream));
                return FALSE;
            }

            if (Stream->Flags.ReceiveDataPending)
            {
                //
                // There is still more data for the app to process and it still has
                // receive callbacks enabled, so do another recv flush (if not already
                // doing multi-receive mode).
                //
                return !Stream->Flags.ReceiveMultiple;
            }

            if (Stream->RecvBuffer.BaseOffset == Stream->RecvMaxLength)
            {
                CXPLAT_DBG_ASSERT(!Stream->Flags.ReceiveDataPending);
                //
                // We have delivered all the payload that needs to be delivered. Deliver
                // the graceful close event now.
                //
                Stream->Flags.RemoteCloseFin = TRUE;
                Stream->Flags.RemoteCloseAcked = TRUE;

                QuicTraceEvent(
                    StreamRecvState,
                    "[strm][%p] Recv State: %hhu",
                    Stream,
                    QuicStreamRecvGetState(Stream));

                QUIC_STREAM_EVENT Event;
                Event.Type = QUIC_STREAM_EVENT_PEER_SEND_SHUTDOWN;
                QuicTraceLogStreamVerbose(
                    IndicatePeerSendShutdown,
                    Stream,
                    "Indicating QUIC_STREAM_EVENT_PEER_SEND_SHUTDOWN");
                (void)QuicStreamIndicateEvent(Stream, &Event);

                //
                // Now that the close event has been delivered to the app, we can shut
                // down the stream.
                //
                QuicStreamTryCompleteShutdown(Stream);

                //
                // Remove any flags we shouldn't be sending now that the receive
                // direction is closed.
                //
                QuicSendClearStreamSendFlag(
                    &Stream->Connection->Send,
                    Stream,
                    QUIC_STREAM_SEND_FLAG_MAX_DATA | QUIC_STREAM_SEND_FLAG_RECV_ABORT);
            }
            else if (Stream->Flags.RemoteCloseResetReliable && Stream->RecvBuffer.BaseOffset >= Stream->RecvMaxLength)
            {
                //
                // ReliableReset was initiated by the peer, and we sent enough data to the app, we can alert the app
                // we're done and shutdown the RECV direction of this stream.
                //
                QuicTraceEvent(
                    StreamRecvState,
                    "[strm][%p] Recv State: %hhu",
                    Stream,
                    QuicStreamRecvGetState(Stream));
                QuicStreamIndicatePeerSendAbortedEvent(Stream, Stream->RecvShutdownErrorCode);
                QuicStreamRecvShutdown(Stream, TRUE, Stream->RecvShutdownErrorCode);
            }

            return FALSE;
        }

        static void QuicStreamOnBytesDelivered(QUIC_STREAM Stream, int BytesDelivered)
        {
            int RecvBufferDrainThreshold = Stream.RecvBuffer.VirtualBufferLength / QUIC_RECV_BUFFER_DRAIN_RATIO;

            Stream.RecvWindowBytesDelivered += BytesDelivered;
            Stream.Connection.Send.MaxData += BytesDelivered;

            Stream.Connection.Send.OrderedStreamBytesDeliveredAccumulator += BytesDelivered;
            if (Stream.Connection.Send.OrderedStreamBytesDeliveredAccumulator >=
                Stream.Connection.Settings.ConnFlowControlWindow / QUIC_RECV_BUFFER_DRAIN_RATIO)
            {
                Stream.Connection.Send.OrderedStreamBytesDeliveredAccumulator = 0;
                QuicSendSetSendFlag(Stream.Connection.Send, QUIC_CONN_SEND_FLAG_MAX_DATA);
            }

            if (Stream.RecvWindowBytesDelivered >= RecvBufferDrainThreshold)
            {
                long TimeNow = CxPlatTime();
                if (Stream.RecvBuffer.VirtualBufferLength < Stream.Connection.Settings.ConnFlowControlWindow)
                {
                    long TimeThreshold = ((Stream.RecvWindowBytesDelivered * Stream.Connection.Paths[0].SmoothedRtt) / RecvBufferDrainThreshold);
                    if (CxPlatTimeDiff64(Stream.RecvWindowLastUpdate, TimeNow) <= TimeThreshold)
                    {
                        QuicRecvBufferIncreaseVirtualBufferLength(Stream.RecvBuffer, Stream.RecvBuffer.VirtualBufferLength * 2);
                    }
                }

                Stream.RecvWindowLastUpdate = TimeNow;
                Stream.RecvWindowBytesDelivered = 0;

            }
            else if (!BoolOk(Stream.Connection.Send.SendFlags & QUIC_CONN_SEND_FLAG_ACK))
            {
                return;
            }

            NetLog.Assert(Stream.RecvBuffer.BaseOffset + Stream.RecvBuffer.VirtualBufferLength > Stream.MaxAllowedRecvOffset);
            Stream.MaxAllowedRecvOffset = Stream.RecvBuffer.BaseOffset + Stream.RecvBuffer.VirtualBufferLength;

            QuicSendSetSendFlag(Stream.Connection.Send, QUIC_CONN_SEND_FLAG_MAX_DATA);
            QuicSendSetStreamSendFlag(Stream.Connection.Send, Stream, QUIC_STREAM_SEND_FLAG_MAX_DATA, false);
        }

        static void QuicStreamRecvFlush(QUIC_STREAM Stream)
        {
            Stream.Flags.ReceiveFlushQueued = false;
            if (!Stream.Flags.ReceiveDataPending)
            {
                return;
            }

            if (!Stream.Flags.ReceiveEnabled)
            {
                return;
            }

            bool FlushRecv = true;
            while (FlushRecv)
            {
                NetLog.Assert(!Stream.Flags.SentStopSending);

                QUIC_BUFFER[] RecvBuffers = new QUIC_BUFFER[3];
                QUIC_STREAM_EVENT Event = new QUIC_STREAM_EVENT();
                Event.Type = QUIC_STREAM_EVENT_TYPE.QUIC_STREAM_EVENT_RECEIVE;
                Event.RECEIVE.Buffers = new List<QUIC_BUFFER>(RecvBuffers);

                bool DataAvailable = QuicRecvBufferHasUnreadData(Stream.RecvBuffer);
                if (DataAvailable)
                {
                    QuicRecvBufferRead(Stream.RecvBuffer,
                        Event.RECEIVE.AbsoluteOffset,
                        Event.RECEIVE.Buffers.Count,
                        RecvBuffers);
                    for (int i = 0; i < Event.RECEIVE.Buffers.Count; ++i)
                    {
                        Event.RECEIVE.TotalBufferLength += RecvBuffers[i].Length;
                    }
                    NetLog.Assert(Event.RECEIVE.TotalBufferLength != 0);

                    if (Event.RECEIVE.AbsoluteOffset < Stream.RecvMax0RttLength)
                    {
                        Event.RECEIVE.Flags |= QUIC_RECEIVE_FLAG_0_RTT;
                    }

                    if (Event.RECEIVE.AbsoluteOffset + Event.RECEIVE.TotalBufferLength == Stream.RecvMaxLength)
                    {
                        Event.RECEIVE.Flags |= QUIC_RECEIVE_FLAG_FIN;
                    }
                }
                else
                {
                    Event.RECEIVE.AbsoluteOffset = Stream.RecvMaxLength;
                    Event.RECEIVE.Buffers.Clear();
                    Event.RECEIVE.Flags |= QUIC_RECEIVE_FLAG_FIN; // TODO - 0-RTT flag?
                }

                Stream.Flags.ReceiveEnabled = Stream.Flags.ReceiveMultiple;
                Stream.Flags.ReceiveCallActive = true;
                Stream.RecvPendingLength += Event.RECEIVE.TotalBufferLength;
                NetLog.Assert(Stream.RecvPendingLength <= Stream.RecvBuffer.ReadPendingLength);

                ulong Status = QuicStreamIndicateEvent(Stream, Event);
                Stream.Flags.ReceiveCallActive = false;
                if (Status == QUIC_STATUS_CONTINUE)
                {
                    NetLog.Assert(!Stream.Flags.SentStopSending);
                    Interlocked.Add(ref Stream.RecvCompletionLength, Event.RECEIVE.TotalBufferLength);
                    FlushRecv = true;
                    Stream.Flags.ReceiveEnabled = true;
                }
                else if (Status == QUIC_STATUS_PENDING)
                {
                    FlushRecv = (Stream.RecvCompletionLength != 0);
                }
                else
                {
                    NetLog.Assert(QUIC_SUCCEEDED(Status), "App failed recv callback");
                    Interlocked.Add(ref Stream.RecvCompletionLength, Event.RECEIVE.TotalBufferLength);
                    FlushRecv = true;
                }

                if (FlushRecv)
                {
                    int BufferLength = Stream.RecvCompletionLength;
                    Interlocked.Add(ref Stream.RecvCompletionLength, -BufferLength);
                    FlushRecv = QuicStreamReceiveComplete(Stream, BufferLength);
                }
            }
        }

        static void QuicStreamProcessStopSendingFrame(QUIC_STREAM Stream,ulong ErrorCode)
        {
            if (!Stream.Flags.LocalCloseAcked && !Stream.Flags.LocalCloseReset)
            {
                Stream.Flags.ReceivedStopSending = true;
                QUIC_STREAM_EVENT Event = new QUIC_STREAM_EVENT();
                Event.Type =  QUIC_STREAM_EVENT_TYPE.QUIC_STREAM_EVENT_PEER_RECEIVE_ABORTED;
                Event.PEER_RECEIVE_ABORTED.ErrorCode = ErrorCode;
                QuicStreamIndicateEvent(Stream, Event);
                QuicStreamSendShutdown(Stream, false, false, false, QUIC_ERROR_NO_ERROR);
            }
        }

        static ulong QuicStreamRecv(QUIC_STREAM Stream, QUIC_RX_PACKET Packet, QUIC_FRAME_TYPE FrameType, ReadOnlySpan<byte> Buffer, ref bool UpdatedFlowControl)
        {
            ulong Status = QUIC_STATUS_SUCCESS;

            switch (FrameType)
            {

                case  QUIC_FRAME_TYPE.QUIC_FRAME_RESET_STREAM: 
                    {
                        QUIC_RESET_STREAM_EX Frame = new QUIC_RESET_STREAM_EX();
                        if (!QuicResetStreamFrameDecode(ref Buffer, ref Frame)) 
                        {
                            return QUIC_STATUS_INVALID_PARAMETER;
                        }

                        QuicStreamProcessResetFrame(Stream,Frame.FinalSize,Frame.ErrorCode);

                        break;
                    }

                case  QUIC_FRAME_TYPE.QUIC_FRAME_STOP_SENDING:
                    {
                        QUIC_STOP_SENDING_EX Frame = new QUIC_STOP_SENDING_EX();
                        if (!QuicStopSendingFrameDecode(ref Buffer, ref Frame))
                        {
                            return QUIC_STATUS_INVALID_PARAMETER;
                        }

                        QuicStreamProcessStopSendingFrame(Stream, Frame.ErrorCode);
                        break;
                    }

                case  QUIC_FRAME_TYPE.QUIC_FRAME_MAX_STREAM_DATA:
                    {
                        QUIC_MAX_STREAM_DATA_EX Frame;
                        if (!QuicMaxStreamDataFrameDecode(ref Buffer, ref Frame))
                        {
                            return QUIC_STATUS_INVALID_PARAMETER;
                        }

                        if (Stream.MaxAllowedSendOffset < Frame.MaximumData)
                        {
                            Stream.MaxAllowedSendOffset = Frame.MaximumData;
                            UpdatedFlowControl = true;

                            Stream.SendWindow = (uint)Math.Min(Stream.MaxAllowedSendOffset - Stream.UnAckedOffset, uint.MaxValue);

                            QuicSendBufferStreamAdjust(Stream);
                            QuicStreamRemoveOutFlowBlockedReason(Stream, QUIC_FLOW_BLOCKED_STREAM_FLOW_CONTROL);
                            QuicSendClearStreamSendFlag(Stream.Connection.Send, Stream, QUIC_STREAM_SEND_FLAG_DATA_BLOCKED);
                            QuicStreamSendDumpState(Stream);
                            QuicSendQueueFlush(Stream.Connection.Send, QUIC_SEND_FLUSH_REASON.REASON_STREAM_FLOW_CONTROL);
                        }

                        break;
                    }

                case  QUIC_FRAME_TYPE.QUIC_FRAME_STREAM_DATA_BLOCKED:
                    {
                        QUIC_STREAM_DATA_BLOCKED_EX Frame;
                        if (!QuicStreamDataBlockedFrameDecode(BufferLength, Buffer, Offset, &Frame))
                        {
                            return QUIC_STATUS_INVALID_PARAMETER;
                        }

                        QuicTraceLogStreamVerbose(
                            RemoteBlocked,
                            Stream,
                            "Remote FC blocked (%llu)",
                            Frame.StreamDataLimit);

                        QuicSendSetStreamSendFlag(
                            &Stream->Connection->Send,
                            Stream,
                            QUIC_STREAM_SEND_FLAG_MAX_DATA,
                            FALSE);

                        break;
                    }

                case  QUIC_FRAME_TYPE.QUIC_FRAME_RELIABLE_RESET_STREAM:
                    {
                        QUIC_RELIABLE_RESET_STREAM_EX Frame;
                        if (!QuicReliableResetFrameDecode(BufferLength, Buffer, Offset, &Frame))
                        {
                            return QUIC_STATUS_INVALID_PARAMETER;
                        }

                        QuicStreamProcessReliableResetFrame(
                            Stream,
                            Frame.ErrorCode,
                            Frame.ReliableSize);

                        break;
                    }

                default: // QUIC_FRAME_STREAM*
                    {
                        QUIC_STREAM_EX Frame;
                        if (!QuicStreamFrameDecode(FrameType, BufferLength, Buffer, Offset, &Frame))
                        {
                            return QUIC_STATUS_INVALID_PARAMETER;
                        }
                        Status =
                            QuicStreamProcessStreamFrame(
                                Stream, Packet->EncryptedWith0Rtt, &Frame);

                        break;
                    }
            }

            return Status;
        }

    }
}
