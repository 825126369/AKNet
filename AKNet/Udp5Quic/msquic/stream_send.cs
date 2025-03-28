using System.Threading;

namespace AKNet.Udp5Quic.Common
{
    internal static partial class MSQuicFunc
    {
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
                Event.Type =  QUIC_STREAM_EVENT_SEND_COMPLETE;
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
            else if (SendRequest->InternalBuffer.Length != 0)
            {
                QuicSendBufferFree(
                    &Connection->SendBuffer,
                    SendRequest->InternalBuffer.Buffer,
                    SendRequest->InternalBuffer.Length);
            }

            if (PreviouslyPosted)
            {
                CXPLAT_DBG_ASSERT(Connection->SendBuffer.PostedBytes >= SendRequest->TotalLength);
                Connection->SendBuffer.PostedBytes -= SendRequest->TotalLength;

                if (Connection->Settings.SendBufferingEnabled)
                {
                    QuicSendBufferFill(Connection);
                }
            }

            CxPlatPoolFree(&Connection->Worker->SendRequestPool, SendRequest);
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

                NetLog.Assert(!(SendRequest.Flags & QUIC_SEND_FLAG_BUFFERED));

                if (!Stream.Flags.CancelOnLoss && (SendRequest.Flags & QUIC_SEND_FLAGS.QUIC_SEND_FLAG_CANCEL_ON_LOSS) != 0)
                {
                    Stream.Flags.CancelOnLoss = true;
                }

                if (!Stream.Flags.SendEnabled)
                {
                    QuicStreamCompleteSendRequest(Stream, SendRequest, true, false);
                    continue;
                }

                QuicStreamEnqueueSendRequest(Stream, SendRequest);

                if (SendRequest.Flags & QUIC_SEND_FLAG_START && !Stream.Flags.Started)
                {
                    Start = TRUE;
                }

                if (SendRequest->Flags & QUIC_SEND_FLAG_FIN)
                {
                    //
                    // Gracefully shutdown the send direction if the flag is set.
                    //
                    QuicStreamSendShutdown(
                        Stream,
                        TRUE,
                        FALSE,
                        !!(SendRequest->Flags & QUIC_SEND_FLAG_DELAY_SEND),
                        0);
                }

                QuicSendSetStreamSendFlag(
                    &Stream->Connection->Send,
                    Stream,
                    QUIC_STREAM_SEND_FLAG_DATA,
                    !!(SendRequest->Flags & QUIC_SEND_FLAG_DELAY_SEND));

                if (Stream->Connection->Settings.SendBufferingEnabled)
                {
                    QuicSendBufferFill(Stream->Connection);
                }

                CXPLAT_DBG_ASSERT(Stream->SendRequests != NULL);

                QuicStreamSendDumpState(Stream);
            }

            if (Start)
            {
                (void)QuicStreamStart(
                    Stream,
                    QUIC_STREAM_START_FLAG_IMMEDIATE | QUIC_STREAM_START_FLAG_SHUTDOWN_ON_FAIL,
                    FALSE);
            }

            QuicPerfCounterAdd(QUIC_PERF_COUNTER_APP_SEND_BYTES, TotalBytesSent);
        }
    }
}
