using AKNet.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

namespace AKNet.Udp5MSQuic.Common
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
                    Stream.Connection.Send.OrderedStreamBytesReceived += (int)FlowControlIncrease;
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
                QuicPerfCounterAdd(QUIC_PERFORMANCE_COUNTERS.QUIC_PERF_COUNTER_APP_RECV_BYTES, BufferLength);
                QuicStreamOnBytesDelivered(Stream, BufferLength);
            }

            if (Stream.RecvPendingLength == 0)
            {
                Stream.Flags.ReceiveEnabled = true;
            }
            else if (!Stream.Flags.ReceiveMultiple)
            {
                Stream.RecvPendingLength = 0;
            }

            if (!Stream.Flags.ReceiveEnabled)
            {
                return false;
            }

            if (Stream.Flags.ReceiveDataPending)
            {
                return !Stream.Flags.ReceiveMultiple;
            }

            if (Stream.RecvBuffer.BaseOffset == Stream.RecvMaxLength)
            {
                NetLog.Assert(!Stream.Flags.ReceiveDataPending);
                Stream.Flags.RemoteCloseFin = true;
                Stream.Flags.RemoteCloseAcked = true;

                QUIC_STREAM_EVENT Event = new QUIC_STREAM_EVENT();
                Event.Type = QUIC_STREAM_EVENT_TYPE.QUIC_STREAM_EVENT_PEER_SEND_SHUTDOWN;
                QuicStreamIndicateEvent(Stream, Event);
                QuicStreamTryCompleteShutdown(Stream);
                QuicSendClearStreamSendFlag(Stream.Connection.Send, Stream, QUIC_STREAM_SEND_FLAG_MAX_DATA | QUIC_STREAM_SEND_FLAG_RECV_ABORT);
            }
            else if (Stream.Flags.RemoteCloseResetReliable && Stream.RecvBuffer.BaseOffset >= Stream.RecvMaxLength)
            {
                QuicStreamIndicatePeerSendAbortedEvent(Stream, Stream.RecvShutdownErrorCode);
                QuicStreamRecvShutdown(Stream, true, Stream.RecvShutdownErrorCode);
            }

            return false;
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
                    if (CxPlatTimeDiff(Stream.RecvWindowLastUpdate, TimeNow) <= TimeThreshold)
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
                QUIC_STREAM_EVENT Event = new QUIC_STREAM_EVENT();
                Event.Type = QUIC_STREAM_EVENT_TYPE.QUIC_STREAM_EVENT_RECEIVE;
                Event.RECEIVE = new QUIC_STREAM_EVENT.RECEIVE_DATA();

                bool DataAvailable = QuicRecvBufferHasUnreadData(Stream.RecvBuffer);
                if (DataAvailable)
                {
                    QuicRecvBufferRead(Stream.RecvBuffer,
                        ref Event.RECEIVE.AbsoluteOffset,
                        ref Event.RECEIVE.BufferCount,
                        Event.RECEIVE.Buffers);

                    for (int i = 0; i < Event.RECEIVE.BufferCount; ++i)
                    {
                        Event.RECEIVE.TotalBufferLength += Event.RECEIVE.Buffers[i].Length;
                    }
                    NetLog.Assert(Event.RECEIVE.TotalBufferLength != 0);

                    if (Event.RECEIVE.AbsoluteOffset < Stream.RecvMax0RttLength)
                    {
                        Event.RECEIVE.Flags |=  QUIC_RECEIVE_FLAGS.QUIC_RECEIVE_FLAG_0_RTT;
                    }

                    if (Event.RECEIVE.AbsoluteOffset + Event.RECEIVE.TotalBufferLength == Stream.RecvMaxLength)
                    {
                        Event.RECEIVE.Flags |=  QUIC_RECEIVE_FLAGS.QUIC_RECEIVE_FLAG_FIN;
                    }
                }
                else
                {
                    Event.RECEIVE.BufferCount = 0;
                    Event.RECEIVE.AbsoluteOffset = Stream.RecvMaxLength;
                    Event.RECEIVE.Flags |=  QUIC_RECEIVE_FLAGS.QUIC_RECEIVE_FLAG_FIN; // TODO - 0-RTT flag?
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

        static ulong QuicStreamRecv(QUIC_STREAM Stream, QUIC_RX_PACKET Packet, QUIC_FRAME_TYPE FrameType, QUIC_SSBuffer Buffer, ref bool UpdatedFlowControl)
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
                        QUIC_MAX_STREAM_DATA_EX Frame = new QUIC_MAX_STREAM_DATA_EX();
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
                        QUIC_STREAM_DATA_BLOCKED_EX Frame = default;
                        if (!QuicStreamDataBlockedFrameDecode(ref Buffer,ref Frame))
                        {
                            return QUIC_STATUS_INVALID_PARAMETER;
                        }

                        QuicSendSetStreamSendFlag(
                            Stream.Connection.Send,
                            Stream,
                            QUIC_STREAM_SEND_FLAG_MAX_DATA,
                            false);

                        break;
                    }

                case  QUIC_FRAME_TYPE.QUIC_FRAME_RELIABLE_RESET_STREAM:
                    {
                        QUIC_RELIABLE_RESET_STREAM_EX Frame = new QUIC_RELIABLE_RESET_STREAM_EX();
                        if (!QuicReliableResetFrameDecode(ref Buffer, Frame))
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
                        QUIC_STREAM_EX Frame = new QUIC_STREAM_EX();
                        if (!QuicStreamFrameDecode(FrameType, ref Buffer, ref Frame))
                        {
                            return QUIC_STATUS_INVALID_PARAMETER;
                        }
                        Status = QuicStreamProcessStreamFrame(Stream, Packet.EncryptedWith0Rtt, Frame);

                        break;
                    }
            }

            return Status;
        }

        static void QuicStreamReceiveCompletePending(QUIC_STREAM Stream)
        {
            Interlocked.Exchange(ref Stream.ReceiveCompleteOperation, Stream.ReceiveCompleteOperationStorage);
            int BufferLength = Stream.RecvCompletionLength;
            Interlocked.Add(ref Stream.RecvCompletionLength, -BufferLength);
            if (QuicStreamReceiveComplete(Stream, BufferLength))
            {
                QuicStreamRecvFlush(Stream);
            }
            QuicStreamRelease(Stream, QUIC_STREAM_REF.QUIC_STREAM_REF_OPERATION);
        }

        static ulong QuicStreamRecvSetEnabledState(QUIC_STREAM Stream, bool NewRecvEnabled)
        {
            if (Stream.Flags.RemoteNotAllowed ||
                Stream.Flags.RemoteCloseFin ||
                Stream.Flags.RemoteCloseReset ||
                Stream.Flags.SentStopSending)
            {
                return QUIC_STATUS_INVALID_STATE;
            }

            if (Stream.Flags.ReceiveEnabled != NewRecvEnabled)
            {
                NetLog.Assert(!Stream.Flags.SentStopSending);
                Stream.Flags.ReceiveEnabled = NewRecvEnabled;

                if (Stream.Flags.Started && NewRecvEnabled && (Stream.RecvBuffer.RecvMode ==  QUIC_RECV_BUF_MODE.QUIC_RECV_BUF_MODE_MULTIPLE ||
                    Stream.RecvBuffer.ReadPendingLength == 0))
                {
                    QuicStreamRecvQueueFlush(Stream, true);
                }
            }

            return QUIC_STATUS_SUCCESS;
        }

        static void QuicStreamProcessReliableResetFrame(QUIC_STREAM Stream, ulong ErrorCode, int ReliableOffset)
        {
            if (!Stream.Connection.State.ReliableResetStreamNegotiated)
            {
                QuicConnTransportError(Stream.Connection, QUIC_ERROR_TRANSPORT_PARAMETER_ERROR);
                return;
            }

            if (Stream.RecvMaxLength == 0 || ReliableOffset < Stream.RecvMaxLength)
            {
                Stream.RecvMaxLength = ReliableOffset;
                Stream.Flags.RemoteCloseResetReliable = true;
            }

            if (Stream.RecvBuffer.BaseOffset >= Stream.RecvMaxLength)
            {
                QuicStreamIndicatePeerSendAbortedEvent(Stream, ErrorCode);
                QuicStreamRecvShutdown(Stream, true, ErrorCode);
            }
            else
            {
                Stream.RecvShutdownErrorCode = ErrorCode;
            }
        }


        static void QuicStreamRecvQueueFlush(QUIC_STREAM Stream, bool AllowInlineFlush)
        {
            if (Stream.Flags.ReceiveEnabled && Stream.Flags.ReceiveDataPending)
            {
                if (AllowInlineFlush)
                {
                    QuicStreamRecvFlush(Stream);

                }
                else if (!Stream.Flags.ReceiveFlushQueued)
                {
                    QUIC_OPERATION Oper;
                    if ((Oper = QuicOperationAlloc(Stream.Connection.Worker,  QUIC_OPERATION_TYPE.QUIC_OPER_TYPE_FLUSH_STREAM_RECV)) != null)
                    {
                        Oper.FLUSH_STREAM_RECEIVE.Stream = Stream;
                        QuicStreamAddRef(Stream,  QUIC_STREAM_REF.QUIC_STREAM_REF_OPERATION);
                        QuicConnQueueOper(Stream.Connection, Oper);
                        Stream.Flags.ReceiveFlushQueued = true;
                    }
                }
            }
        }

        static void QuicStreamIndicatePeerSendAbortedEvent(QUIC_STREAM Stream, ulong ErrorCode)
        {
            QUIC_STREAM_EVENT Event = new QUIC_STREAM_EVENT();
            Event.Type =  QUIC_STREAM_EVENT_TYPE.QUIC_STREAM_EVENT_PEER_SEND_ABORTED;
            Event.PEER_SEND_ABORTED.ErrorCode = ErrorCode;
            QuicStreamIndicateEvent(Stream, Event);
        }

        static ulong QuicStreamProcessStreamFrame(QUIC_STREAM Stream, bool EncryptedWith0Rtt, QUIC_STREAM_EX Frame)
        {
            ulong Status;
            bool ReadyToDeliver = false;
            int EndOffset = Frame.Data.Offset + Frame.Data.Length;

            if (Stream.Flags.RemoteNotAllowed)
            {
                Status = QUIC_STATUS_INVALID_STATE;
                goto Error;
            }

            if (Stream.Flags.RemoteCloseFin || Stream.Flags.RemoteCloseReset)
            {
                Status = QUIC_STATUS_SUCCESS;
                goto Error;
            }

            if (Stream.Flags.SentStopSending)
            {
                if (Frame.Fin)
                {
                    QuicStreamProcessResetFrame(Stream, Frame.Data.Offset + Frame.Data.Length, 0);
                }
                Status = QUIC_STATUS_SUCCESS;
                goto Error;
            }

            if (Frame.Fin && Stream.RecvMaxLength != int.MaxValue && EndOffset != Stream.RecvMaxLength)
            {
                Status = QUIC_STATUS_INVALID_PARAMETER;
                goto Error;
            }

            if (Stream.Flags.RemoteCloseResetReliable)
            {
                if (Stream.RecvBuffer.BaseOffset >= Stream.RecvMaxLength)
                {
                    Status = QUIC_STATUS_SUCCESS;
                    goto Error;
                }
            }
            else if (EndOffset > Stream.RecvMaxLength)
            {
                Status = QUIC_STATUS_INVALID_PARAMETER;
                goto Error;
            }

            if (EndOffset > (long)QUIC_VAR_INT_MAX)
            {
                QuicConnTransportError(Stream.Connection, QUIC_ERROR_FLOW_CONTROL_ERROR);
                Status = QUIC_STATUS_INVALID_PARAMETER;
                goto Error;
            }

            if (Frame.Data.Length == 0)
            {
                Status = QUIC_STATUS_SUCCESS;
            }
            else
            {
                int WriteLength = (int)(Stream.Connection.Send.MaxData - Stream.Connection.Send.OrderedStreamBytesReceived);
                Status =
                    QuicRecvBufferWrite(
                        Stream.RecvBuffer,
                        Frame.Data,
                        ref WriteLength,
                        ref ReadyToDeliver);
                if (QUIC_FAILED(Status))
                {
                    goto Error;
                }

                Stream.Connection.Send.OrderedStreamBytesReceived += WriteLength;
                NetLog.Assert(Stream.Connection.Send.OrderedStreamBytesReceived <= Stream.Connection.Send.MaxData);
                NetLog.Assert(Stream.Connection.Send.OrderedStreamBytesReceived >= WriteLength);

                if (EncryptedWith0Rtt)
                {
                    if (EndOffset > Stream.RecvMax0RttLength)
                    {
                        Stream.RecvMax0RttLength = EndOffset;
                    }
                }

                Stream.Connection.Stats.Recv.TotalStreamBytes += Frame.Data.Length;
            }

            if (Frame.Fin)
            {
                Stream.RecvMaxLength = EndOffset;
                if (Stream.RecvBuffer.BaseOffset == Stream.RecvMaxLength)
                {
                    ReadyToDeliver = true;
                }
            }

            if (ReadyToDeliver && (Stream.RecvBuffer.RecvMode == QUIC_RECV_BUF_MODE.QUIC_RECV_BUF_MODE_MULTIPLE ||
                 Stream.RecvBuffer.ReadPendingLength == 0))
            {
                Stream.Flags.ReceiveDataPending = true;
                QuicStreamRecvQueueFlush(Stream, Stream.RecvBuffer.BaseOffset == Stream.RecvMaxLength);
            }

        Error:

            if (Status == QUIC_STATUS_INVALID_PARAMETER)
            {
                QuicConnTransportError(Stream.Connection, QUIC_ERROR_FINAL_SIZE_ERROR);
            }
            else if (Status == QUIC_STATUS_BUFFER_TOO_SMALL)
            {
                QuicConnTransportError(Stream.Connection, QUIC_ERROR_FLOW_CONTROL_ERROR);
            }

            return Status;
        }

    }
}
