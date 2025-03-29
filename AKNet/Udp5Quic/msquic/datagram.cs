using System;
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
                if(SendRequest.TotalLength > Datagram.MaxSendLength)
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
                CxPlatPoolFree(Connection.Worker.SendRequestPool, SendRequest);
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
    }
}
