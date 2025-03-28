using AKNet.Common;
using System;
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

    internal class QUIC_OPERATION
    {
        public CXPLAT_LIST_ENTRY Link;
        public QUIC_OPERATION_TYPE Type;
        public bool FreeAfterProcess;

        public class INITIALIZE_Class
        {
            //void* Reserved; // Nothing.
        }
        public INITIALIZE_Class INITIALIZE;

        public class API_CALL_Class
        {
            public QUIC_API_CONTEXT Context;
        }
        public API_CALL_Class API_CALL;

        //struct 
        //{
        //    void* Reserved; // Nothing.
        //}
        //FLUSH_RECEIVE;
        //struct {
        //    QUIC_ADDR RemoteAddress;
        //}
        //UNREACHABLE;
        //struct {
        //    QUIC_STREAM* Stream;
        //}
        //FLUSH_STREAM_RECEIVE;
        //struct {
        //    void* Reserved; // Nothing.
        //}
        //FLUSH_SEND;
        //struct {
        //    QUIC_CONN_TIMER_TYPE Type;
        //}
        //TIMER_EXPIRED;
        //struct {
        //    QUIC_STATELESS_CONTEXT* Context;
        //}
        //STATELESS; // Stateless reset, retry and VN
        //struct {
        //    uint8_t PhysicalAddress[6];
        //    uint8_t PathId;
        //    BOOLEAN Succeeded;
        //}
        //ROUTE;
    };

    internal class QUIC_OPERATION_QUEUE
    {
        public bool ActivelyProcessing;
        public readonly object Lock = new object();
        public CXPLAT_LIST_ENTRY List;
        public CXPLAT_LIST_ENTRY PriorityTail; // Tail of the priority queue.
    }

    internal class QUIC_API_CONTEXT
    {
        public QUIC_API_TYPE Type;
        public long Status;
        public CXPLAT_EVENT Completed;

        public class CONN_OPEN_Class
        {
            // void* Reserved; // Nothing.
        }
        public CONN_OPEN_Class CONN_OPEN;

        public class CONN_CLOSED_Class
        {
            //void* Reserved;
        }
        public CONN_CLOSED_Class CONN_CLOSED;

        public class CONN_SHUTDOWN_Class
        {
            public QUIC_CONNECTION_SHUTDOWN_FLAGS Flags;
            public bool RegistrationShutdown;
            public bool TransportShutdown;
            public ulong ErrorCode;
        }

        public CONN_SHUTDOWN_Class CONN_SHUTDOWN;

        public class CONN_START_Class
        {
            public QUIC_CONFIGURATION Configuration;
            public string ServerName;
            public ushort ServerPort;
            public ushort Family;
        }
        public CONN_START_Class CONN_START;


        public class CONN_SET_CONFIGURATION_Class
        {
            public QUIC_CONFIGURATION Configuration;
        }
        public CONN_SET_CONFIGURATION_Class CONN_SET_CONFIGURATION;

        public class CONN_SEND_RESUMPTION_TICKET_Class
        {
            public QUIC_SEND_RESUMPTION_FLAGS Flags;
            public byte[] ResumptionAppData;
            public ushort AppDataLength;
        }
        public CONN_SEND_RESUMPTION_TICKET_Class CONN_SEND_RESUMPTION_TICKET;

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
            public QUIC_STREAM_OPEN_FLAGS Flags;
            public QUIC_STREAM_CALLBACK Handler;
            void* Context;
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
            public QUIC_STREAM_START_FLAGS Flags;
        }
        public STRM_START_Class STRM_START;

        public class STRM_SHUTDOWN_Class
        {
            public QUIC_STREAM Stream;
            public QUIC_STREAM_SHUTDOWN_FLAGS Flags;
            public QUIC_VAR_INT ErrorCode;
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

    }

    internal static partial class MSQuicFunc
    {

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
            QUIC_OPERATION Oper = (QUIC_OPERATION)CxPlatPoolAlloc(Worker.OperPool);
            if (Oper != null)
            {
#if DEBUG
                Oper.Link.Flink = null;
#endif
                Oper.Type = Type;
                Oper.FreeAfterProcess = true;

                if (Oper.Type == QUIC_OPERATION_TYPE.QUIC_OPER_TYPE_API_CALL)
                {
                    Oper.API_CALL.Context = (QUIC_API_CONTEXT)CxPlatPoolAlloc(Worker.ApiContextPool);
                    if (Oper.API_CALL.Context == null)
                    {
                        CxPlatPoolFree(Worker.OperPool, Oper);
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
#if DEBUG
            NetLog.Assert(Oper.Link.Flink == null);
#endif
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
#if DEBUG
            NetLog.Assert(Oper.Link.Flink == null);
#endif
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

    }
    
}
