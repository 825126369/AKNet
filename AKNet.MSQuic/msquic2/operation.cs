/************************************Copyright*****************************************
*        ProjectName:AKNet
*        Web:https://github.com/825126369/AKNet
*        Description:C#游戏网络库
*        Author:许珂
*        StartTime:2024/11/01 00:00:00
*        ModifyTime:2025/11/14 8:56:48
*        Copyright:MIT软件许可证
************************************Copyright*****************************************/
using AKNet.Common;
using System;
using System.Collections.Concurrent;
using System.IO;
using System.Net.Sockets;
using System.Threading;

namespace MSQuic2
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
        //用于控制数据包的发送速率，避免突发发送导致网络拥塞。Pacing 定时器确保数据包以更平滑、更均匀的间隔发送，有助于提高网络效率和公平性。
        QUIC_CONN_TIMER_PACING,
        //控制接收方发送 ACK（确认）的延迟时间。QUIC 允许接收方延迟发送 ACK 以减少小数据包的数量，提高网络效率。该定时器用于在延迟超时后强制发送 ACK。
        QUIC_CONN_TIMER_ACK_DELAY,
        //用于检测数据包是否丢失。QUIC 使用基于时间的机制来判断哪些数据包可能已经丢失，从而触发重传。这个定时器是 QUIC 拥塞控制和可靠性机制的核心部分。
        QUIC_CONN_TIMER_LOSS_DETECTION,
        //在长时间空闲但连接仍需保持时，定期发送保持连接的探测包（keep-alive packets），以防止中间 NAT 或防火墙关闭连接。
        QUIC_CONN_TIMER_KEEP_ALIVE,
        //用于管理连接的空闲超时。如果在指定时间内没有任何活动（包括数据和 keep-alive），连接将被关闭。该定时器用于检测和处理长时间无活动的连接。
        QUIC_CONN_TIMER_IDLE,
        //在连接关闭过程中使用，确保关闭过程（如发送关闭帧、等待对端确认等）在合理时间内完成，避免资源长时间占用。
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
        public CXPLAT_POOL<QUIC_STATELESS_CONTEXT> mPool = null;
        public readonly CXPLAT_POOL_ENTRY<QUIC_STATELESS_CONTEXT> POOL_ENTRY = null;
        public readonly CXPLAT_LIST_ENTRY ListEntry;

        public QUIC_BINDING Binding;
        public QUIC_WORKER Worker;
        public QUIC_ADDR RemoteAddress;
        public QUIC_RX_PACKET Packet;
        public long CreationTimeMs;
        public bool HasBindingRef;
        public bool IsProcessed;
        public bool IsExpired;

        public QUIC_STATELESS_CONTEXT()
        {
            POOL_ENTRY = new CXPLAT_POOL_ENTRY<QUIC_STATELESS_CONTEXT>(this);
            ListEntry = new CXPLAT_LIST_ENTRY<QUIC_STATELESS_CONTEXT>(this);
        }

        public CXPLAT_POOL_ENTRY<QUIC_STATELESS_CONTEXT> GetEntry()
        {
            return POOL_ENTRY;
        }

        public CXPLAT_POOL<QUIC_STATELESS_CONTEXT> GetPool()
        {
            return this.mPool;
        }

        public void Reset()
        {
            throw new NotImplementedException();
        }

        public void SetPool(CXPLAT_POOL<QUIC_STATELESS_CONTEXT> mPool)
        {
            this.mPool = mPool;
        }
    }

    internal unsafe class QUIC_OPERATION:CXPLAT_POOL_Interface<QUIC_OPERATION>
    {
        public CXPLAT_POOL<QUIC_OPERATION> mPool = null;
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
        
        public QUIC_OPERATION()
        {
            POOL_ENTRY = new CXPLAT_POOL_ENTRY<QUIC_OPERATION>(this);
            Link = new CXPLAT_LIST_ENTRY<QUIC_OPERATION>(this);
        }
        public CXPLAT_POOL_ENTRY<QUIC_OPERATION> GetEntry()
        {
            return POOL_ENTRY;
        }

        public CXPLAT_POOL<QUIC_OPERATION> GetPool()
        {
            return mPool;
        }

        public void Reset()
        {
            FreeAfterProcess = false;

            INITIALIZE.Reset();
            API_CALL.Reset();
            FLUSH_RECEIVE.Reset();
            UNREACHABLE.Reset();
            FLUSH_STREAM_RECEIVE.Reset();
            FLUSH_SEND.Reset();
            TIMER_EXPIRED.Reset();
            STATELESS.Reset();
            ROUTE.Reset();
        }

        public void SetPool(CXPLAT_POOL<QUIC_OPERATION> mPool)
        {
            this.mPool = mPool;
        }

        public struct INITIALIZE_DATA
        {
            public void Reset()
            {

            }
        }
        public struct API_CALL_DATA
        {
            public QUIC_API_CONTEXT Context;

            public void Reset()
            {
                Context = null;
            }
        }
        public struct FLUSH_RECEIVE_DATA
        {
            public void Reset()
            {
                
            }
        }
        public struct UNREACHABLE_DATA
        {
            public QUIC_ADDR RemoteAddress;

            public void Reset()
            {
                RemoteAddress = null;
            }
        }
        public struct FLUSH_STREAM_RECEIVE_DATA
        {
            public QUIC_STREAM Stream;
            public void Reset()
            {
                Stream = null;
            }
        }

        public struct FLUSH_SEND_DATA
        {
            public void Reset()
            {
                
            }
        }
        public struct TIMER_EXPIRED_DATA
        {
            public QUIC_CONN_TIMER_TYPE Type;
            public void Reset()
            {
                
            }
        }
        public struct STATELESS_DATA
        {
            public QUIC_STATELESS_CONTEXT Context;

            public void Reset()
            {
                Context = null;
            }
        }
        public struct ROUTE_DATA
        {
            private byte[] m_PhysicalAddress;
            public byte PathId;
            public bool Succeeded;

            public byte[] PhysicalAddress 
            {
                get
                {
                    if(m_PhysicalAddress == null)
                    {
                        m_PhysicalAddress = new byte[6];
                    }
                    return m_PhysicalAddress;
                }
            }

            public void Reset()
            {
                PathId = 0;
                Succeeded = false;
            }
        }
    };

    internal class QUIC_OPERATION_QUEUE
    {
        public bool ActivelyProcessing;
        public readonly object Lock = new object();
        public readonly CXPLAT_LIST_ENTRY List = new CXPLAT_LIST_ENTRY<QUIC_OPERATION>(null);
        public CXPLAT_LIST_ENTRY PriorityTail; // Tail of the priority queue.
    }

    internal class QUIC_API_CONTEXT:CXPLAT_POOL_Interface<QUIC_API_CONTEXT>
    {
        public CXPLAT_POOL<QUIC_API_CONTEXT> mPool;
        public readonly CXPLAT_POOL_ENTRY<QUIC_API_CONTEXT> POOL_ENTRY;
        public QUIC_API_TYPE Type;
        public int Status;
        public EventWaitHandle Completed;
        public CONN_OPEN_DATA CONN_OPEN;
        public CONN_CLOSED_DATA CONN_CLOSED;
        public CONN_SHUTDOWN_DATA CONN_SHUTDOWN;
        public CONN_START_DATA CONN_START;
        public CONN_SET_CONFIGURATION_DATA CONN_SET_CONFIGURATION;
        public CONN_SEND_RESUMPTION_TICKET_DATA CONN_SEND_RESUMPTION_TICKET;
        public CONN_COMPLETE_RESUMPTION_TICKET_VALIDATION_DATA CONN_COMPLETE_RESUMPTION_TICKET_VALIDATION;
        public CONN_COMPLETE_CERTIFICATE_VALIDATION_DATA CONN_COMPLETE_CERTIFICATE_VALIDATION;
        public STRM_OPEN_DATA STRM_OPEN;
        public STRM_CLOSE_DATA STRM_CLOSE;
        public STRM_START_DATA STRM_START;
        public STRM_SHUTDOWN_DATA STRM_SHUTDOWN;
        public STRM_SEND_DATA STRM_SEND;
        public STRM_RECV_COMPLETE_DATA STRM_RECV_COMPLETE;
        public STRM_RECV_SET_ENABLED_DATA STRM_RECV_SET_ENABLED;
        public STRM_PROVIDE_RECV_BUFFERS_DATA STRM_PROVIDE_RECV_BUFFERS;
        public SET_PARAM_DATA SET_PARAM;
        public GET_PARAM_DATA GET_PARAM;

        public QUIC_API_CONTEXT()
        {
            POOL_ENTRY = new CXPLAT_POOL_ENTRY<QUIC_API_CONTEXT>(this);
        }
        public CXPLAT_POOL_ENTRY<QUIC_API_CONTEXT> GetEntry()
        {
            return POOL_ENTRY;
        }

        public CXPLAT_POOL<QUIC_API_CONTEXT> GetPool()
        {
            return this.mPool;
        }

        public void Reset()
        {
            
        }

        public void SetPool(CXPLAT_POOL<QUIC_API_CONTEXT> mPool)
        {
            this.mPool = mPool;
        }

        public struct CONN_OPEN_DATA
        {
            
        }
        public struct CONN_CLOSED_DATA
        {
            
        }
        public struct CONN_SHUTDOWN_DATA
        {
            public QUIC_CONNECTION_SHUTDOWN_FLAGS Flags;
            public bool RegistrationShutdown;
            public bool TransportShutdown;
            public int ErrorCode;
        }

        public struct CONN_START_DATA
        {
            public QUIC_CONFIGURATION Configuration;
            public string ServerName;
            public ushort ServerPort;
            public AddressFamily Family;
        }

        public struct CONN_SET_CONFIGURATION_DATA
        {
            public QUIC_CONFIGURATION Configuration;
        }

        public struct CONN_SEND_RESUMPTION_TICKET_DATA
        {
            public uint Flags;
            public QUIC_BUFFER ResumptionAppData;
        }

        public struct CONN_COMPLETE_RESUMPTION_TICKET_VALIDATION_DATA
        {
            public bool Result;
        }

        public struct CONN_COMPLETE_CERTIFICATE_VALIDATION_DATA
        {
            public QUIC_TLS_ALERT_CODES TlsAlert;
            public bool Result;
        }

        public struct STRM_OPEN_DATA
        {
            public uint Flags;
            public QUIC_STREAM_CALLBACK Handler;
            public object Context;
            public QUIC_HANDLE NewStream;
        }

        public struct STRM_CLOSE_DATA
        {
            public QUIC_STREAM Stream;
        }

        public struct STRM_START_DATA
        {
            public QUIC_STREAM Stream;
            public QUIC_STREAM_START_FLAGS Flags;
        }

        public struct STRM_SHUTDOWN_DATA
        {
            public QUIC_STREAM Stream;
            public uint Flags;
            public int ErrorCode;
        }

        public struct STRM_SEND_DATA
        {
            public QUIC_STREAM Stream;
        }
        
        public struct STRM_RECV_COMPLETE_DATA
        {
            public QUIC_STREAM Stream;
        }

        public struct STRM_RECV_SET_ENABLED_DATA
        {
            public QUIC_STREAM Stream;
            public bool IsEnabled;
        }

        public struct STRM_PROVIDE_RECV_BUFFERS_DATA
        {
            public QUIC_STREAM Stream;
            public CXPLAT_LIST_ENTRY Chunks;
        }

        public struct SET_PARAM_DATA
        {
            public QUIC_HANDLE Handle;
            public uint Param;
            public QUIC_BUFFER Buffer;
        }

        public struct GET_PARAM_DATA
        {
            public QUIC_HANDLE Handle;
            public uint Param;
            public QUIC_BUFFER Buffer;
        }
    }

    internal static partial class MSQuicFunc
    {
        static void QuicOperationQueueInitialize(QUIC_OPERATION_QUEUE OperQ)
        {
            OperQ.ActivelyProcessing = false;
            CxPlatListInitializeHead(OperQ.List);
            OperQ.PriorityTail = OperQ.List;
        }

        static void QuicOperationQueueUninitialize(QUIC_OPERATION_QUEUE OperQ)
        {
            NetLog.Assert(CxPlatListIsEmpty(OperQ.List));
            NetLog.Assert(OperQ.PriorityTail == OperQ.List);
        }

        static bool QuicOperationEnqueue(QUIC_OPERATION_QUEUE OperQ, QUIC_PARTITION Partition, QUIC_OPERATION Oper)
        {
            bool StartProcessing;
            CxPlatDispatchLockAcquire(OperQ.Lock);
            StartProcessing = CxPlatListIsEmpty(OperQ.List) && !OperQ.ActivelyProcessing;
            CxPlatListInsertTail(OperQ.List, Oper.Link);
            CxPlatDispatchLockRelease(OperQ.Lock);

            QuicPerfCounterAdd(Partition, QUIC_PERFORMANCE_COUNTERS.QUIC_PERF_COUNTER_CONN_OPER_QUEUED, 1);
            QuicPerfCounterAdd(Partition, QUIC_PERFORMANCE_COUNTERS.QUIC_PERF_COUNTER_CONN_OPER_QUEUE_DEPTH, 1);
            return StartProcessing;
        }

        static bool QuicOperationEnqueuePriority(QUIC_OPERATION_QUEUE OperQ, QUIC_PARTITION Partition, QUIC_OPERATION Oper)
        {
            bool StartProcessing;

            CxPlatDispatchLockAcquire(OperQ.Lock);
#if DEBUG
            NetLog.Assert(Oper.Link.Next == null);
#endif
            StartProcessing = CxPlatListIsEmpty(OperQ.List) && !OperQ.ActivelyProcessing;
            CxPlatListInsertMiddle(OperQ.List, OperQ.PriorityTail, Oper.Link);
            OperQ.PriorityTail = Oper.Link;
            CxPlatDispatchLockRelease(OperQ.Lock);

            QuicPerfCounterAdd(Partition, QUIC_PERFORMANCE_COUNTERS.QUIC_PERF_COUNTER_CONN_OPER_QUEUED, 1);
            QuicPerfCounterAdd(Partition, QUIC_PERFORMANCE_COUNTERS.QUIC_PERF_COUNTER_CONN_OPER_QUEUE_DEPTH, 1);
            return StartProcessing;
        }

        static bool QuicOperationEnqueueFront(QUIC_OPERATION_QUEUE OperQ, QUIC_PARTITION Partition, QUIC_OPERATION Oper)
        {
            bool StartProcessing;

            CxPlatDispatchLockAcquire(OperQ.Lock);
            StartProcessing = CxPlatListIsEmpty(OperQ.List) && !OperQ.ActivelyProcessing;
            CxPlatListInsertHead(OperQ.List, Oper.Link);
            if (OperQ.PriorityTail == OperQ.List)
            {
                OperQ.PriorityTail = Oper.Link;
            }
            CxPlatDispatchLockRelease(OperQ.Lock);

            QuicPerfCounterAdd(Partition, QUIC_PERFORMANCE_COUNTERS.QUIC_PERF_COUNTER_CONN_OPER_QUEUED, 1);
            QuicPerfCounterAdd(Partition, QUIC_PERFORMANCE_COUNTERS.QUIC_PERF_COUNTER_CONN_OPER_QUEUE_DEPTH, 1);
            return StartProcessing;
        }

        static QUIC_OPERATION QuicOperationDequeue(QUIC_OPERATION_QUEUE OperQ, QUIC_PARTITION Partition)
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
                if (OperQ.PriorityTail == Oper.Link)
                {
                    OperQ.PriorityTail = OperQ.List;
                }
            }
            CxPlatDispatchLockRelease(OperQ.Lock);

            if (Oper != null)
            {
                QuicPerfCounterDecrement(Partition, QUIC_PERFORMANCE_COUNTERS.QUIC_PERF_COUNTER_CONN_OPER_QUEUE_DEPTH);
            }
            return Oper;
        }

        static void QuicOperationQueueClear(QUIC_OPERATION_QUEUE OperQ, QUIC_PARTITION Partition)
        {
            CXPLAT_LIST_ENTRY OldList = new CXPLAT_LIST_ENTRY<QUIC_OPERATION>(null);
            CxPlatListInitializeHead(OldList);

            CxPlatDispatchLockAcquire(OperQ.Lock);
            OperQ.ActivelyProcessing = false;
            CxPlatListMoveItems(OperQ.List, OldList);
            OperQ.PriorityTail = OperQ.List;
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
                            if (ApiCtx.STRM_START.Flags.HasFlag(QUIC_STREAM_START_FLAGS.QUIC_STREAM_START_FLAG_SHUTDOWN_ON_FAIL))
                            {
                                QuicStreamShutdown(ApiCtx.STRM_START.Stream, QUIC_STREAM_SHUTDOWN_FLAG_ABORT | QUIC_STREAM_SHUTDOWN_FLAG_IMMEDIATE, 0);
                            }
                        }
                        else if (ApiCtx.Type == QUIC_API_TYPE.QUIC_API_TYPE_STRM_SEND && !ApiCtx.STRM_START.Stream.Flags.Started)
                        {
                            QuicStreamShutdown(ApiCtx.STRM_START.Stream, QUIC_STREAM_SHUTDOWN_FLAG_ABORT | QUIC_STREAM_SHUTDOWN_FLAG_IMMEDIATE, 0);
                        }
                    }
                    QuicOperationFree(Oper);
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
            QuicPerfCounterAdd(Partition, QUIC_PERFORMANCE_COUNTERS.QUIC_PERF_COUNTER_CONN_OPER_QUEUE_DEPTH, OperationsDequeued);
        }

        static bool QuicOperationHasPriority(QUIC_OPERATION_QUEUE OperQ)
        {
            CxPlatDispatchLockAcquire(OperQ.Lock);
            bool HasPriorityWork = OperQ.List != OperQ.PriorityTail;
            CxPlatDispatchLockRelease(OperQ.Lock);
            return HasPriorityWork;
        }

        static QUIC_OPERATION QuicConnAllocOperation(QUIC_CONNECTION Connection, QUIC_OPERATION_TYPE Type)
        {
            return QuicOperationAlloc(Connection.Partition, Type);
        }

        static QUIC_OPERATION QuicOperationAlloc(QUIC_PARTITION Partition, QUIC_OPERATION_TYPE Type)
        {
            QUIC_OPERATION Oper = Partition.OperPool.CxPlatPoolAlloc();
            if (Oper != null)
            {
#if DEBUG
                Oper.Link.Next = null;
#endif
                Oper.Type = Type;
                Oper.FreeAfterProcess = true;

                if (Oper.Type == QUIC_OPERATION_TYPE.QUIC_OPER_TYPE_API_CALL)
                {
                    Oper.API_CALL.Context = Partition.ApiContextPool.CxPlatPoolAlloc();
                    if (Oper.API_CALL.Context == null)
                    {
                        Partition.OperPool.CxPlatPoolFree(Oper);
                        Oper = null;
                    }
                    else
                    {
                        Oper.API_CALL.Context.Status = 0;
                        Oper.API_CALL.Context.Completed = null;
                    }
                }
            }
            return Oper;
        }

        static void QuicOperationFree(QUIC_OPERATION Oper)
        {
            NetLog.Assert(Oper.Link.Next == null);
            NetLog.Assert(Oper.FreeAfterProcess);
            if (Oper.Type == QUIC_OPERATION_TYPE.QUIC_OPER_TYPE_API_CALL)
            {
                QUIC_API_CONTEXT ApiCtx = Oper.API_CALL.Context;
                if (ApiCtx.Type == QUIC_API_TYPE.QUIC_API_TYPE_CONN_START)
                {
                    QuicConfigurationRelease(ApiCtx.CONN_START.Configuration);
                    ApiCtx.CONN_START.ServerName = null;
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
                ApiCtx.GetPool().CxPlatPoolFree(ApiCtx);
            }
            else if (Oper.Type == QUIC_OPERATION_TYPE.QUIC_OPER_TYPE_FLUSH_STREAM_RECV)
            {

            }
            else if (Oper.Type >= QUIC_OPERATION_TYPE.QUIC_OPER_TYPE_VERSION_NEGOTIATION)
            {

            }
            Oper.GetPool().CxPlatPoolFree(Oper);
        }

    }
    
}
