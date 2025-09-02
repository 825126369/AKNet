using AKNet.Common;
using System;

namespace MSQuic2
{
    internal static partial class MSQuicFunc
    {
        static bool QuicStreamRemoveOutFlowBlockedReason(QUIC_STREAM Stream, uint Reason)
        {
            if (BoolOk(Stream.OutFlowBlockedReasons & Reason))
            {
                long Now = CxPlatTimeUs();
                if (BoolOk(Stream.OutFlowBlockedReasons & QUIC_FLOW_BLOCKED_STREAM_FLOW_CONTROL) && BoolOk(Reason & QUIC_FLOW_BLOCKED_STREAM_FLOW_CONTROL))
                {
                    Stream.BlockedTimings.FlowControl.CumulativeTimeUs += CxPlatTimeDiff(Stream.BlockedTimings.FlowControl.LastStartTimeUs, Now);
                    Stream.BlockedTimings.FlowControl.LastStartTimeUs = 0;
                }

                if (BoolOk(Stream.OutFlowBlockedReasons & QUIC_FLOW_BLOCKED_APP) && BoolOk(Reason & QUIC_FLOW_BLOCKED_APP))
                {
                    Stream.BlockedTimings.App.CumulativeTimeUs += CxPlatTimeDiff(Stream.BlockedTimings.App.LastStartTimeUs, Now);
                    Stream.BlockedTimings.App.LastStartTimeUs = 0;
                }

                if (BoolOk(Stream.OutFlowBlockedReasons & QUIC_FLOW_BLOCKED_STREAM_ID_FLOW_CONTROL) && BoolOk(Reason & QUIC_FLOW_BLOCKED_STREAM_ID_FLOW_CONTROL))
                {
                    Stream.BlockedTimings.StreamIdFlowControl.CumulativeTimeUs += CxPlatTimeDiff(Stream.BlockedTimings.StreamIdFlowControl.LastStartTimeUs, Now);
                    Stream.BlockedTimings.StreamIdFlowControl.LastStartTimeUs = 0;
                }

                Stream.OutFlowBlockedReasons &= ~Reason;
                return true;
            }
            return false;
        }

        static void QuicStreamCancelRequests(QUIC_STREAM Stream)
        {
            while (Stream.SendRequests != null)
            {
                QUIC_SEND_REQUEST Req = Stream.SendRequests;
                Stream.SendRequests = Req.Next;
                QuicStreamCompleteSendRequest(Stream, Req, true, true);
            }
            Stream.SendRequestsTail = Stream.SendRequests = null;
        }

        static void QuicStreamIndicateSendShutdownComplete(QUIC_STREAM Stream, bool GracefulShutdown)
        {
            NetLog.Assert(!Stream.Flags.SendEnabled);
            NetLog.Assert(Stream.ApiSendRequests == null);
            NetLog.Assert(Stream.SendRequests == null);
            if (!Stream.Flags.HandleSendShutdown)
            {
                Stream.Flags.HandleSendShutdown = true;

                QUIC_STREAM_EVENT Event = new QUIC_STREAM_EVENT();
                Event.Type = QUIC_STREAM_EVENT_TYPE.QUIC_STREAM_EVENT_SEND_SHUTDOWN_COMPLETE;
                Event.SEND_SHUTDOWN_COMPLETE.Graceful = GracefulShutdown;
                QuicStreamIndicateEvent(Stream, ref Event);
            }
        }

        static void QuicStreamEnqueueSendRequest(QUIC_STREAM Stream, QUIC_SEND_REQUEST SendRequest)
        {
            Stream.Connection.SendBuffer.PostedBytes += SendRequest.TotalLength;
            QuicStreamRemoveOutFlowBlockedReason(Stream, QUIC_FLOW_BLOCKED_APP);

            SendRequest.StreamOffset = Stream.QueuedSendOffset;
            Stream.QueuedSendOffset += SendRequest.TotalLength;

            if (SendRequest.Flags.HasFlag(QUIC_SEND_FLAGS.QUIC_SEND_FLAG_ALLOW_0_RTT) && Stream.Queued0Rtt == SendRequest.StreamOffset)
            {
                Stream.Queued0Rtt = Stream.QueuedSendOffset;
            }

            if (Stream.SendBookmark == null)
            {
                Stream.SendBookmark = SendRequest;
            }

            if (Stream.SendBufferBookmark == null)
            {
                NetLog.Assert(Stream.SendRequests == null || Stream.SendRequests.Flags.HasFlag(QUIC_SEND_FLAGS.QUIC_SEND_FLAG_BUFFERED));
                Stream.SendBufferBookmark = SendRequest;
            }

            if (Stream.SendRequestsTail == null)
            {
                Stream.SendRequests = Stream.SendRequestsTail = SendRequest;
            }
            else
            {
                Stream.SendRequestsTail.Next = SendRequest;
                Stream.SendRequestsTail = SendRequest;
            }
        }



        //内部缓存一个待发送数据缓存，用于重传等
        static int QuicStreamSendBufferRequest(QUIC_STREAM Stream, QUIC_SEND_REQUEST Req)
        {
            QUIC_CONNECTION Connection = Stream.Connection;
            NetLog.Assert(Req.TotalLength <= int.MaxValue);

            if (Req.TotalLength != 0)
            {
                QUIC_SSBuffer Buf = QuicSendBufferAlloc(Connection.SendBuffer, Req.TotalLength);
                if (Buf.IsEmpty)
                {
                    return QUIC_STATUS_OUT_OF_MEMORY;
                }

                QUIC_SSBuffer CurBuf = Buf;
                for (int i = 0; i < Req.BufferCount; i++)
                {
                    Req.Buffers[i].GetSpan().CopyTo(CurBuf.GetSpan());
                    CurBuf += Req.Buffers[i].Length;
                }
                Req.InternalBuffer.Buffer = Buf.Buffer;
            }
            else
            {
                Req.InternalBuffer.Buffer = null;
            }

            Req.BufferCount = 1;
            Req.Buffers = new QUIC_BUFFER[1];
            Req.Buffers[0] = Req.InternalBuffer;
            Req.InternalBuffer.Length = Req.TotalLength;
            Req.Flags |= QUIC_SEND_FLAGS.QUIC_SEND_FLAG_BUFFERED;

            Stream.SendBufferBookmark = Req.Next;
            NetLog.Assert(Stream.SendBufferBookmark == null || !Stream.SendBufferBookmark.Flags.HasFlag(QUIC_SEND_FLAGS.QUIC_SEND_FLAG_BUFFERED));

            QUIC_STREAM_EVENT Event = new QUIC_STREAM_EVENT();
            Event.Type = QUIC_STREAM_EVENT_TYPE.QUIC_STREAM_EVENT_SEND_COMPLETE;
            Event.SEND_COMPLETE.Canceled = false;
            Event.SEND_COMPLETE.ClientContext = Req.ClientContext;
            QuicStreamIndicateEvent(Stream, ref Event);
            Req.ClientContext = null;
            return QUIC_STATUS_SUCCESS;
        }

        static void QuicStreamSendShutdown(QUIC_STREAM Stream, bool Graceful, bool Silent, bool DelaySend, int ErrorCode)
        {
            if (Stream.Flags.LocalCloseAcked)
            {
                goto Exit;
            }

            QuicStreamRemoveOutFlowBlockedReason(Stream, QUIC_FLOW_BLOCKED_APP);

            CxPlatDispatchLockAcquire(Stream.ApiSendRequestLock);
            Stream.Flags.SendEnabled = false;
            QUIC_SEND_REQUEST ApiSendRequests = Stream.ApiSendRequests;
            Stream.ApiSendRequests = null;
            CxPlatDispatchLockRelease(Stream.ApiSendRequestLock);

            if (Graceful)
            {
                NetLog.Assert(!Silent);
                if (Stream.Flags.LocalCloseFin || Stream.Flags.LocalCloseReset)
                {
                    goto Exit;
                }

                while (ApiSendRequests != null)
                {
                    QUIC_SEND_REQUEST SendRequest = ApiSendRequests;
                    ApiSendRequests = ApiSendRequests.Next;
                    QuicStreamCompleteSendRequest(Stream, SendRequest, true, false);
                }

                Stream.Flags.LocalCloseFin = true;
                QuicSendSetStreamSendFlag(Stream.Connection.Send, Stream, QUIC_STREAM_SEND_FLAG_FIN, DelaySend);
            }
            else if (Stream.ReliableOffsetSend == 0 || Stream.Flags.LocalCloseResetReliable)
            {
                QuicStreamRemoveOutFlowBlockedReason(Stream, QUIC_FLOW_BLOCKED_STREAM_FLOW_CONTROL);
                QuicStreamCancelRequests(Stream);
                while (ApiSendRequests != null)
                {
                    QUIC_SEND_REQUEST SendRequest = ApiSendRequests;
                    ApiSendRequests = ApiSendRequests.Next;
                    QuicStreamCompleteSendRequest(Stream, SendRequest, true, false);
                }

                if (Silent)
                {
                    QuicSendClearStreamSendFlag(Stream.Connection.Send, Stream, QUIC_STREAM_SEND_FLAGS_ALL);
                    Stream.Flags.LocalCloseAcked = true;
                    QuicStreamIndicateSendShutdownComplete(Stream, false);
                }

                if (Stream.Flags.LocalCloseReset)
                {
                    goto Exit;
                }

                Stream.Flags.LocalCloseReset = true;
                Stream.SendShutdownErrorCode = ErrorCode;

                if (!Silent)
                {
                    QuicSendSetStreamSendFlag(Stream.Connection.Send, Stream, QUIC_STREAM_SEND_FLAG_SEND_ABORT, false);
                    QuicSendClearStreamSendFlag(Stream.Connection.Send, Stream, QUIC_STREAM_SEND_FLAG_ALL_SEND_PATH);
                }
            }
            else
            {
                if (Stream.Flags.LocalCloseReset)
                {
                    goto Exit;
                }

                Stream.Flags.LocalCloseResetReliable = true;
                Stream.SendShutdownErrorCode = ErrorCode;

                while (ApiSendRequests != null)
                {
                    QUIC_SEND_REQUEST SendRequest = ApiSendRequests;
                    ApiSendRequests = ApiSendRequests.Next;
                    SendRequest.Next = null;
                    QuicStreamEnqueueSendRequest(Stream, SendRequest);

                    if (Stream.Connection.Settings.SendBufferingEnabled)
                    {
                        QuicSendBufferFill(Stream.Connection);
                    }

                    NetLog.Assert(Stream.SendRequests != null);
                    QuicStreamSendDumpState(Stream);
                }
                QuicSendSetStreamSendFlag(Stream.Connection.Send, Stream, QUIC_STREAM_SEND_FLAG_RELIABLE_ABORT, false);
            }
            QuicStreamSendDumpState(Stream);

        Exit:
            if (Silent)
            {
                QuicStreamTryCompleteShutdown(Stream);
            }
        }

        static void QuicStreamCompleteSendRequest(QUIC_STREAM Stream, QUIC_SEND_REQUEST SendRequest, bool Canceled, bool PreviouslyPosted)
        {
            QUIC_CONNECTION Connection = Stream.Connection;
            if (Stream.SendBookmark == SendRequest)
            {
                Stream.SendBookmark = SendRequest.Next;
            }
            if (Stream.SendBufferBookmark == SendRequest)
            {
                Stream.SendBufferBookmark = SendRequest.Next;
            }

            if (SendRequest.Flags.HasFlag(QUIC_SEND_FLAGS.QUIC_SEND_FLAG_START) && !Stream.Flags.Started)
            {
                QuicStreamIndicateStartComplete(Stream, QUIC_STATUS_ABORTED);
            }

            if (!SendRequest.Flags.HasFlag(QUIC_SEND_FLAGS.QUIC_SEND_FLAG_BUFFERED))
            {
                QUIC_STREAM_EVENT Event = new QUIC_STREAM_EVENT();
                Event.Type = QUIC_STREAM_EVENT_TYPE.QUIC_STREAM_EVENT_SEND_COMPLETE;
                Event.SEND_COMPLETE.Canceled = Canceled;
                Event.SEND_COMPLETE.ClientContext = SendRequest.ClientContext;

                if (Canceled)
                {

                }
                else
                {

                }

                QuicStreamIndicateEvent(Stream, ref Event);
            }
            else if (SendRequest.InternalBuffer.Length != 0)
            {
                QuicSendBufferFree(Connection.SendBuffer, SendRequest.InternalBuffer.Buffer, SendRequest.InternalBuffer.Length);
            }

            if (PreviouslyPosted)
            {
                NetLog.Assert(Connection.SendBuffer.PostedBytes >= SendRequest.TotalLength);
                Connection.SendBuffer.PostedBytes -= SendRequest.TotalLength;

                if (Connection.Settings.SendBufferingEnabled)
                {
                    QuicSendBufferFill(Connection);
                }
            }

            Connection.Partition.SendRequestPool.CxPlatPoolFree(SendRequest);
        }

        static void QuicStreamSendFlush(QUIC_STREAM Stream)
        {
            CxPlatDispatchLockAcquire(Stream.ApiSendRequestLock);
            QUIC_SEND_REQUEST ApiSendRequests = Stream.ApiSendRequests;
            Stream.ApiSendRequests = null;
            CxPlatDispatchLockRelease(Stream.ApiSendRequestLock);

            long TotalBytesSent = 0;
            bool Start = false;
            while (ApiSendRequests != null)
            {
                QUIC_SEND_REQUEST SendRequest = ApiSendRequests;
                ApiSendRequests = ApiSendRequests.Next;
                SendRequest.Next = null;

                TotalBytesSent += SendRequest.TotalLength;
                NetLog.Assert(!SendRequest.Flags.HasFlag(QUIC_SEND_FLAGS.QUIC_SEND_FLAG_BUFFERED));

                if (SendRequest.Flags.HasFlag(QUIC_SEND_FLAGS.QUIC_SEND_FLAG_CANCEL_ON_LOSS))
                {
                    Stream.Flags.CancelOnLoss = true;
                }

                if (!Stream.Flags.SendEnabled)
                {
                    QuicStreamCompleteSendRequest(Stream, SendRequest, true, false);
                    continue;
                }

                QuicStreamEnqueueSendRequest(Stream, SendRequest);

                if (SendRequest.Flags.HasFlag(QUIC_SEND_FLAGS.QUIC_SEND_FLAG_START) && !Stream.Flags.Started)
                {
                    Start = true;
                }

                if (SendRequest.Flags.HasFlag(QUIC_SEND_FLAGS.QUIC_SEND_FLAG_FIN))
                {
                    QuicStreamSendShutdown(Stream, true, false, SendRequest.Flags.HasFlag(QUIC_SEND_FLAGS.QUIC_SEND_FLAG_DELAY_SEND), 0);
                }

                QuicSendSetStreamSendFlag(Stream.Connection.Send, Stream, QUIC_STREAM_SEND_FLAG_DATA, SendRequest.Flags.HasFlag(QUIC_SEND_FLAGS.QUIC_SEND_FLAG_DELAY_SEND));

                if (Stream.Connection.Settings.SendBufferingEnabled)
                {
                    QuicSendBufferFill(Stream.Connection);
                }

                NetLog.Assert(Stream.SendRequests != null);
                QuicStreamSendDumpState(Stream);
            }

            if (Start)
            {
                QuicStreamStart(Stream, QUIC_STREAM_START_FLAGS.QUIC_STREAM_START_FLAG_IMMEDIATE | QUIC_STREAM_START_FLAGS.QUIC_STREAM_START_FLAG_SHUTDOWN_ON_FAIL, false);
            }

            QuicPerfCounterAdd(Stream.Connection.Partition, QUIC_PERFORMANCE_COUNTERS.QUIC_PERF_COUNTER_APP_SEND_BYTES, TotalBytesSent);
        }

        static bool QuicStreamOnLoss(QUIC_STREAM Stream, QUIC_SENT_FRAME_METADATA FrameMetadata)
        {
            if (Stream.Flags.LocalCloseReset)
            {
                return false;
            }

            if (Stream.Flags.LocalCloseResetReliableAcked && Stream.UnAckedOffset >= Stream.ReliableOffsetSend)
            {
                return false;
            }

            uint AddSendFlags = 0;
            long Start = FrameMetadata.StreamOffset;
            long End = FrameMetadata.StreamOffset +  FrameMetadata.StreamLength;

            if (BoolOk(FrameMetadata.Flags & QUIC_SENT_FRAME_FLAG_STREAM_OPEN) && !Stream.Flags.SendOpenAcked)
            {
                AddSendFlags |= QUIC_STREAM_SEND_FLAG_OPEN;
            }

            if (BoolOk(FrameMetadata.Flags & QUIC_SENT_FRAME_FLAG_STREAM_FIN) && !Stream.Flags.FinAcked)
            {
                AddSendFlags |= QUIC_STREAM_SEND_FLAG_FIN;
            }

            if (End <= Stream.UnAckedOffset)
            {
                goto Done; //这个Frame已经被确认了，无须再次确认
            }
            else if (Start < Stream.UnAckedOffset)
            {
                Start = Stream.UnAckedOffset;
            }

            QUIC_SUBRANGE Sack;
            int i = 0;
            while ((Sack = QuicRangeGetSafe(Stream.SparseAckRanges, i++)).Count > 0 && Sack.Low < (ulong)End)
            {
                //在已经被确认的ACK稀疏列表里，查找到还没被确认的集合
                if (Start < (long)Sack.End)
                {
                    if (Start >= (long)Sack.Low)
                    {
                        if (End <= (long)Sack.End)
                        {
                            goto Done; //已经被确认了
                        }
                        else
                        {
                            Start = (long)Sack.End;
                        }
                    }
                    else if (End <= (long)Sack.End)
                    {
                        End = (long)Sack.Low;
                    }
                    else
                    {

                    }
                }
            }

            bool UpdatedRecoveryWindow = false;
            if (Start < Stream.RecoveryNextOffset)
            {
                Stream.RecoveryNextOffset = Start;
                UpdatedRecoveryWindow = true;
            }

            if (Stream.RecoveryEndOffset < End)
            {
                Stream.RecoveryEndOffset = End;
                UpdatedRecoveryWindow = true;
            }

            if (UpdatedRecoveryWindow)
            {
                AddSendFlags |= QUIC_STREAM_SEND_FLAG_DATA;
            }

        Done:

            if (AddSendFlags != 0)
            {
                if (Stream.Flags.CancelOnLoss)
                {
                    QUIC_STREAM_EVENT Event = new QUIC_STREAM_EVENT();
                    Event.Type = QUIC_STREAM_EVENT_TYPE.QUIC_STREAM_EVENT_CANCEL_ON_LOSS;
                    Event.CANCEL_ON_LOSS.ErrorCode = 0;
                    QuicStreamIndicateEvent(Stream, ref Event);

                    QuicStreamShutdown(Stream, QUIC_STREAM_SHUTDOWN_FLAG_ABORT, Event.CANCEL_ON_LOSS.ErrorCode);
                    return false;
                }

                if (!Stream.Flags.InRecovery)
                {
                    Stream.Flags.InRecovery = true;
                }

                bool DataQueued = QuicSendSetStreamSendFlag(Stream.Connection.Send, Stream, AddSendFlags, false);
                QuicStreamSendDumpState(Stream);
                QuicStreamValidateRecoveryState(Stream);
                return DataQueued;
            }
            
            return false;
        }

        static bool QuicStreamAllowedByPeer(QUIC_STREAM Stream)
        {
            ulong StreamType = Stream.ID & STREAM_ID_MASK;
            long StreamCount = (long)(Stream.ID >> 2) + 1;
            ref QUIC_STREAM_TYPE_INFO Info = ref Stream.Connection.Streams.Types[StreamType];
            return Info.MaxTotalStreamCount >= StreamCount;
        }

        static bool QuicStreamSendCanWriteDataFrames(QUIC_STREAM Stream)
        {
            NetLog.Assert(QuicStreamAllowedByPeer(Stream));
            NetLog.Assert(HasStreamDataFrames(Stream.SendFlags));

            if (BoolOk(Stream.SendFlags & QUIC_STREAM_SEND_FLAG_OPEN))
            {
                return true;
            }

            if (Stream.RECOV_WINDOW_OPEN())
            {
                return true;
            }

            if (Stream.NextSendOffset == Stream.QueuedSendOffset)
            {
                return BoolOk(Stream.SendFlags & QUIC_STREAM_SEND_FLAG_FIN);
            }

            QUIC_SEND Send = Stream.Connection.Send;
            return Stream.NextSendOffset < Stream.MaxAllowedSendOffset && Send.OrderedStreamBytesSent < Send.PeerMaxData;
        }

        static bool QuicStreamCanSendNow(QUIC_STREAM Stream, bool ZeroRtt)
        {
            NetLog.Assert(Stream.SendFlags != 0);

            if (!QuicStreamAllowedByPeer(Stream))
            {
                return false;
            }

            if (HasStreamControlFrames(Stream.SendFlags) || BoolOk(Stream.SendFlags & QUIC_STREAM_SEND_FLAG_OPEN))
            {
                return true;
            }

            if (QuicStreamSendCanWriteDataFrames(Stream))
            {
                return ZeroRtt ? QuicStreamHasPending0RttData(Stream) : true;
            }

            return false;
        }

        static bool QuicStreamHasPending0RttData(QUIC_STREAM Stream)
        {
            return Stream.Queued0Rtt > Stream.NextSendOffset || (Stream.NextSendOffset == Stream.QueuedSendOffset && BoolOk(Stream.SendFlags & QUIC_STREAM_SEND_FLAG_FIN));
        }

        static void QuicStreamValidateRecoveryState(QUIC_STREAM Stream)
        {
            if (Stream.RECOV_WINDOW_OPEN())
            {
                QUIC_SUBRANGE Sack;
                int i = 0;
                while (!(Sack = QuicRangeGetSafe(Stream.SparseAckRanges, i++)).IsEmpty && (long)Sack.Low < Stream.RecoveryNextOffset)
                {
                    NetLog.Assert((long)Sack.End <= Stream.RecoveryNextOffset);
                }
            }
        }

        static void QuicStreamSendDumpState(QUIC_STREAM Stream)
        {
            //这里都是日志
        }

        static void QuicStreamOnResetAck(QUIC_STREAM Stream)
        {
            if (!Stream.Flags.LocalCloseAcked)
            {
                Stream.Flags.LocalCloseAcked = true;
                QuicStreamIndicateSendShutdownComplete(Stream, false);
                QuicStreamTryCompleteShutdown(Stream);
            }
        }

        static void QuicStreamOnResetReliableAck(QUIC_STREAM Stream)
        {
            NetLog.Assert(Stream.Flags.LocalCloseResetReliable);
            if (Stream.UnAckedOffset >= Stream.ReliableOffsetSend && !Stream.Flags.LocalCloseAcked)
            {
                Stream.Flags.LocalCloseResetReliableAcked = true;
                Stream.Flags.LocalCloseAcked = true;
                QuicSendClearStreamSendFlag(Stream.Connection.Send, Stream, QUIC_STREAM_SEND_FLAG_ALL_SEND_PATH);
                QuicStreamCancelRequests(Stream);
                QuicStreamIndicateSendShutdownComplete(Stream, false);
                QuicStreamTryCompleteShutdown(Stream);
            }
            else
            {
                Stream.Flags.LocalCloseResetReliableAcked = true;
            }
        }

        static bool QuicStreamSendWrite(QUIC_STREAM Stream, QUIC_PACKET_BUILDER Builder)
        {
            NetLog.Assert(Builder.Metadata.FrameCount < QUIC_MAX_FRAMES_PER_PACKET);
            int PrevFrameCount = Builder.Metadata.FrameCount;
            bool RanOutOfRoom = false;
            bool IsInitial =
                (Stream.Connection.Stats.QuicVersion != QUIC_VERSION_2 && Builder.PacketType == (byte)QUIC_LONG_HEADER_TYPE_V1.QUIC_INITIAL_V1) ||
                (Stream.Connection.Stats.QuicVersion == QUIC_VERSION_2 && Builder.PacketType == (byte)QUIC_LONG_HEADER_TYPE_V2.QUIC_INITIAL_V2);

            NetLog.Assert(Stream.SendFlags != 0);
            NetLog.Assert(Builder.Metadata.Flags.KeyType == QUIC_PACKET_KEY_TYPE.QUIC_PACKET_KEY_1_RTT ||
                Builder.Metadata.Flags.KeyType == QUIC_PACKET_KEY_TYPE.QUIC_PACKET_KEY_0_RTT);
            NetLog.Assert(QuicStreamAllowedByPeer(Stream));

            //告知发送方我接受的最大数据量,非公开协议
            if (BoolOk(Stream.SendFlags & QUIC_STREAM_SEND_FLAG_MAX_DATA))
            {
                QUIC_MAX_STREAM_DATA_EX Frame = new QUIC_MAX_STREAM_DATA_EX() 
                {
                    StreamID = Stream.ID,
                    MaximumData = Stream.MaxAllowedRecvOffset
                };

                var mBuf = Builder.GetDatagramCanWriteSSBufer();
                if (QuicMaxStreamDataFrameEncode(Frame, ref mBuf))
                {
                    Builder.SetDatagramOffset(mBuf);
                    Stream.SendFlags &= ~QUIC_STREAM_SEND_FLAG_MAX_DATA;
                    if (QuicPacketBuilderAddStreamFrame(Builder, Stream, QUIC_FRAME_TYPE.QUIC_FRAME_MAX_STREAM_DATA))
                    {
                        return true;
                    }
                }
                else
                {
                    RanOutOfRoom = true;
                }
            }

            //用于标识当前流的发送操作已经被中止（aborted）。不可靠
            if (BoolOk(Stream.SendFlags & QUIC_STREAM_SEND_FLAG_SEND_ABORT))
            {
                QUIC_RESET_STREAM_EX Frame = new QUIC_RESET_STREAM_EX() {
                    StreamID = Stream.ID,
                    ErrorCode = Stream.SendShutdownErrorCode,
                    FinalSize = Stream.MaxSentLength
                };

                var mBuf = Builder.GetDatagramCanWriteSSBufer();
                if (QuicResetStreamFrameEncode(Frame, ref mBuf))
                {
                    Builder.SetDatagramOffset(mBuf);
                    Stream.SendFlags &= ~QUIC_STREAM_SEND_FLAG_SEND_ABORT;
                    if (QuicPacketBuilderAddStreamFrame(Builder, Stream, QUIC_FRAME_TYPE.QUIC_FRAME_RESET_STREAM))
                    {
                        return true;
                    }
                }
                else
                {
                    RanOutOfRoom = true;
                }
            }

            //内部使用的发送标志（send flag），用于表示当前流的发送操作【已经被可靠】地中止（reliably aborted）
            if (BoolOk(Stream.SendFlags & QUIC_STREAM_SEND_FLAG_RELIABLE_ABORT))
            {
                QUIC_RELIABLE_RESET_STREAM_EX Frame = new QUIC_RELIABLE_RESET_STREAM_EX() {
                    StreamID = Stream.ID,
                    ErrorCode = Stream.SendShutdownErrorCode,
                    FinalSize = Stream.MaxSentLength,
                    ReliableSize = Stream.ReliableOffsetSend
                };

                var mBuf = Builder.GetDatagramCanWriteSSBufer();
                if (QuicReliableResetFrameEncode(Frame, ref mBuf))
                {
                    Builder.SetDatagramOffset(mBuf);
                    Stream.SendFlags &= ~QUIC_STREAM_SEND_FLAG_RELIABLE_ABORT;
                    if (QuicPacketBuilderAddStreamFrame(Builder, Stream, QUIC_FRAME_TYPE.QUIC_FRAME_RELIABLE_RESET_STREAM))
                    {
                        return true;
                    }
                }
                else
                {
                    RanOutOfRoom = true;
                }
            }

            //内部使用的发送标志（send flag），用于表示当前流的接收端已被中止（即对端主动关闭了接收路径），因此本地不应再尝试发送更多数据
            if (BoolOk(Stream.SendFlags & QUIC_STREAM_SEND_FLAG_RECV_ABORT))
            {
                QUIC_STOP_SENDING_EX Frame = new QUIC_STOP_SENDING_EX()
                {
                    StreamID = Stream.ID,
                    ErrorCode = Stream.RecvShutdownErrorCode
                };

                var mBuf = Builder.GetDatagramCanWriteSSBufer();
                if (QuicStopSendingFrameEncode(Frame, ref mBuf))
                {
                    Builder.SetDatagramOffset(mBuf);
                    Stream.SendFlags &= ~QUIC_STREAM_SEND_FLAG_RECV_ABORT;
                    if (QuicPacketBuilderAddStreamFrame(Builder, Stream, QUIC_FRAME_TYPE.QUIC_FRAME_STOP_SENDING))
                    {
                        return true;
                    }
                }
                else
                {
                    RanOutOfRoom = true;
                }
            }

            //正常流的数据
            if (HasStreamDataFrames(Stream.SendFlags) && QuicStreamSendCanWriteDataFrames(Stream))
            {
                var mBuf = Builder.GetDatagramCanWriteSSBufer();
                QuicStreamWriteStreamFrames(
                    Stream,
                    IsInitial,
                    Builder.Metadata,
                    ref mBuf);
                
                if (mBuf.Offset > Builder.DatagramLength)
                {
                    Builder.SetDatagramOffset(mBuf);
                    if (!QuicStreamHasPendingStreamData(Stream))
                    {
                        Stream.SendFlags &= ~QUIC_STREAM_SEND_FLAG_DATA;
                    }

                    if (Builder.Metadata.FrameCount == QUIC_MAX_FRAMES_PER_PACKET)
                    {
                        return true;
                    }
                }
                else
                {
                    RanOutOfRoom = true;
                }
            }

            //指示当前发送操作由于流量控制限制（flow control limit) 而被阻塞。
            if (BoolOk(Stream.SendFlags & QUIC_STREAM_SEND_FLAG_DATA_BLOCKED))
            {
                QUIC_STREAM_DATA_BLOCKED_EX Frame = new QUIC_STREAM_DATA_BLOCKED_EX()
                {
                    StreamID = Stream.ID,
                    StreamDataLimit = (int)Stream.NextSendOffset
                };

                var mBuf = Builder.GetDatagramCanWriteSSBufer();
                if (QuicStreamDataBlockedFrameEncode(Frame, ref mBuf))
                {
                    Builder.SetDatagramOffset(mBuf);
                    Stream.SendFlags &= ~QUIC_STREAM_SEND_FLAG_DATA_BLOCKED;
                    if (QuicPacketBuilderAddStreamFrame(Builder, Stream, QUIC_FRAME_TYPE.QUIC_FRAME_STREAM_DATA_BLOCKED))
                    {
                        return true;
                    }
                }
                else
                {
                    RanOutOfRoom = true;
                }
            }

            NetLog.Assert(Builder.Metadata.FrameCount > PrevFrameCount || RanOutOfRoom);
            return Builder.Metadata.FrameCount > PrevFrameCount;
        }

        static void QuicStreamWriteStreamFrames(QUIC_STREAM Stream, bool ExplicitDataLength, QUIC_SENT_PACKET_METADATA PacketMetadata, ref QUIC_SSBuffer Buffer)
        {
            QUIC_SEND Send = Stream.Connection.Send;
            ExplicitDataLength = true;

            while (Buffer.Length > 0 && PacketMetadata.FrameCount < QUIC_MAX_FRAMES_PER_PACKET)
            {
                long Left;
                long Right;
                bool Recovery;
                if (Stream.RECOV_WINDOW_OPEN())
                {
                    Left = (int)Stream.RecoveryNextOffset;
                    Recovery = true;
                }
                else
                {
                    Left = (int)Stream.NextSendOffset;
                    Recovery = false;
                }
                Right = Left + Buffer.Length; //刚开始设置 Right 为理想的最大值

                if (Recovery && Right > Stream.RecoveryEndOffset && Stream.RecoveryEndOffset != Stream.NextSendOffset)
                {
                    Right = Stream.RecoveryEndOffset;
                }

                QUIC_SUBRANGE Sack = QUIC_SUBRANGE.Empty;
                if (Left != Stream.MaxSentLength)
                {
                    //在发送数据时，跳过已经被对端确认（SACKed）的数据范围，避免重复发送。
                    int i = 0;
                    while (!(Sack = QuicRangeGetSafe(Stream.SparseAckRanges, i++)).IsEmpty && (long)Sack.Low < Left)
                    {
                        NetLog.Assert((long)Sack.End <= Left);
                    }
                }

                if (!Sack.IsEmpty)
                {
                    if (Right > (long)Sack.Low)
                    {
                        Right = (long)Sack.Low;
                    }
                }
                else
                {
                    if (Right > Stream.QueuedSendOffset) //这个偏移是最大值
                    {
                        Right = Stream.QueuedSendOffset;
                    }
                }

                //流级的流量控制，为啥不是 Right - Left
                //流控是基于偏移量的，不是基于长度的
                if (Right > Stream.MaxAllowedSendOffset)
                {
                    Right = Stream.MaxAllowedSendOffset;
                }

                //连接级的流量控制
                long MaxConnFlowControlOffset = Stream.MaxSentLength + (Send.PeerMaxData - Send.OrderedStreamBytesSent);
                if (Right > MaxConnFlowControlOffset)
                {
                    Right = MaxConnFlowControlOffset;
                }

                NetLog.Assert(Right >= Left);
                int FramePayloadBytes = (int)(Right - Left);
                QuicStreamWriteOneFrame(
                    Stream, 
                    ExplicitDataLength,
                    (int)Left,
                    ref FramePayloadBytes,
                    ref Buffer,
                    PacketMetadata);

                bool ExitLoop = false;
                if (FramePayloadBytes == 0)
                {
                    ExitLoop = true;
                }

                Right = Left + FramePayloadBytes;

                NetLog.Assert(Right <= Stream.QueuedSendOffset);
                if (Right == Stream.QueuedSendOffset)
                {
                    if (Stream.Flags.SendEnabled)
                    {
                        QuicStreamAddOutFlowBlockedReason(Stream, QUIC_FLOW_BLOCKED_APP);
                    }
                    ExitLoop = true;
                }

                NetLog.Assert(Right <= Stream.MaxAllowedSendOffset);
                if (Right == Stream.MaxAllowedSendOffset)
                {
                    if (QuicStreamAddOutFlowBlockedReason(Stream, QUIC_FLOW_BLOCKED_STREAM_FLOW_CONTROL))
                    {
                        QuicSendSetStreamSendFlag(Stream.Connection.Send, Stream, QUIC_STREAM_SEND_FLAG_DATA_BLOCKED, false);
                    }
                    ExitLoop = true;
                }

                NetLog.Assert(Right <= MaxConnFlowControlOffset);
                if (Right == MaxConnFlowControlOffset)
                {
                    if (QuicConnAddOutFlowBlockedReason(Stream.Connection, QUIC_FLOW_BLOCKED_CONN_FLOW_CONTROL))
                    {
                        QuicSendSetSendFlag(Stream.Connection.Send, QUIC_CONN_SEND_FLAG_DATA_BLOCKED);
                    }
                    ExitLoop = true;
                }

                if (Recovery)
                {
                    NetLog.Assert(Stream.RecoveryNextOffset <= Right);
                    Stream.RecoveryNextOffset = Right;
                    if (!Sack.IsEmpty && Stream.RecoveryNextOffset == (long)Sack.Low)
                    {
                        Stream.RecoveryNextOffset += Sack.Count;
                    }
                }

                if (Stream.NextSendOffset < Right)
                {
                    Stream.NextSendOffset = (int)Right;
                    if (!Sack.IsEmpty && Stream.NextSendOffset == (long)Sack.Low)
                    {
                        Stream.NextSendOffset += Sack.Count;
                    }
                }

                if (Stream.MaxSentLength < Right)
                {
                    Send.OrderedStreamBytesSent += (int)Right - Stream.MaxSentLength;
                    NetLog.Assert(Send.OrderedStreamBytesSent <= Send.PeerMaxData);
                    Stream.MaxSentLength = (int)Right;
                }

                QuicStreamValidateRecoveryState(Stream);

                if (ExitLoop)
                {
                    break;
                }
            }

            QuicStreamSendDumpState(Stream);
        }

        static void QuicStreamWriteOneFrame(QUIC_STREAM Stream, bool ExplicitDataLength,int Offset, ref int FramePayloadBytes, ref QUIC_SSBuffer Buffer,  QUIC_SENT_PACKET_METADATA PacketMetadata)
        {
            QUIC_STREAM_EX Frame = new QUIC_STREAM_EX()
            {
                Fin = false,
                ExplicitLength = ExplicitDataLength,
                StreamID = Stream.ID,
                Offset = Offset,
                Length = 0,
            };

            int HeaderLength = QuicStreamFrameHeaderSize(Frame);
            if (Buffer.Length < HeaderLength)
            {
                FramePayloadBytes = 0;
                Buffer.Length = 0;
                return;
            }

            Frame.Length = Buffer.Length - HeaderLength;
            if (Frame.Length > FramePayloadBytes)
            {
                Frame.Length = FramePayloadBytes;
            }

            if (Frame.Length > 0)
            {
                NetLog.Assert(Offset < Stream.QueuedSendOffset);
                if (Frame.Length > Stream.QueuedSendOffset - Offset)
                {
                    Frame.Length = Stream.QueuedSendOffset - Offset;
                    NetLog.Assert(Frame.Length > 0);
                }
                
                Frame.Data.SetData(Buffer + HeaderLength);
                //这里先编码Data，后面的话编码头部
                QuicStreamCopyFromSendRequests(Stream, Offset, Frame.Data.Slice(0, Frame.Length));
                Stream.Connection.Stats.Send.TotalStreamBytes += Frame.Length;
            }

            if (BoolOk(Stream.SendFlags & QUIC_STREAM_SEND_FLAG_FIN) && Frame.Offset + Frame.Length == Stream.QueuedSendOffset)
            {
                Frame.Fin = true;
            }
            else if (Frame.Length == 0 && !BoolOk(Stream.SendFlags & QUIC_STREAM_SEND_FLAG_OPEN))
            {
                FramePayloadBytes = 0;
                return;
            }

            FramePayloadBytes = (ushort)Frame.Length;
            if (!QuicStreamFrameEncode(Frame, ref Buffer))
            {
                NetLog.Assert(false);
            }

            PacketMetadata.Flags.IsAckEliciting = true;
            PacketMetadata.Frames[PacketMetadata.FrameCount].Type =  QUIC_FRAME_TYPE.QUIC_FRAME_STREAM;
            PacketMetadata.Frames[PacketMetadata.FrameCount].STREAM.Stream = Stream;
            PacketMetadata.Frames[PacketMetadata.FrameCount].StreamOffset = Frame.Offset;
            PacketMetadata.Frames[PacketMetadata.FrameCount].StreamLength = Frame.Length;
            PacketMetadata.Frames[PacketMetadata.FrameCount].Flags = 0;

            if (BoolOk(Stream.SendFlags & QUIC_STREAM_SEND_FLAG_OPEN))
            {
                Stream.SendFlags &= ~QUIC_STREAM_SEND_FLAG_OPEN;
                PacketMetadata.Frames[PacketMetadata.FrameCount].Flags |= QUIC_SENT_FRAME_FLAG_STREAM_OPEN;
            }
            if (Frame.Fin)
            {
                Stream.SendFlags &= ~QUIC_STREAM_SEND_FLAG_FIN;
                PacketMetadata.Frames[PacketMetadata.FrameCount].Flags |= QUIC_SENT_FRAME_FLAG_STREAM_FIN;
            }
            QuicStreamSentMetadataIncrement(Stream);
            PacketMetadata.FrameCount++;
        }

        static void QuicStreamCopyFromSendRequests(QUIC_STREAM Stream, int Offset, QUIC_SSBuffer Buf)
        {
            NetLog.Assert(Buf.Length > 0);
            NetLog.Assert(Stream.SendRequests != null);
            NetLog.Assert(Offset >= Stream.SendRequests.StreamOffset);
            
            // 查找包含第一个字节的发送请求，如果可能的话使用书签（bookmark）。
            // 如果调用者请求的是书签之前的字节（例如用于重传），则必须进行完整搜索。
            QUIC_SEND_REQUEST Req = null;
            if (Stream.SendBookmark != null && Stream.SendBookmark.StreamOffset <= Offset)
            {
                //重传也可能走这
                Req = Stream.SendBookmark;
            }
            else
            {
                //重传的时候，Stream.SendBookmark.StreamOffset <= Offset 这个条件如果不满足，所以走这里
                //如果调用者请求的是书签之前的字节（例如用于重传），则必须进行完整搜索。
                //NetLog.Log("重传");
                //重传也可能走这
                Req = Stream.SendRequests;
            }

            NetLog.Assert(Req.Buffers[0].Buffer != null, "Req.Buffers[0].Buffer == nul");
            while (Req.StreamOffset + Req.TotalLength <= Offset)
            {
                NetLog.Assert(Req.Next != null);
                Req = Req.Next;
            }
            NetLog.Assert(Req != null); //上面 选择了一个当前的 Request

            int CurIndex = 0;
            int CurOffset = Offset - (int)Req.StreamOffset;
            while (CurOffset >= Req.Buffers[CurIndex].Length)
            {
                CurOffset -= Req.Buffers[CurIndex++].Length;
            }

            for (; ; )
            {
                NetLog.Assert(Req != null);
                NetLog.Assert(CurIndex < Req.BufferCount);
                NetLog.Assert(CurOffset < Req.Buffers[CurIndex].Length);
                NetLog.Assert(Buf.Length > 0);

                int BufferLeft = Req.Buffers[CurIndex].Length - CurOffset;
                int CopyLength = Buf.Length < BufferLeft ? Buf.Length : BufferLeft;
                NetLog.Assert(CopyLength > 0);

                Req.Buffers[CurIndex].Buffer.AsSpan().Slice(CurOffset, CopyLength).CopyTo(Buf.GetSpan());
                Buf += CopyLength;

                if (Buf.Length == 0)
                {
                    break;
                }

                CurOffset = 0;
                //找到下一个非零长度的数据块进行发送。
                do
                {
                    if (++CurIndex == Req.BufferCount)
                    {
                        CurIndex = 0;
                        NetLog.Assert(Req.Next != null);
                        Req = Req.Next;
                    }
                } while (Req.Buffers[CurIndex].Length == 0);
            }

            Stream.SendBookmark = Req;//设置新的 下次要发送的书签
        }

        static bool QuicStreamHasPendingStreamData(QUIC_STREAM Stream)
        {
            return Stream.RECOV_WINDOW_OPEN() || (Stream.NextSendOffset < Stream.QueuedSendOffset);
        }

        static void QuicStreamOnAck(QUIC_STREAM Stream, QUIC_SEND_PACKET_FLAGS PacketFlags, QUIC_SENT_FRAME_METADATA FrameMetadata)
        {
            int Offset = FrameMetadata.StreamOffset;
            int Length = FrameMetadata.StreamLength;

            int FollowingOffset = Offset + Length;
            uint RemoveSendFlags = 0;
            NetLog.Assert(FollowingOffset <= Stream.QueuedSendOffset);

            if (PacketFlags.KeyType ==  QUIC_PACKET_KEY_TYPE.QUIC_PACKET_KEY_0_RTT && Stream.Sent0Rtt < FollowingOffset)
            {
                Stream.Sent0Rtt = FollowingOffset;
            }

            if (!Stream.Flags.SendOpenAcked)
            {
                Stream.Flags.SendOpenAcked = true;
                RemoveSendFlags |= QUIC_STREAM_SEND_FLAG_OPEN;
            }

            if (BoolOk(FrameMetadata.Flags & QUIC_SENT_FRAME_FLAG_STREAM_FIN))
            {
                Stream.Flags.FinAcked = true;
                RemoveSendFlags |= QUIC_STREAM_SEND_FLAG_FIN;
            }

            if (Offset <= Stream.UnAckedOffset)
            {
                if (Stream.UnAckedOffset < FollowingOffset)
                {
                    Stream.UnAckedOffset = FollowingOffset;
                    QuicRangeSetMin(Stream.SparseAckRanges, (ulong)Stream.UnAckedOffset);

                    QUIC_SUBRANGE Sack = QuicRangeGetSafe(Stream.SparseAckRanges, 0);
                    if (!Sack.IsEmpty && Sack.Low == (ulong)Stream.UnAckedOffset)
                    {
                        Stream.UnAckedOffset = (long)(Sack.Low + (ulong)Sack.Count);
                        QuicRangeRemoveSubranges(Stream.SparseAckRanges, 0, 1);
                    }

                    if (Stream.NextSendOffset < Stream.UnAckedOffset)
                    {
                        Stream.NextSendOffset = Stream.UnAckedOffset;
                    }
                    if (Stream.RecoveryNextOffset < Stream.UnAckedOffset)
                    {
                        Stream.RecoveryNextOffset = Stream.UnAckedOffset;
                    }
                    if (Stream.RecoveryEndOffset < Stream.UnAckedOffset)
                    {
                        Stream.Flags.InRecovery = false;
                    }
                }
                
                while (Stream.SendRequests != null)
                {
                    QUIC_SEND_REQUEST Req = Stream.SendRequests;
                    if (Req.StreamOffset + Req.TotalLength > Stream.UnAckedOffset)
                    {
                        break;
                    }

                    Stream.SendRequests = Req.Next;
                    if (Stream.SendRequests == null)
                    {
                        Stream.SendRequestsTail = Stream.SendRequests;
                    }

                    QuicStreamCompleteSendRequest(Stream, Req, false, true);
                }

                if (Stream.UnAckedOffset == Stream.QueuedSendOffset && Stream.Flags.FinAcked)
                {
                    NetLog.Assert(Stream.SendRequests == null);
                    if (!Stream.Flags.LocalCloseAcked)
                    {
                        Stream.Flags.LocalCloseAcked = true;
                        QuicStreamIndicateSendShutdownComplete(Stream, true);
                        QuicStreamTryCompleteShutdown(Stream);
                    }
                }
            }
            else
            {

                bool SacksUpdated = false;
                QUIC_SUBRANGE Sack = QuicRangeAddRange(Stream.SparseAckRanges, (ulong)Offset, Length, out SacksUpdated);
                if (Sack.IsEmpty)
                {
                    QuicConnTransportError(Stream.Connection, QUIC_ERROR_INTERNAL_ERROR);
                }
                else if (SacksUpdated)
                {
                    if (Stream.NextSendOffset >= (long)Sack.Low &&
                        Stream.NextSendOffset < (long)Sack.Low + Sack.Count)
                    {
                        Stream.NextSendOffset = (long)Sack.Low + Sack.Count;
                    }
                    if (Stream.RecoveryNextOffset >= (long)Sack.Low && Stream.RecoveryNextOffset < (long)Sack.Low + Sack.Count)
                    {
                        Stream.RecoveryNextOffset = (long)Sack.Low + Sack.Count;
                    }
                }
            }
            
            bool ReliableResetShutdown =
                !Stream.Flags.LocalCloseAcked &&
                Stream.Flags.LocalCloseResetReliableAcked &&
                Stream.UnAckedOffset >= Stream.ReliableOffsetSend;
            if (ReliableResetShutdown)
            {
                Stream.Flags.LocalCloseAcked = true;
                QuicSendClearStreamSendFlag(Stream.Connection.Send, Stream, QUIC_STREAM_SEND_FLAG_ALL_SEND_PATH);
                QuicStreamCancelRequests(Stream);
                QuicStreamIndicateSendShutdownComplete(Stream, false);
                QuicStreamTryCompleteShutdown(Stream);
            }

            if (!QuicStreamHasPendingStreamData(Stream))
            {
                RemoveSendFlags |= QUIC_STREAM_SEND_FLAG_DATA;
            }

            if (RemoveSendFlags != 0)
            {
                QuicSendClearStreamSendFlag(
                    Stream.Connection.Send,
                    Stream,
                    RemoveSendFlags);
            }

            QuicStreamSendDumpState(Stream);
            QuicStreamValidateRecoveryState(Stream);
        }

    }
}
