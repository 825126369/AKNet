using AKNet.Common;
using System;
using System.Net;
using System.Threading;

namespace AKNet.Udp5Quic.Common
{
    internal enum QUIC_SCHEDULE_STATE
    {
        QUIC_SCHEDULE_IDLE,
        QUIC_SCHEDULE_QUEUED,
        QUIC_SCHEDULE_PROCESSING
    }

    internal enum QUIC_API_TYPE
    {
        QUIC_API_TYPE_CONN_CLOSE,
        QUIC_API_TYPE_CONN_SHUTDOWN,
        QUIC_API_TYPE_CONN_START,
        QUIC_API_TYPE_CONN_SET_CONFIGURATION,
        QUIC_API_TYPE_CONN_SEND_RESUMPTION_TICKET,

        QUIC_API_TYPE_STRM_CLOSE,
        QUIC_API_TYPE_STRM_SHUTDOWN,
        QUIC_API_TYPE_STRM_START,
        QUIC_API_TYPE_STRM_SEND,
        QUIC_API_TYPE_STRM_RECV_COMPLETE,
        QUIC_API_TYPE_STRM_RECV_SET_ENABLED,
        QUIC_API_TYPE_STRM_PROVIDE_RECV_BUFFERS,

        QUIC_API_TYPE_SET_PARAM,
        QUIC_API_TYPE_GET_PARAM,

        QUIC_API_TYPE_DATAGRAM_SEND,
        QUIC_API_TYPE_CONN_COMPLETE_RESUMPTION_TICKET_VALIDATION,
        QUIC_API_TYPE_CONN_COMPLETE_CERTIFICATE_VALIDATION,
    }

    internal enum QUIC_CONN_TIMER_TYPE
    {
        QUIC_CONN_TIMER_PACING,
        QUIC_CONN_TIMER_ACK_DELAY,
        QUIC_CONN_TIMER_LOSS_DETECTION,
        QUIC_CONN_TIMER_KEEP_ALIVE,
        QUIC_CONN_TIMER_IDLE,
        QUIC_CONN_TIMER_SHUTDOWN,
        QUIC_CONN_TIMER_COUNT
    }

    internal enum QUIC_OPERATION_TYPE
    {
        QUIC_OPER_TYPE_API_CALL,            // Process an API call from the app.
        QUIC_OPER_TYPE_FLUSH_RECV,          // Process queue of receive packets.
        QUIC_OPER_TYPE_UNREACHABLE,         // Process UDP unreachable event.
        QUIC_OPER_TYPE_FLUSH_STREAM_RECV,   // Indicate a stream data to the app.
        QUIC_OPER_TYPE_FLUSH_SEND,          // Frame packets and send them.
        QUIC_OPER_TYPE_DEPRECATED,          // No longer used.
        QUIC_OPER_TYPE_TIMER_EXPIRED,       // A timer expired.
        QUIC_OPER_TYPE_TRACE_RUNDOWN,       // A trace rundown was triggered.
        QUIC_OPER_TYPE_ROUTE_COMPLETION,    // Process route completion event.
        QUIC_OPER_TYPE_VERSION_NEGOTIATION, // A version negotiation needs to be sent.
        QUIC_OPER_TYPE_STATELESS_RESET,     // A stateless reset needs to be sent.
        QUIC_OPER_TYPE_RETRY,               // A retry needs to be sent.
    }

    internal class QUIC_STATELESS_CONTEXT:CXPLAT_POOL_Interface<QUIC_STATELESS_CONTEXT>
    {
        public readonly CXPLAT_POOL_ENTRY<QUIC_STATELESS_CONTEXT> POOL_ENTRY = null;
        
        public QUIC_BINDING Binding;
        public QUIC_WORKER Worker;
        public QUIC_ADDR RemoteAddress;
        public CXPLAT_LIST_ENTRY ListEntry;
        public QUIC_RX_PACKET Packet;
        public long CreationTimeMs;
        public bool HasBindingRef;
        public bool IsProcessed;
        public bool IsExpired;

        public QUIC_STATELESS_CONTEXT()
        {
            POOL_ENTRY = new CXPLAT_POOL_ENTRY<QUIC_STATELESS_CONTEXT>(this);
        }

        public CXPLAT_POOL_ENTRY<QUIC_STATELESS_CONTEXT> GetEntry()
        {
            return POOL_ENTRY;
        }

        public void Reset()
        {
            throw new NotImplementedException();
        }
    }

    internal class QUIC_OPERATION:CXPLAT_POOL_Interface<QUIC_OPERATION>
    {
        public readonly CXPLAT_POOL_ENTRY<QUIC_OPERATION> POOL_ENTRY = null;
        public readonly CXPLAT_LIST_ENTRY<QUIC_OPERATION> Link;
        public QUIC_OPERATION_TYPE Type;
        public bool FreeAfterProcess;

        public INITIALIZE_DATA INITIALIZE;
        public API_CALL_DATA API_CALL;
        public FLUSH_RECEIVE_DATA FLUSH_RECEIVE;
        public UNREACHABLE_DATA UNREACHABLE;
        public FLUSH_STREAM_RECEIVE_DATA FLUSH_STREAM_RECEIVE;
        public FLUSH_SEND_DATA FLUSH_SEND;
        public TIMER_EXPIRED_DATA TIMER_EXPIRED;
        public STATELESS_DATA STATELESS;
        public ROUTE_DATA ROUTE;

        public class INITIALIZE_DATA
        {
            
        }
        public class API_CALL_DATA
        {
            public QUIC_API_CONTEXT Context;
        }
        public class FLUSH_RECEIVE_DATA
        {
            
        }
        public class UNREACHABLE_DATA
        {
            public QUIC_ADDR RemoteAddress;
        }
        public class FLUSH_STREAM_RECEIVE_DATA
        {
            public QUIC_STREAM Stream;
        }
        public class FLUSH_SEND_DATA
        {
            
        }
        public class TIMER_EXPIRED_DATA
        {
            public QUIC_CONN_TIMER_TYPE Type;
        }
        public class STATELESS_DATA
        {
            public QUIC_STATELESS_CONTEXT Context;
        }
        public class ROUTE_DATA
        {
            public readonly byte[] PhysicalAddress = new byte[6] ;
            public byte PathId;
            public bool Succeeded;
        }
;

        public QUIC_OPERATION()
        {
            POOL_ENTRY = new CXPLAT_POOL_ENTRY<QUIC_OPERATION>(this);
            Link = new CXPLAT_LIST_ENTRY<QUIC_OPERATION>(this);
        }

        public CXPLAT_POOL_ENTRY<QUIC_OPERATION> GetEntry()
        {
            return POOL_ENTRY;
        }
        public void Reset()
        {
           
        }
    };

    internal class QUIC_OPERATION_QUEUE
    {
        public bool ActivelyProcessing;
        public readonly object Lock = new object();
        public CXPLAT_LIST_ENTRY List;
        public CXPLAT_LIST_ENTRY PriorityTail; // Tail of the priority queue.
    }

    internal class QUIC_API_CONTEXT:CXPLAT_POOL_Interface<QUIC_API_CONTEXT>
    {
        public readonly CXPLAT_POOL_ENTRY<QUIC_API_CONTEXT> POOL_ENTRY;
        public QUIC_API_TYPE Type;
        public ulong Status;
        public CXPLAT_EVENT Completed;
        public CONN_OPEN_DATA CONN_OPEN;
        public CONN_CLOSED_DATA CONN_CLOSED;
        public CONN_SHUTDOWN_DATA CONN_SHUTDOWN;
        public CONN_START_DATA CONN_START;
        public CONN_SET_CONFIGURATION_DATA CONN_SET_CONFIGURATION;
        public CONN_SEND_RESUMPTION_TICKET_DATA CONN_SEND_RESUMPTION_TICKET;



        public struct CONN_OPEN_DATA
        {
            
        }
        public class CONN_CLOSED_DATA
        {
            
        }
        public class CONN_SHUTDOWN_DATA
        {
            public QUIC_CONNECTION_SHUTDOWN_FLAGS Flags;
            public bool RegistrationShutdown;
            public bool TransportShutdown;
            public ulong ErrorCode;
        }

        public class CONN_START_DATA
        {
            public QUIC_CONFIGURATION Configuration;
            public string ServerName;
            public ushort ServerPort;
            public ushort Family;
        }

        public class CONN_SET_CONFIGURATION_DATA
        {
            public QUIC_CONFIGURATION Configuration;
        }

        public class CONN_SEND_RESUMPTION_TICKET_DATA
        {
            public uint Flags;
            public byte[] ResumptionAppData;
            public ushort AppDataLength;
        }

        public class CONN_COMPLETE_RESUMPTION_TICKET_VALIDATION_Class
        {
            public bool Result;
        }
        public CONN_COMPLETE_RESUMPTION_TICKET_VALIDATION_Class CONN_COMPLETE_RESUMPTION_TICKET_VALIDATION;

        public class CONN_COMPLETE_CERTIFICATE_VALIDATION_Class
        {
            public QUIC_TLS_ALERT_CODES TlsAlert;
            public bool Result;
        }
        public CONN_COMPLETE_CERTIFICATE_VALIDATION_Class CONN_COMPLETE_CERTIFICATE_VALIDATION;

        public class STRM_OPEN_Class
        {
            public uint Flags;
            public QUIC_STREAM_CALLBACK Handler;
            public object Context;
            public QUIC_HANDLE NewStream;
        }
        public STRM_OPEN_Class STRM_OPEN;

        public class STRM_CLOSE_Class
        {
            public QUIC_STREAM Stream;
        }
        public STRM_CLOSE_Class STRM_CLOSE;

        public class STRM_START_Class
        {
            public QUIC_STREAM Stream;
            public uint Flags;
        }
        public STRM_START_Class STRM_START;

        public class STRM_SHUTDOWN_Class
        {
            public QUIC_STREAM Stream;
            public uint Flags;
            public ulong ErrorCode;
        }
        public STRM_SHUTDOWN_Class STRM_SHUTDOWN;

        public class STRM_SEND_Class
        {
            public QUIC_STREAM Stream;
        }
        public STRM_SEND_Class STRM_SEND;


        public class STRM_RECV_COMPLETE_Class
        {
            public QUIC_STREAM Stream;
        }
        public STRM_RECV_COMPLETE_Class STRM_RECV_COMPLETE;

        public class STRM_RECV_SET_ENABLED_Class
        {
            public QUIC_STREAM Stream;
            public bool IsEnabled;
        }
        public STRM_RECV_SET_ENABLED_Class STRM_RECV_SET_ENABLED;

        public class STRM_PROVIDE_RECV_BUFFERS_Class
        {
            public QUIC_STREAM Stream;
            public CXPLAT_LIST_ENTRY Chunks;
        }
        public STRM_PROVIDE_RECV_BUFFERS_Class STRM_PROVIDE_RECV_BUFFERS;

        public class SET_PARAM_Class
        {
            public QUIC_HANDLE Handle;
            public uint Param;
            public uint BufferLength;
            public byte[] Buffer;
        }
        public SET_PARAM_Class SET_PARAM;

        public class GET_PARAM_Class
        {
            public QUIC_HANDLE Handle;
            public uint Param;
            public int BufferLength;
            public byte[] Buffer;
        }
        public GET_PARAM_Class GET_PARAM;

        
        public QUIC_API_CONTEXT()
        {
            POOL_ENTRY = new CXPLAT_POOL_ENTRY<QUIC_API_CONTEXT>(this);
        }
        public CXPLAT_POOL_ENTRY<QUIC_API_CONTEXT> GetEntry()
        {
            return POOL_ENTRY;
        }
        public void Reset()
        {
            throw new NotImplementedException();
        }
    }

    internal static partial class MSQuicFunc
    {
        static void QuicOperationQueueInitialize(QUIC_OPERATION_QUEUE OperQ)
        {
            OperQ.ActivelyProcessing = false;
            CxPlatListInitializeHead(OperQ.List);
            OperQ.PriorityTail = OperQ.List.Flink;
        }

        static bool QuicOperationEnqueuePriority(QUIC_OPERATION_QUEUE OperQ, QUIC_OPERATION Oper)
        {
            bool StartProcessing;
            Monitor.Enter(OperQ.Lock);
#if DEBUG
            NetLog.Assert(Oper.Link.Flink == null);
#endif
            StartProcessing = CxPlatListIsEmpty(OperQ.List) && !OperQ.ActivelyProcessing;
            CxPlatListInsertTail(OperQ.PriorityTail, Oper.Link);
            OperQ.PriorityTail = Oper.Link.Flink;
            Monitor.Exit(OperQ.Lock);

            QuicPerfCounterIncrement(QUIC_PERFORMANCE_COUNTERS.QUIC_PERF_COUNTER_CONN_OPER_QUEUED);
            QuicPerfCounterIncrement(QUIC_PERFORMANCE_COUNTERS.QUIC_PERF_COUNTER_CONN_OPER_QUEUE_DEPTH);
            return StartProcessing;
        }

        static QUIC_OPERATION QuicOperationAlloc(QUIC_WORKER Worker, QUIC_OPERATION_TYPE Type)
        {
            QUIC_OPERATION Oper = Worker.OperPool.CxPlatPoolAlloc();
            if (Oper != null)
            {
#if DEBUG
                Oper.Link.Flink = null;
#endif
                Oper.Type = Type;
                Oper.FreeAfterProcess = true;

                if (Oper.Type == QUIC_OPERATION_TYPE.QUIC_OPER_TYPE_API_CALL)
                {
                    Oper.API_CALL.Context = Worker.ApiContextPool.CxPlatPoolAlloc();
                    if (Oper.API_CALL.Context == null)
                    {
                        Worker.OperPool.CxPlatPoolFree(Oper);
                        Oper = null;
                    }
                    else
                    {
                        Oper.API_CALL.Context.Status = null;
                        Oper.API_CALL.Context.Completed = null;
                    }
                }
            }
            return Oper;
        }

        static bool QuicOperationEnqueue(QUIC_OPERATION_QUEUE OperQ, QUIC_OPERATION Oper)
        {
            bool StartProcessing;
            Monitor.Enter(OperQ.Lock);
            StartProcessing = CxPlatListIsEmpty(OperQ.List) && !OperQ.ActivelyProcessing;
            CxPlatListInsertTail(OperQ.List, Oper.Link);
            Monitor.Exit(OperQ.Lock);

            QuicPerfCounterAdd(QUIC_PERFORMANCE_COUNTERS.QUIC_PERF_COUNTER_CONN_OPER_QUEUED);
            QuicPerfCounterAdd(QUIC_PERFORMANCE_COUNTERS.QUIC_PERF_COUNTER_CONN_OPER_QUEUE_DEPTH);
            return StartProcessing;
        }
        
        static bool QuicOperationEnqueueFront(QUIC_OPERATION_QUEUE OperQ, QUIC_OPERATION Oper)
        {
            bool StartProcessing;
            Monitor.Enter(OperQ.Lock);
            StartProcessing = CxPlatListIsEmpty(OperQ.List) && !OperQ.ActivelyProcessing;
            CxPlatListInsertHead(OperQ.List, Oper.Link);
            if (OperQ.PriorityTail == OperQ.List.Flink)
            {
                OperQ.PriorityTail = Oper.Link.Flink;
            }
            Monitor.Exit(OperQ.Lock);

            QuicPerfCounterAdd(QUIC_PERFORMANCE_COUNTERS.QUIC_PERF_COUNTER_CONN_OPER_QUEUED);
            QuicPerfCounterAdd(QUIC_PERFORMANCE_COUNTERS.QUIC_PERF_COUNTER_CONN_OPER_QUEUE_DEPTH);
            return StartProcessing;
        }

        static void QuicOperationFree(QUIC_WORKER Worker, QUIC_OPERATION Oper)
        {
            NetLog.Assert(Oper.Link.Flink == null);
            NetLog.Assert(Oper.FreeAfterProcess);
            if (Oper.Type == QUIC_OPERATION_TYPE.QUIC_OPER_TYPE_API_CALL)
            {
                QUIC_API_CONTEXT ApiCtx = Oper.API_CALL.Context;
                if (ApiCtx.Type == QUIC_API_TYPE.QUIC_API_TYPE_CONN_START)
                {

                }
                else if (ApiCtx.Type == QUIC_API_TYPE.QUIC_API_TYPE_CONN_SET_CONFIGURATION)
                {

                }
                else if (ApiCtx.Type == QUIC_API_TYPE.QUIC_API_TYPE_CONN_SEND_RESUMPTION_TICKET)
                {
                    if (ApiCtx.CONN_SEND_RESUMPTION_TICKET.ResumptionAppData != null)
                    {

                    }
                }
                else if (ApiCtx.Type == QUIC_API_TYPE.QUIC_API_TYPE_STRM_START)
                {

                }
                else if (ApiCtx.Type == QUIC_API_TYPE.QUIC_API_TYPE_STRM_SHUTDOWN)
                {

                }
                else if (ApiCtx.Type == QUIC_API_TYPE.QUIC_API_TYPE_STRM_SEND)
                {

                }
                else if (ApiCtx.Type == QUIC_API_TYPE.QUIC_API_TYPE_STRM_RECV_COMPLETE)
                {

                }
                else if (ApiCtx.Type == QUIC_API_TYPE.QUIC_API_TYPE_STRM_RECV_SET_ENABLED)
                {

                }
                Worker.ApiContextPool.CxPlatPoolFree(ApiCtx);
            }
            else if (Oper.Type == QUIC_OPERATION_TYPE.QUIC_OPER_TYPE_FLUSH_STREAM_RECV)
            {

            }
            else if (Oper.Type >= QUIC_OPERATION_TYPE.QUIC_OPER_TYPE_VERSION_NEGOTIATION)
            {

            }
            Worker.OperPool.CxPlatPoolFree(Oper);
        }

        static QUIC_OPERATION QuicOperationDequeue(QUIC_OPERATION_QUEUE OperQ)
        {
            QUIC_OPERATION Oper;
            CxPlatDispatchLockAcquire(OperQ.Lock);
            if (CxPlatListIsEmpty(OperQ.List))
            {
                OperQ.ActivelyProcessing = false;
                Oper = null;
            }
            else
            {
                OperQ.ActivelyProcessing = true;
                Oper = CXPLAT_CONTAINING_RECORD<QUIC_OPERATION>(CxPlatListRemoveHead(OperQ.List));
                if (OperQ.PriorityTail == Oper.Link.Flink)
                {
                    OperQ.PriorityTail = OperQ.List.Flink;
                }
            }
            CxPlatDispatchLockRelease(OperQ.Lock);

            if (Oper != null)
            {
                QuicPerfCounterDecrement(QUIC_PERFORMANCE_COUNTERS.QUIC_PERF_COUNTER_CONN_OPER_QUEUE_DEPTH);
            }
            return Oper;
        }

        static void QuicOperationQueueClear(QUIC_WORKER Worker, QUIC_OPERATION_QUEUE OperQ)
        {
            CXPLAT_LIST_ENTRY OldList = new CXPLAT_LIST_ENTRY<QUIC_OPERATION>(null);
            CxPlatListInitializeHead(OldList);

            CxPlatDispatchLockAcquire(OperQ.Lock);
            OperQ.ActivelyProcessing = false;
            CxPlatListMoveItems(OperQ.List, OldList);
            OperQ.PriorityTail = OperQ.List.Flink;
            CxPlatDispatchLockRelease(OperQ.Lock);

            int OperationsDequeued = 0;
            while (!CxPlatListIsEmpty(OldList))
            {
                QUIC_OPERATION Oper = CXPLAT_CONTAINING_RECORD<QUIC_OPERATION>(CxPlatListRemoveHead(OldList));
                --OperationsDequeued;
                if (Oper.FreeAfterProcess)
                {
                    if (Oper.Type == QUIC_OPERATION_TYPE.QUIC_OPER_TYPE_API_CALL)
                    {
                        QUIC_API_CONTEXT ApiCtx = Oper.API_CALL.Context;
                        if (ApiCtx.Type == QUIC_API_TYPE.QUIC_API_TYPE_STRM_START)
                        {
                            NetLog.Assert(ApiCtx.Completed == null);
                            QuicStreamIndicateStartComplete(ApiCtx.STRM_START.Stream, QUIC_STATUS_ABORTED);
                            if (BoolOk(ApiCtx.STRM_START.Flags & QUIC_STREAM_START_FLAG_SHUTDOWN_ON_FAIL))
                            {
                                QuicStreamShutdown(ApiCtx.STRM_START.Stream, QUIC_STREAM_SHUTDOWN_FLAG_ABORT | QUIC_STREAM_SHUTDOWN_FLAG_IMMEDIATE, 0);
                            }
                        }
                        else if (ApiCtx.Type == QUIC_API_TYPE.QUIC_API_TYPE_STRM_SEND && !ApiCtx.STRM_START.Stream.Flags.Started)
                        {
                            QuicStreamShutdown(ApiCtx.STRM_START.Stream, QUIC_STREAM_SHUTDOWN_FLAG_ABORT | QUIC_STREAM_SHUTDOWN_FLAG_IMMEDIATE, 0);
                        }
                    }
                    QuicOperationFree(Worker, Oper);
                }
                else
                {
                    NetLog.Assert(Oper.Type == QUIC_OPERATION_TYPE.QUIC_OPER_TYPE_API_CALL);
                    if (Oper.Type == QUIC_OPERATION_TYPE.QUIC_OPER_TYPE_API_CALL)
                    {
                        QUIC_API_CONTEXT ApiCtx = Oper.API_CALL.Context;
                        if (ApiCtx.Status != null)
                        {
                            ApiCtx.Status = QUIC_STATUS_INVALID_STATE;
                            CxPlatEventSet(ApiCtx.Completed);
                        }
                        if (ApiCtx.Type == QUIC_API_TYPE.QUIC_API_TYPE_STRM_RECV_COMPLETE)
                        {
                            QuicStreamRelease(ApiCtx.STRM_RECV_COMPLETE.Stream, QUIC_STREAM_REF.QUIC_STREAM_REF_OPERATION);
                        }
                    }
                }
            }
            QuicPerfCounterAdd(QUIC_PERFORMANCE_COUNTERS.QUIC_PERF_COUNTER_CONN_OPER_QUEUE_DEPTH, OperationsDequeued);
        }

    }
    
}
