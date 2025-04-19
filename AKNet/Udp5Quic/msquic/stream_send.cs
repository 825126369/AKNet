using AKNet.Common;
using AKNet.Udp5Quic.Common;
using System;
using System.IO;
using System.Threading;
using static System.Net.WebRequestMethods;

namespace AKNet.Udp5Quic.Common
{
    internal static partial class MSQuicFunc
    {
        static bool QuicStreamRemoveOutFlowBlockedReason(QUIC_STREAM Stream, uint Reason)
        {
            if (BoolOk(Stream.OutFlowBlockedReasons & Reason))
            {
                long Now = mStopwatch.ElapsedMilliseconds;
                if (BoolOk(Stream.OutFlowBlockedReasons & QUIC_FLOW_BLOCKED_STREAM_FLOW_CONTROL) && BoolOk(Reason & QUIC_FLOW_BLOCKED_STREAM_FLOW_CONTROL))
                {
                    Stream.BlockedTimings.FlowControl.CumulativeTimeUs += CxPlatTimeDiff64(Stream.BlockedTimings.FlowControl.LastStartTimeUs, Now);
                    Stream.BlockedTimings.FlowControl.LastStartTimeUs = 0;
                }

                if (BoolOk(Stream.OutFlowBlockedReasons & QUIC_FLOW_BLOCKED_APP) && BoolOk(Reason & QUIC_FLOW_BLOCKED_APP))
                {
                    Stream.BlockedTimings.App.CumulativeTimeUs += CxPlatTimeDiff64(Stream.BlockedTimings.App.LastStartTimeUs, Now);
                    Stream.BlockedTimings.App.LastStartTimeUs = 0;
                }

                if (BoolOk(Stream.OutFlowBlockedReasons & QUIC_FLOW_BLOCKED_STREAM_ID_FLOW_CONTROL) && BoolOk(Reason & QUIC_FLOW_BLOCKED_STREAM_ID_FLOW_CONTROL))
                {
                    Stream.BlockedTimings.StreamIdFlowControl.CumulativeTimeUs += CxPlatTimeDiff64(Stream.BlockedTimings.StreamIdFlowControl.LastStartTimeUs, Now);
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
            Stream.SendRequestsTail = Stream.SendRequests;
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
                QuicStreamIndicateEvent(Stream, Event);
            }
        }

        static void QuicStreamEnqueueSendRequest(QUIC_STREAM Stream, QUIC_SEND_REQUEST SendRequest)
        {
            Stream.Connection.SendBuffer.PostedBytes += SendRequest.TotalLength;
            QuicStreamRemoveOutFlowBlockedReason(Stream, QUIC_FLOW_BLOCKED_APP);

            SendRequest.StreamOffset = Stream.QueuedSendOffset;
            Stream.QueuedSendOffset += SendRequest.TotalLength;

            if (BoolOk(SendRequest.Flags & QUIC_SEND_FLAG_ALLOW_0_RTT) && Stream.Queued0Rtt == SendRequest.StreamOffset)
            {
                Stream.Queued0Rtt = Stream.QueuedSendOffset;
            }

            if (Stream.SendBookmark == null)
            {
                Stream.SendBookmark = SendRequest;
            }
            if (Stream.SendBufferBookmark == null)
            {
                NetLog.Assert(Stream.SendRequests == null || BoolOk(Stream.SendRequests.Flags & QUIC_SEND_FLAG_BUFFERED));
                Stream.SendBufferBookmark = SendRequest;
            }

            Stream.SendRequestsTail = SendRequest;
            Stream.SendRequestsTail = SendRequest.Next;
        }

        static ulong QuicStreamSendBufferRequest(QUIC_STREAM Stream, QUIC_SEND_REQUEST Req)
        {
            QUIC_CONNECTION Connection = Stream.Connection;
            NetLog.Assert(Req.TotalLength <= uint.MaxValue);

            if (Req.TotalLength != 0)
            {
                byte[] Buf = QuicSendBufferAlloc(Connection.SendBuffer, Req.TotalLength);
                if (Buf == null)
                {
                    return QUIC_STATUS_OUT_OF_MEMORY;
                }

                byte[] CurBuf = Buf;
                for (int i = 0; i < Req.Buffers.Count; i++)
                {
                    System.Buffer.BlockCopy(Req.Buffers[i].Buffer, 0, CurBuf, 0, Req.Buffers[i].Length);
                    CurBuf += Req.Buffers[i].Length;
                }
                Req.InternalBuffer.Buffer = Buf;
            }
            else
            {
                Req.InternalBuffer.Buffer = null;
            }

            Req.Buffers.Count = 1;
            Req.Buffers = Req.InternalBuffer;
            Req.InternalBuffer.Length = Req.TotalLength;

            Req.Flags |= QUIC_SEND_FLAG_BUFFERED;
            Stream.SendBufferBookmark = Req.Next;
            NetLog.Assert(Stream.SendBufferBookmark == null || !BoolOk(Stream.SendBufferBookmark.Flags & QUIC_SEND_FLAG_BUFFERED));

            QUIC_STREAM_EVENT Event = new QUIC_STREAM_EVENT();
            Event.Type = QUIC_STREAM_EVENT_TYPE.QUIC_STREAM_EVENT_SEND_COMPLETE;
            Event.SEND_COMPLETE.Canceled = false;
            QuicStreamIndicateEvent(Stream, Event);
            return QUIC_STATUS_SUCCESS;
        }

        static void QuicStreamSendShutdown(QUIC_STREAM Stream, bool Graceful,bool Silent, bool DelaySend,ulong ErrorCode)
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

        static void QuicStreamCompleteSendRequest(QUIC_STREAM Stream,QUIC_SEND_REQUEST SendRequest, bool Canceled,bool PreviouslyPosted)
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

            if (BoolOk(SendRequest.Flags & QUIC_SEND_FLAG_START) && !Stream.Flags.Started)
            {
                QuicStreamIndicateStartComplete(Stream, QUIC_STATUS_ABORTED);
            }

            if (!BoolOk(SendRequest.Flags & QUIC_SEND_FLAG_BUFFERED))
            {
                QUIC_STREAM_EVENT Event = new QUIC_STREAM_EVENT();
                Event.Type =   QUIC_STREAM_EVENT_TYPE.QUIC_STREAM_EVENT_SEND_COMPLETE;
                Event.SEND_COMPLETE.Canceled = Canceled;
                Event.SEND_COMPLETE.ClientContext = SendRequest.ClientContext;

                if (Canceled)
                {
                   
                }
                else
                {
                    
                }

                QuicStreamIndicateEvent(Stream,Event);
            }
            else if (SendRequest.InternalBuffer.Length != 0)
            {
                QuicSendBufferFree(Connection.SendBuffer,SendRequest.InternalBuffer.Buffer, SendRequest.InternalBuffer.Length);
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

            Connection.Worker.SendRequestPool.CxPlatPoolFree(SendRequest);
        }

        static void QuicStreamSendFlush(QUIC_STREAM Stream)
        {
            Monitor.Enter(Stream.ApiSendRequestLock);
            QUIC_SEND_REQUEST ApiSendRequests = Stream.ApiSendRequests;
            Stream.ApiSendRequests = null;
            Monitor.Exit(Stream.ApiSendRequestLock);

            long TotalBytesSent = 0;
            bool Start = false;
            while (ApiSendRequests != null)
            {
                QUIC_SEND_REQUEST SendRequest = ApiSendRequests;
                ApiSendRequests = ApiSendRequests.Next;
                SendRequest.Next = null;
                TotalBytesSent += SendRequest.TotalLength;

                NetLog.Assert(!BoolOk(SendRequest.Flags & QUIC_SEND_FLAG_BUFFERED));

                if (!Stream.Flags.CancelOnLoss && BoolOk(SendRequest.Flags & QUIC_SEND_FLAG_CANCEL_ON_LOSS))
                {
                    Stream.Flags.CancelOnLoss = true;
                }

                if (!Stream.Flags.SendEnabled)
                {
                    QuicStreamCompleteSendRequest(Stream, SendRequest, true, false);
                    continue;
                }

                QuicStreamEnqueueSendRequest(Stream, SendRequest);

                if (BoolOk(SendRequest.Flags & QUIC_SEND_FLAG_START) && !Stream.Flags.Started)
                {
                    Start = true;
                }

                if (BoolOk(SendRequest.Flags & QUIC_SEND_FLAG_FIN))
                {
                    QuicStreamSendShutdown(Stream, true, false, BoolOk(SendRequest.Flags & QUIC_SEND_FLAG_DELAY_SEND), 0);
                }

                QuicSendSetStreamSendFlag(Stream.Connection.Send, Stream, QUIC_STREAM_SEND_FLAG_DATA, BoolOk(SendRequest.Flags & QUIC_SEND_FLAG_DELAY_SEND));

                if (Stream.Connection.Settings.SendBufferingEnabled)
                {
                    QuicSendBufferFill(Stream.Connection);
                }

                NetLog.Assert(Stream.SendRequests != null);
                QuicStreamSendDumpState(Stream);
            }

            if (Start)
            {
                QuicStreamStart(Stream, QUIC_STREAM_START_FLAG_IMMEDIATE | QUIC_STREAM_START_FLAG_SHUTDOWN_ON_FAIL, false);
            }

            QuicPerfCounterAdd(QUIC_PERFORMANCE_COUNTERS.QUIC_PERF_COUNTER_APP_SEND_BYTES, TotalBytesSent);
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

            ulong Start = (ulong)FrameMetadata.StreamOffset;
            ulong End = (ulong)(Start + (ulong)FrameMetadata.StreamLength);

            if (BoolOk(FrameMetadata.Flags &  QUIC_SENT_FRAME_FLAG_STREAM_OPEN) && !Stream.Flags.SendOpenAcked)
            {
                AddSendFlags |= QUIC_STREAM_SEND_FLAG_OPEN;
            }

            if (BoolOk(FrameMetadata.Flags & QUIC_SENT_FRAME_FLAG_STREAM_FIN) && !Stream.Flags.FinAcked)
            {
                AddSendFlags |= QUIC_STREAM_SEND_FLAG_FIN;
            }

            if (End <= (ulong)Stream.UnAckedOffset)
            {
                goto Done;
            }
            else if (Start < (ulong)Stream.UnAckedOffset)
            {
                Start = (ulong)Stream.UnAckedOffset;
            }

            QUIC_SUBRANGE Sack;
            int i = 0;
            while ((Sack = QuicRangeGetSafe(Stream.SparseAckRanges, i++)) != null && Sack.Low < (ulong)End)
            {
                if (Start < Sack.Low + Sack.Count)
                {
                    if (Start >= Sack.Low)
                    {
                        if (End <= Sack.Low + Sack.Count)
                        {
                            goto Done;
                        }
                        else
                        {
                            Start = Sack.Low + Sack.Count;
                        }

                    }
                    else if (End <= Sack.Low + Sack.Count)
                    {
                        End = Sack.Low;
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
                    Event.Type =  QUIC_STREAM_EVENT_TYPE.QUIC_STREAM_EVENT_CANCEL_ON_LOSS;
                    Event.CANCEL_ON_LOSS.ErrorCode = 0;
                    QuicStreamIndicateEvent(Stream, Event);
                    
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
            QUIC_STREAM_TYPE_INFO Info = Stream.Connection.Streams.Types[StreamType];
            return Info.MaxTotalStreamCount >= StreamCount;
        }

        static bool QuicStreamSendCanWriteDataFrames(QUIC_STREAM Stream)
        {
            NetLog.Assert(QuicStreamAllowedByPeer(Stream));
            NetLog.Assert(HasStreamDataFrames(Stream.SendFlags));

            if (BoolOk(Stream.SendFlags & QUIC_STREAM_SEND_FLAG_OPEN)
            {
                return true;
            }

            if (RECOV_WINDOW_OPEN(Stream))
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
            if (RECOV_WINDOW_OPEN(Stream))
            {
                QUIC_SUBRANGE Sack;
                int i = 0;
                while ((Sack = QuicRangeGetSafe(Stream.SparseAckRanges, i++)) != null && Sack.Low < Stream.RecoveryNextOffset)
                {
                    NetLog.Assert(Sack.Low + Sack.Count <= Stream.RecoveryNextOffset);
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

    }
}
