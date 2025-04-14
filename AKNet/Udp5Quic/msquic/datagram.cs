using AKNet.Common;
using System.Threading;

namespace AKNet.Udp5Quic.Common
{
    internal class QUIC_DATAGRAM
    {
        public QUIC_SEND_REQUEST SendQueue;
        public QUIC_SEND_REQUEST PrioritySendQueueTail;
        public QUIC_SEND_REQUEST SendQueueTail;
        public QUIC_SEND_REQUEST ApiQueue;
        public readonly object ApiQueueLock = new object();
        public int MaxSendLength;
        public bool SendEnabled;

        public QUIC_CONNECTION mConnection;
    }

    internal static partial class MSQuicFunc
    {
        static void QuicDatagramInitialize(QUIC_DATAGRAM Datagram)
        {
            Datagram.SendEnabled = true;
            Datagram.MaxSendLength = ushort.MaxValue;
            Datagram.PrioritySendQueueTail = Datagram.SendQueue;
            Datagram.SendQueueTail = Datagram.SendQueue;
            QuicDatagramValidate(Datagram);
        }

        static void QuicDatagramValidate(QUIC_DATAGRAM Datagram)
        {
            QUIC_CONNECTION Connection = QuicDatagramGetConnection(Datagram);
            if (QuicConnIsClosed(Connection))
            {
                NetLog.Assert(Datagram.SendQueue == null);
                NetLog.Assert((Connection.Send.SendFlags & QUIC_CONN_SEND_FLAG_DATAGRAM) == 0);
            }
            else if ((Connection.Send.SendFlags & QUIC_CONN_SEND_FLAG_DATAGRAM) != 0)
            {
                NetLog.Assert(Datagram.SendQueue != null);
            }
            else if (Connection.State.PeerTransportParameterValid)
            {
                NetLog.Assert(Datagram.SendQueue == null);
            }

            if (!Datagram.SendEnabled)
            {
                NetLog.Assert(Datagram.MaxSendLength == 0);
            }
            else
            {
                QUIC_SEND_REQUEST SendRequest = Datagram.SendQueue;
                while (SendRequest != null)
                {
                    NetLog.Assert(SendRequest.TotalLength <= Datagram.MaxSendLength);
                    SendRequest = SendRequest.Next;
                }
            }
        }

        static ulong QuicDatagramQueueSend(QUIC_DATAGRAM Datagram, QUIC_SEND_REQUEST SendRequest)
        {
            ulong Status;
            bool QueueOper = true;
            bool IsPriority = BoolOk(SendRequest.Flags & QUIC_SEND_FLAG_PRIORITY_WORK);
            QUIC_CONNECTION Connection = QuicDatagramGetConnection(Datagram);

            Monitor.Enter(Datagram.ApiQueueLock);
            if (!Datagram.SendEnabled)
            {
                Status = QUIC_STATUS_INVALID_STATE;
            }
            else
            {
                if (SendRequest.TotalLength > Datagram.MaxSendLength)
                {
                    Status = QUIC_STATUS_INVALID_PARAMETER;
                }
                else
                {
                    QUIC_SEND_REQUEST ApiQueueTail = Datagram.ApiQueue;
                    while (ApiQueueTail != null)
                    {
                        ApiQueueTail = ApiQueueTail.Next;
                        QueueOper = false;
                    }

                    ApiQueueTail = SendRequest;
                    Status = QUIC_STATUS_SUCCESS;
                }
            }
            Monitor.Exit(Datagram.ApiQueueLock);

            if (QUIC_FAILED(Status))
            {
                Connection.Worker.SendRequestPool.CxPlatPoolFree(SendRequest);
                goto Exit;
            }

            Status = QUIC_STATUS_PENDING;
            if (QueueOper)
            {
                QUIC_OPERATION Oper = QuicOperationAlloc(Connection.Worker, QUIC_OPERATION_TYPE.QUIC_OPER_TYPE_API_CALL);
                if (Oper == null)
                {
                    goto Exit;
                }

                Oper.API_CALL.Context.Type = QUIC_API_TYPE.QUIC_API_TYPE_DATAGRAM_SEND;
                if (IsPriority)
                {
                    QuicConnQueuePriorityOper(Connection, Oper);
                }
                else
                {
                    QuicConnQueueOper(Connection, Oper);
                }
            }
        Exit:
            return Status;
        }

        static void QuicDatagramSendShutdown(QUIC_DATAGRAM Datagram)
        {
            if (!Datagram.SendEnabled)
            {
                return;
            }

            QUIC_CONNECTION Connection = QuicDatagramGetConnection(Datagram);
            CxPlatDispatchLockAcquire(Datagram.ApiQueueLock);
            Datagram.SendEnabled = false;
            Datagram.MaxSendLength = 0;
            QUIC_SEND_REQUEST ApiQueue = Datagram.ApiQueue;
            Datagram.ApiQueue = null;
            CxPlatDispatchLockRelease(Datagram.ApiQueueLock);

            QuicSendClearSendFlag(Connection.Send, QUIC_CONN_SEND_FLAG_DATAGRAM);

            while (Datagram.SendQueue != null)
            {
                QUIC_SEND_REQUEST SendRequest = Datagram.SendQueue;
                Datagram.SendQueue = SendRequest.Next;
                QuicDatagramCancelSend(Connection, SendRequest);
            }
            Datagram.PrioritySendQueueTail = Datagram.SendQueue;
            Datagram.SendQueueTail = Datagram.SendQueue;

            while (ApiQueue != null)
            {
                QUIC_SEND_REQUEST SendRequest = ApiQueue;
                ApiQueue = ApiQueue.Next;
                QuicDatagramCancelSend(Connection, SendRequest);
            }
            QuicDatagramValidate(Datagram);
        }

        static void QuicDatagramCancelSend(QUIC_CONNECTION Connection,QUIC_SEND_REQUEST SendRequest)
        {
            QuicDatagramIndicateSendStateChange(Connection, ref SendRequest.ClientContext, QUIC_DATAGRAM_SEND_STATE.QUIC_DATAGRAM_SEND_CANCELED);
            Connection.Worker.SendRequestPool.CxPlatPoolFree(SendRequest);
        }

        static void QuicDatagramIndicateSendStateChange(QUIC_CONNECTION Connection, ref object ClientContext, QUIC_DATAGRAM_SEND_STATE State)
        {
            QUIC_CONNECTION_EVENT Event = new QUIC_CONNECTION_EVENT();
            Event.Type =  QUIC_CONNECTION_EVENT_TYPE.QUIC_CONNECTION_EVENT_DATAGRAM_SEND_STATE_CHANGED;
            Event.DATAGRAM_SEND_STATE_CHANGED.ClientContext = ClientContext;
            Event.DATAGRAM_SEND_STATE_CHANGED.State = State;

            QuicConnIndicateEvent(Connection, Event);
            ClientContext = Event.DATAGRAM_SEND_STATE_CHANGED.ClientContext;
        }

        static bool QuicDatagramWriteFrame(QUIC_DATAGRAM Datagram, QUIC_PACKET_BUILDER Builder)
        {
            QUIC_CONNECTION Connection = QuicDatagramGetConnection(Datagram);
            NetLog.Assert(Datagram.SendEnabled);
            bool Result = false;
            QuicDatagramValidate(Datagram);

            while (Datagram.SendQueue != null)
            {
                QUIC_SEND_REQUEST SendRequest = Datagram.SendQueue;

                if (Builder.Metadata.Flags.KeyType == QUIC_PACKET_KEY_TYPE.QUIC_PACKET_KEY_0_RTT && !BoolOk(SendRequest.Flags & QUIC_SEND_FLAG_ALLOW_0_RTT))
                {
                    NetLog.Assert(false);
                    Result = false;
                    goto Exit;
                }

                NetLog.Assert(SendRequest.TotalLength <= Datagram.MaxSendLength);

                int AvailableBufferLength = Builder.Datagram.Length - Builder.EncryptionOverhead;
                bool HadRoomForDatagram = QuicDatagramFrameEncodeEx(
                        SendRequest.Buffers,
                        SendRequest.BufferCount,
                        SendRequest.TotalLength,
                        Builder.DatagramLength,
                        AvailableBufferLength,
                        Builder.Datagram.Buffer);

                if (!HadRoomForDatagram)
                {
                    NetLog.Assert(Builder.Datagram.Length < Datagram.MaxSendLength || Builder.Metadata.FrameCount != 0 || Builder.PacketStart != 0);
                    Result = true;
                    goto Exit;
                }

                if (Datagram.PrioritySendQueueTail == SendRequest.Next)
                {
                    Datagram.PrioritySendQueueTail = Datagram.SendQueue;
                }
                if (Datagram.SendQueueTail == SendRequest.Next)
                {
                    Datagram.SendQueueTail = Datagram.SendQueue;
                }
                Datagram.SendQueue = SendRequest.Next;

                Builder.Metadata.Flags.IsAckEliciting = true;
                Builder.Metadata.Frames[Builder.Metadata.FrameCount].Type = QUIC_FRAME_TYPE.QUIC_FRAME_DATAGRAM;
                Builder.Metadata.Frames[Builder.Metadata.FrameCount].DATAGRAM.ClientContext = SendRequest.ClientContext;
                QuicDatagramCompleteSend(Connection, SendRequest, ref Builder.Metadata.Frames[Builder.Metadata.FrameCount].DATAGRAM.ClientContext);
                if (++Builder.Metadata.FrameCount == QUIC_MAX_FRAMES_PER_PACKET)
                {
                    Result = true;
                    goto Exit;
                }
            }

        Exit:
            if (Datagram.SendQueue == null)
            {
                Connection.Send.SendFlags &= ~QUIC_CONN_SEND_FLAG_DATAGRAM;
            }

            QuicDatagramValidate(Datagram);
            return Result;
        }

        static void QuicDatagramCompleteSend(QUIC_CONNECTION Connection,QUIC_SEND_REQUEST SendRequest, ref object ClientContext)
        {
            ClientContext = SendRequest.ClientContext;
            QuicDatagramIndicateSendStateChange(Connection, ref ClientContext, QUIC_DATAGRAM_SEND_STATE.QUIC_DATAGRAM_SEND_SENT);
            Connection.Worker.SendRequestPool.CxPlatPoolFree(SendRequest);
        }

    }
}
