using AKNet.Common;
using System;
using System.Net.Sockets;
using System.Threading;

namespace AKNet.Udp5MSQuic.Common
{
    internal static partial class MSQuicFunc
    {
        static bool IS_REGISTRATION_HANDLE(QUIC_HANDLE Handle)
        {
            return (Handle) != null && (Handle).Type == QUIC_HANDLE_TYPE.QUIC_HANDLE_TYPE_REGISTRATION;
        }

        static bool IS_CONN_HANDLE(QUIC_HANDLE Handle)
        {
            return Handle != null &&
                (Handle.Type == QUIC_HANDLE_TYPE.QUIC_HANDLE_TYPE_CONNECTION_CLIENT ||
                Handle.Type == QUIC_HANDLE_TYPE.QUIC_HANDLE_TYPE_CONNECTION_SERVER);
        }

        static bool IS_STREAM_HANDLE(QUIC_HANDLE Handle)
        {
            return (Handle) != null && Handle.Type == QUIC_HANDLE_TYPE.QUIC_HANDLE_TYPE_STREAM;
        }

        public static ulong MsQuicConnectionOpen(QUIC_REGISTRATION Registration, QUIC_CONNECTION_CALLBACK Handler, object Context, out QUIC_CONNECTION NewConnection)
        {
            ulong Status = 0;
            QUIC_CONNECTION Connection = NewConnection = null;
            if (Handler == null)
            {
                Status = QUIC_STATUS_INVALID_PARAMETER;
                goto Error;
            }
            Status = QuicConnAlloc(Registration, null, null, out Connection);
            if (QUIC_FAILED(Status))
            {
                goto Error;
            }

            Connection.ClientCallbackHandler = Handler;
            Connection.ClientContext = Context;
            NewConnection = Connection;
            Status = QUIC_STATUS_SUCCESS;
        Error:
            return Status;
        }

        static void MsQuicConnectionClose(QUIC_HANDLE Handle)
        {
            QUIC_CONNECTION Connection;
            if (!IS_CONN_HANDLE(Handle))
            {
                goto Error;
            }

            Connection = (QUIC_CONNECTION)Handle;
            NetLog.Assert(!Connection.State.Freed);
            NetLog.Assert(!Connection.State.HandleClosed);

            bool IsWorkerThread = Connection.WorkerThreadID == Thread.CurrentThread.ManagedThreadId;
            if (IsWorkerThread && Connection.State.HandleClosed)
            {
                goto Error;
            }

            NetLog.Assert(!Connection.State.HandleClosed);

            if (IsWorkerThread)
            {
                bool AlreadyInline = Connection.State.InlineApiExecution;
                if (!AlreadyInline)
                {
                    Connection.State.InlineApiExecution = true;
                }
                QuicConnCloseHandle(Connection);
                if (!AlreadyInline)
                {
                    Connection.State.InlineApiExecution = false;
                }
            }
            else
            {

                CXPLAT_EVENT CompletionEvent = new CXPLAT_EVENT();
                QUIC_OPERATION Oper = new QUIC_OPERATION();
                QUIC_API_CONTEXT ApiCtx = new QUIC_API_CONTEXT();

                Oper.Type = QUIC_OPERATION_TYPE.QUIC_OPER_TYPE_API_CALL;
                Oper.FreeAfterProcess = false;
                Oper.API_CALL.Context = ApiCtx;

                ApiCtx.Type = QUIC_API_TYPE.QUIC_API_TYPE_CONN_CLOSE;
                CxPlatEventInitialize(CompletionEvent, true, false);
                ApiCtx.Completed = CompletionEvent;
                ApiCtx.Status = 0;

                QuicConnQueueOper(Connection, Oper);
                QuicTraceEvent(QuicEventId.ApiWaitOperation, "[ api] Waiting on operation");
                CxPlatEventWaitForever(CompletionEvent);
                CxPlatEventUninitialize(CompletionEvent);
            }

            NetLog.Assert(Connection.State.HandleClosed);
            QuicConnRelease(Connection, QUIC_CONNECTION_REF.QUIC_CONN_REF_HANDLE_OWNER);

        Error:
            QuicTraceEvent(QuicEventId.ApiExit, "[ api] Exit");
        }

        public static void MsQuicConnectionShutdown(QUIC_HANDLE Handle, QUIC_CONNECTION_SHUTDOWN_FLAGS Flags, ulong ErrorCode)
        {
            QUIC_CONNECTION Connection;
            QUIC_OPERATION Oper;

            QuicTraceEvent(QuicEventId.ApiEnter, "[ api] Enter %u (%p).", QUIC_TRACE_API_TYPE.QUIC_TRACE_API_CONNECTION_SHUTDOWN, Handle);
            if (IS_CONN_HANDLE(Handle))
            {
                Connection = (QUIC_CONNECTION)Handle;
            }
            else if (IS_STREAM_HANDLE(Handle))
            {
                QUIC_STREAM Stream = (QUIC_STREAM)Handle;
                NetLog.Assert(!Stream.Flags.HandleClosed);
                NetLog.Assert(!Stream.Flags.Freed);
                Connection = Stream.Connection;
            }
            else
            {
                goto Error;
            }

            if (ErrorCode > QUIC_UINT62_MAX)
            {
                NetLog.Assert(ErrorCode <= QUIC_UINT62_MAX);
                goto Error;
            }

            NetLog.Assert(!Connection.State.Freed);
            NetLog.Assert((Connection.WorkerThreadID == CxPlatCurThreadID()) || !Connection.State.HandleClosed);

            Oper = QuicOperationAlloc(Connection.Worker, QUIC_OPERATION_TYPE.QUIC_OPER_TYPE_API_CALL);
            if (Oper == null)
            {
                if (Interlocked.CompareExchange(ref Connection.BackUpOperUsed, 1, 0) != 0)
                {
                    goto Error;
                }
                Oper = Connection.BackUpOper;
                Oper.FreeAfterProcess = false;
                Oper.Type = QUIC_OPERATION_TYPE.QUIC_OPER_TYPE_API_CALL;
                Oper.API_CALL.Context = Connection.BackupApiContext;
            }

            Oper.API_CALL.Context.Type = QUIC_API_TYPE.QUIC_API_TYPE_CONN_SHUTDOWN;
            Oper.API_CALL.Context.CONN_SHUTDOWN.Flags = Flags;
            Oper.API_CALL.Context.CONN_SHUTDOWN.ErrorCode = ErrorCode;
            Oper.API_CALL.Context.CONN_SHUTDOWN.RegistrationShutdown = false;
            Oper.API_CALL.Context.CONN_SHUTDOWN.TransportShutdown = false;

            QuicConnQueueHighestPriorityOper(Connection, Oper);
        Error:
            QuicTraceEvent(QuicEventId.ApiExit, "[ api] Exit");
        }

        public static ulong MsQuicConnectionStart(QUIC_CONNECTION Handle, QUIC_CONFIGURATION ConfigHandle, QUIC_ADDR ServerAddr)
        {
            ulong Status;
            QUIC_CONNECTION Connection;
            QUIC_CONFIGURATION Configuration;

            if (ConfigHandle == null || ConfigHandle.Type != QUIC_HANDLE_TYPE.QUIC_HANDLE_TYPE_CONFIGURATION || ServerAddr.nPort == 0)
            {
                Status = QUIC_STATUS_INVALID_PARAMETER;
                goto Error;
            }

            if (ServerAddr.Family != AddressFamily.InterNetwork && ServerAddr.Family != AddressFamily.InterNetworkV6)
            {
                Status = QUIC_STATUS_INVALID_PARAMETER;
                goto Error;
            }

            Connection = (QUIC_CONNECTION)Handle;
            NetLog.Assert(!Connection.State.Freed);
            if (QuicConnIsServer(Connection) || (Connection.State.RemoteAddressSet == false && ServerAddr.ServerName == null))
            {
                Status = QUIC_STATUS_INVALID_PARAMETER;
                goto Error;
            }

            if (Connection.State.Started || Connection.State.ClosedLocally)
            {
                Status = QUIC_STATUS_INVALID_STATE;
                goto Error;
            }

            Configuration = ConfigHandle;
            if (Configuration.SecurityConfig == null)
            {
                Status = QUIC_STATUS_INVALID_PARAMETER;
                goto Error;
            }

            if (ServerAddr.ServerName != null)
            {
                int ServerNameLength = ServerAddr.ServerName.Length;
                if (ServerNameLength == QUIC_MAX_SNI_LENGTH + 1)
                {
                    Status = QUIC_STATUS_INVALID_PARAMETER;
                    goto Error;
                }
            }

            NetLog.Assert(!Connection.State.HandleClosed);
            NetLog.Assert(QuicConnIsClient(Connection));
             
            //发送开始连接指令
            QUIC_OPERATION Oper = QuicOperationAlloc(Connection.Worker, QUIC_OPERATION_TYPE.QUIC_OPER_TYPE_API_CALL);
            if (Oper == null)
            {
                Status = QUIC_STATUS_OUT_OF_MEMORY;
                goto Error;
            }

            QuicConfigurationAddRef(Configuration);
            Oper.API_CALL.Context.Type = QUIC_API_TYPE.QUIC_API_TYPE_CONN_START;
            Oper.API_CALL.Context.CONN_START.Configuration = Configuration;
            Oper.API_CALL.Context.CONN_START.ServerName = ServerAddr.ServerName;
            Oper.API_CALL.Context.CONN_START.ServerPort = (ushort)ServerAddr.nPort;
            Oper.API_CALL.Context.CONN_START.Family = ServerAddr.Family;
            QuicConnQueueOper(Connection, Oper);
            Status = QUIC_STATUS_PENDING;

        Error:
            return Status;
        }

        public static ulong MsQuicConnectionSetConfiguration(QUIC_HANDLE Handle, QUIC_HANDLE ConfigHandle)
        {
            ulong Status;
            QUIC_CONNECTION Connection;
            QUIC_CONFIGURATION Configuration;
            QUIC_OPERATION Oper;

            QuicTraceEvent(QuicEventId.ApiEnter, "[ api] Enter %u (%p).", QUIC_TRACE_API_TYPE.QUIC_TRACE_API_CONNECTION_SET_CONFIGURATION, Handle);

            if (ConfigHandle == null || ConfigHandle.Type != QUIC_HANDLE_TYPE.QUIC_HANDLE_TYPE_CONFIGURATION)
            {
                Status = QUIC_STATUS_INVALID_PARAMETER;
                goto Error;
            }

            if (IS_CONN_HANDLE(Handle))
            {
                Connection = (QUIC_CONNECTION)Handle;
            }
            else if (IS_STREAM_HANDLE(Handle))
            {
                QUIC_STREAM Stream = (QUIC_STREAM)Handle;
                NetLog.Assert(!Stream.Flags.HandleClosed);
                NetLog.Assert(!Stream.Flags.Freed);
                Connection = Stream.Connection;
            }
            else
            {
                Status = QUIC_STATUS_INVALID_PARAMETER;
                goto Error;
            }

            NetLog.Assert(!Connection.State.Freed);

            if (QuicConnIsClient(Connection))
            {
                Status = QUIC_STATUS_INVALID_PARAMETER;
                goto Error;
            }

            if (Connection.Configuration != null)
            {
                Status = QUIC_STATUS_INVALID_STATE;
                goto Error;
            }

            Configuration = (QUIC_CONFIGURATION)ConfigHandle;

            if (Configuration.SecurityConfig == null)
            {
                Status = QUIC_STATUS_INVALID_PARAMETER;
                goto Error;
            }

            NetLog.Assert(!Connection.State.HandleClosed);
            NetLog.Assert(QuicConnIsServer(Connection));
            Oper = QuicOperationAlloc(Connection.Worker, QUIC_OPERATION_TYPE.QUIC_OPER_TYPE_API_CALL);
            if (Oper == null)
            {
                Status = QUIC_STATUS_OUT_OF_MEMORY;
                QuicTraceEvent(QuicEventId.AllocFailure, "Allocation of '%s' failed. (%llu bytes)", "CONN_SET_CONFIGURATION operation", 0);
                goto Error;
            }

            QuicConfigurationAddRef(Configuration);
            Oper.API_CALL.Context.Type = QUIC_API_TYPE.QUIC_API_TYPE_CONN_SET_CONFIGURATION;
            Oper.API_CALL.Context.CONN_SET_CONFIGURATION.Configuration = Configuration;
            QuicConnQueueOper(Connection, Oper);
            Status = QUIC_STATUS_PENDING;

        Error:
            return Status;
        }

        static ulong MsQuicConnectionSendResumptionTicket(QUIC_HANDLE Handle, uint Flags, ushort DataLength, QUIC_SSBuffer ResumptionData)
        {
            ulong Status;
            QUIC_CONNECTION Connection;
            QUIC_OPERATION Oper;
            QUIC_SSBuffer ResumptionDataCopy = QUIC_SSBuffer.Empty;

            QuicTraceEvent(QuicEventId.ApiEnter, "[ api] Enter %u (%p).", QUIC_TRACE_API_TYPE.QUIC_TRACE_API_CONNECTION_SEND_RESUMPTION_TICKET, Handle);

            if (DataLength > QUIC_MAX_RESUMPTION_APP_DATA_LENGTH || (ResumptionData == QUIC_SSBuffer.Empty && DataLength != 0))
            {
                Status = QUIC_STATUS_INVALID_PARAMETER;
                goto Error;
            }

            if (Flags > QUIC_SEND_RESUMPTION_FLAG_FINAL)
            {
                Status = QUIC_STATUS_INVALID_PARAMETER;
                goto Error;
            }

            if (IS_CONN_HANDLE(Handle))
            {
                Connection = (QUIC_CONNECTION)Handle;
            }
            else if (IS_STREAM_HANDLE(Handle))
            {
                QUIC_STREAM Stream = (QUIC_STREAM)Handle;
                NetLog.Assert(!Stream.Flags.HandleClosed);
                NetLog.Assert(!Stream.Flags.Freed);
                Connection = Stream.Connection;
            }
            else
            {
                Status = QUIC_STATUS_INVALID_PARAMETER;
                goto Error;
            }

            NetLog.Assert(!Connection.State.Freed);
            NetLog.Assert(!Connection.State.HandleClosed);

            if (QuicConnIsClient(Connection))
            {
                Status = QUIC_STATUS_INVALID_PARAMETER;
                goto Error;
            }

            if (!Connection.State.ResumptionEnabled ||
                !Connection.State.Connected ||
                !Connection.Crypto.TlsState.HandshakeComplete)
            {
                Status = QUIC_STATUS_INVALID_STATE;
                goto Error;
            }

            if (DataLength > 0)
            {
                ResumptionDataCopy = new byte[DataLength];
                if (ResumptionDataCopy == QUIC_SSBuffer.Empty)
                {
                    Status = QUIC_STATUS_OUT_OF_MEMORY;
                    QuicTraceEvent(QuicEventId.AllocFailure, "Allocation of '%s' failed. (%llu bytes)", "Resumption data copy", DataLength);
                    goto Error;
                }

                ResumptionData.CopyTo(ResumptionDataCopy);
            }

            Oper = QuicOperationAlloc(Connection.Worker, QUIC_OPERATION_TYPE.QUIC_OPER_TYPE_API_CALL);
            if (Oper == null)
            {
                Status = QUIC_STATUS_OUT_OF_MEMORY;
                QuicTraceEvent(QuicEventId.AllocFailure, "Allocation of '%s' failed. (%llu bytes)", "CONN_SEND_RESUMPTION_TICKET operation", 0);
                goto Error;
            }
            Oper.API_CALL.Context.Type = QUIC_API_TYPE.QUIC_API_TYPE_CONN_SEND_RESUMPTION_TICKET;
            Oper.API_CALL.Context.CONN_SEND_RESUMPTION_TICKET.Flags = Flags;
            Oper.API_CALL.Context.CONN_SEND_RESUMPTION_TICKET.ResumptionAppData = ResumptionDataCopy;

            QuicConnQueueOper(Connection, Oper);
            Status = QUIC_STATUS_SUCCESS;
            ResumptionDataCopy = QUIC_SSBuffer.Empty;

        Error:
            return Status;
        }

        public static ulong MsQuicStreamOpen(QUIC_CONNECTION Connection, QUIC_STREAM_OPEN_FLAGS Flags, QUIC_STREAM_CALLBACK Handler, object Contex, out QUIC_STREAM NewStream)
        {
            ulong Status;
            NewStream = null;
            if (Handler == null)
            {
                Status = QUIC_STATUS_INVALID_PARAMETER;
                goto Error;
            }

            NetLog.Assert(!Connection.State.Freed);
            bool ClosedLocally = Connection.State.ClosedLocally;
            if (ClosedLocally || Connection.State.ClosedRemotely)
            {
                Status = ClosedLocally ? QUIC_STATUS_INVALID_STATE : QUIC_STATUS_ABORTED;
                goto Error;
            }

            Status = QuicStreamInitialize(Connection, false, Flags, out NewStream);
            if (QUIC_FAILED(Status))
            {
                goto Error;
            }

            NewStream.ClientCallbackHandler = Handler;
            NewStream.ClientContext = Contex;
        Error:
            return Status;
        }

        static void MsQuicStreamClose(QUIC_HANDLE Handle)
        {
            QUIC_STREAM Stream;
            QUIC_CONNECTION Connection;

            if (!IS_STREAM_HANDLE(Handle))
            {
                goto Error;
            }

            Stream = (QUIC_STREAM)Handle;

            NetLog.Assert(!Stream.Flags.Freed);
            Connection = Stream.Connection;
            NetLog.Assert(!Connection.State.Freed);
            NetLog.Assert(!Stream.Flags.HandleClosed);
            bool IsWorkerThread = Connection.WorkerThreadID == CxPlatCurThreadID();

            if (IsWorkerThread && Stream.Flags.HandleClosed)
            {
                goto Error;
            }

            NetLog.Assert(!Stream.Flags.HandleClosed);

            if (IsWorkerThread)
            {
                bool AlreadyInline = Connection.State.InlineApiExecution;
                if (!AlreadyInline)
                {
                    Connection.State.InlineApiExecution = true;
                }

                QuicStreamClose(Stream);
                if (!AlreadyInline)
                {
                    Connection.State.InlineApiExecution = false;
                }
            }
            else
            {
                bool AlreadyShutdownComplete = Stream.ClientCallbackHandler == null;
                if (AlreadyShutdownComplete)
                {
                    QUIC_OPERATION Oper2 = QuicOperationAlloc(Connection.Worker, QUIC_OPERATION_TYPE.QUIC_OPER_TYPE_API_CALL);
                    if (Oper2 != null)
                    {
                        Oper2.API_CALL.Context.Type = QUIC_API_TYPE.QUIC_API_TYPE_STRM_CLOSE;
                        Oper2.API_CALL.Context.STRM_CLOSE.Stream = Stream;
                        QuicConnQueueOper(Connection, Oper2);
                        goto Error;
                    }
                }

                CXPLAT_EVENT CompletionEvent = new CXPLAT_EVENT();
                QUIC_OPERATION Oper = new QUIC_OPERATION();

                QUIC_API_CONTEXT ApiCtx = new QUIC_API_CONTEXT();
                Oper.Type = QUIC_OPERATION_TYPE.QUIC_OPER_TYPE_API_CALL;
                Oper.FreeAfterProcess = false;
                Oper.API_CALL.Context = ApiCtx;

                ApiCtx.Type = QUIC_API_TYPE.QUIC_API_TYPE_STRM_CLOSE;
                CxPlatEventInitialize(CompletionEvent, true, false);
                ApiCtx.Completed = CompletionEvent;
                ApiCtx.Status = 0;
                ApiCtx.STRM_CLOSE.Stream = Stream;
                QuicConnQueueOper(Connection, Oper);
                CxPlatEventWaitForever(CompletionEvent);
                CxPlatEventUninitialize(CompletionEvent);
            }
        Error:
            return;
        }

        public static ulong MsQuicStreamStart(QUIC_STREAM Stream, QUIC_STREAM_START_FLAGS Flags)
        {
            ulong Status;
            NetLog.Assert(!Stream.Flags.HandleClosed);
            NetLog.Assert(!Stream.Flags.Freed);
            QUIC_CONNECTION Connection = Stream.Connection;
            NetLog.Assert(!Connection.State.Freed);

            if (Stream.Flags.Started)
            {
                Status = QUIC_STATUS_INVALID_STATE;
                goto Exit;
            }

            if (Connection.State.ClosedRemotely)
            {
                Status = QUIC_STATUS_ABORTED;
                goto Exit;
            }

            QUIC_OPERATION Oper = QuicOperationAlloc(Connection.Worker, QUIC_OPERATION_TYPE.QUIC_OPER_TYPE_API_CALL);
            if (Oper == null)
            {
                Status = QUIC_STATUS_OUT_OF_MEMORY;
                goto Exit;
            }

            Oper.API_CALL.Context.Type = QUIC_API_TYPE.QUIC_API_TYPE_STRM_START;
            Oper.API_CALL.Context.STRM_START.Stream = Stream;
            Oper.API_CALL.Context.STRM_START.Flags = Flags;

            QuicStreamAddRef(Stream, QUIC_STREAM_REF.QUIC_STREAM_REF_OPERATION);
            if (BoolOk((uint)(Flags & QUIC_STREAM_START_FLAGS.QUIC_STREAM_START_FLAG_PRIORITY_WORK)))
            {
                QuicConnQueuePriorityOper(Connection, Oper);
            }
            else
            {
                QuicConnQueueOper(Connection, Oper);
            }
            Status = QUIC_STATUS_PENDING;

        Exit:
            return Status;
        }

        public static ulong MsQuicStreamShutdown(QUIC_HANDLE Handle, QUIC_STREAM_SHUTDOWN_FLAGS Flags, ulong ErrorCode)
        {
            ulong Status;
            QUIC_STREAM Stream = null;
            QUIC_CONNECTION Connection;
            QUIC_OPERATION Oper;

            if (!IS_STREAM_HANDLE(Handle) || Flags == 0 || (uint)Flags == QUIC_STREAM_SHUTDOWN_SILENT)
            {
                Status = QUIC_STATUS_INVALID_PARAMETER;
                goto Error;
            }

            if (ErrorCode > QUIC_UINT62_MAX)
            {
                Status = QUIC_STATUS_INVALID_PARAMETER;
                goto Error;
            }

            Stream = (QUIC_STREAM)Handle;

            NetLog.Assert(!Stream.Flags.HandleClosed);
            NetLog.Assert(!Stream.Flags.Freed);
            Connection = Stream.Connection;

            NetLog.Assert(!Connection.State.Freed);

            if (Flags.HasFlag(QUIC_STREAM_SHUTDOWN_FLAGS.QUIC_STREAM_SHUTDOWN_FLAG_INLINE) && Connection.WorkerThreadID == CxPlatCurThreadID())
            {
                bool AlreadyInline = Connection.State.InlineApiExecution;
                if (!AlreadyInline)
                {
                    Connection.State.InlineApiExecution = true;
                }
                QuicStreamShutdown(Stream, (byte)Flags, ErrorCode);
                if (!AlreadyInline)
                {
                    Connection.State.InlineApiExecution = false;
                }

                Status = QUIC_STATUS_SUCCESS;
                goto Error;
            }

            Oper = QuicOperationAlloc(Connection.Worker,  QUIC_OPERATION_TYPE.QUIC_OPER_TYPE_API_CALL);
            if (Oper == null)
            {
                Status = QUIC_STATUS_OUT_OF_MEMORY;
                goto Error;
            }
            Oper.API_CALL.Context.Type =  QUIC_API_TYPE.QUIC_API_TYPE_STRM_SHUTDOWN;
            Oper.API_CALL.Context.STRM_SHUTDOWN.Stream = Stream;
            Oper.API_CALL.Context.STRM_SHUTDOWN.Flags = (byte)Flags;
            Oper.API_CALL.Context.STRM_SHUTDOWN.ErrorCode = ErrorCode;

            QuicStreamAddRef(Stream,  QUIC_STREAM_REF.QUIC_STREAM_REF_OPERATION);
            QuicConnQueueOper(Connection, Oper);
            Status = QUIC_STATUS_PENDING;

        Error:
            return Status;
        }
        
        public static ulong MsQuicStreamSend(QUIC_HANDLE Handle, QUIC_BUFFER[] Buffers, int BufferCount, QUIC_SEND_FLAGS Flags)
        {
            ulong Status;
            QUIC_STREAM Stream;
            QUIC_CONNECTION Connection;
            long TotalLength;
            QUIC_SEND_REQUEST SendRequest;
            bool QueueOper = true;
            bool IsPriority = Flags.HasFlag(QUIC_SEND_FLAGS.QUIC_SEND_FLAG_PRIORITY_WORK);
            bool SendInline;
            QUIC_OPERATION Oper;

            if (!IS_STREAM_HANDLE(Handle) || (Buffers == null && BufferCount != 0))
            {
                Status = QUIC_STATUS_INVALID_PARAMETER;
                goto Exit;
            }

            Stream = (QUIC_STREAM)Handle;

            NetLog.Assert(!Stream.Flags.HandleClosed);
            NetLog.Assert(!Stream.Flags.Freed);

            Connection = Stream.Connection;

            if (Connection.State.ClosedRemotely)
            {
                Status = QUIC_STATUS_ABORTED;
                goto Exit;
            }

            TotalLength = 0;
            for (int i = 0; i < BufferCount; ++i)
            {
                TotalLength += Buffers[i].Length;
            }
            if (TotalLength > uint.MaxValue)
            {
                Status = QUIC_STATUS_INVALID_PARAMETER;
                goto Exit;
            }
            SendRequest = Connection.Worker.SendRequestPool.CxPlatPoolAlloc();
            if (SendRequest == null)
            {
                Status = QUIC_STATUS_OUT_OF_MEMORY;
                goto Exit;
            }

            SendRequest.Next = null;
            SendRequest.Buffers = Buffers;
            SendRequest.Flags = (Flags & ~QUIC_SEND_FLAGS.QUIC_SEND_FLAG_BUFFERED);
            SendRequest.TotalLength = (int)TotalLength;

            SendInline = !Connection.Settings.SendBufferingEnabled && Connection.WorkerThreadID == CxPlatCurThreadID();

            Monitor.Enter(Stream.ApiSendRequestLock);
            if (!Stream.Flags.SendEnabled)
            {
                Status = (Connection.State.ClosedRemotely || Stream.Flags.ReceivedStopSending) ? QUIC_STATUS_ABORTED : QUIC_STATUS_INVALID_STATE;
            }
            else
            {
                QUIC_SEND_REQUEST ApiSendRequestsTail = Stream.ApiSendRequests;
                while (ApiSendRequestsTail != null)
                {
                    ApiSendRequestsTail = ApiSendRequestsTail.Next;
                    QueueOper = false;
                }

                ApiSendRequestsTail = SendRequest;
                Status = QUIC_STATUS_SUCCESS;

                if (!SendInline && QueueOper)
                {
                    QuicStreamAddRef(Stream, QUIC_STREAM_REF.QUIC_STREAM_REF_OPERATION);
                }
            }
            Monitor.Exit(Stream.ApiSendRequestLock);

            if (QUIC_FAILED(Status))
            {
                goto Exit;
            }

            Status = QUIC_STATUS_PENDING;

            if (SendInline)
            {
                bool AlreadyInline = Connection.State.InlineApiExecution;
                if (!AlreadyInline)
                {
                    Connection.State.InlineApiExecution = true;
                }
                QuicStreamSendFlush(Stream);
                if (!AlreadyInline)
                {
                    Connection.State.InlineApiExecution = false;
                }

            }
            else if (QueueOper)
            {
                Oper = QuicOperationAlloc(Connection.Worker,  QUIC_OPERATION_TYPE.QUIC_OPER_TYPE_API_CALL);
                if (Oper == null)
                {
                    goto Exit;
                }
                Oper.API_CALL.Context.STRM_SEND.Stream = Stream;
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


        public static ulong MsQuicStreamReceiveSetEnabled(QUIC_STREAM Handle, bool IsEnabled)
        {
            ulong Status;
            QUIC_STREAM Stream;
            QUIC_CONNECTION Connection;
            QUIC_OPERATION Oper;

            if (!IS_STREAM_HANDLE(Handle))
            {
                Status = QUIC_STATUS_INVALID_PARAMETER;
                goto Error;
            }

            Stream = (QUIC_STREAM)Handle;
            NetLog.Assert(!Stream.Flags.HandleClosed);
            NetLog.Assert(!Stream.Flags.Freed);
            Connection = Stream.Connection;

            Oper = QuicOperationAlloc(Connection.Worker, QUIC_OPERATION_TYPE.QUIC_OPER_TYPE_API_CALL);
            if (Oper == null)
            {
                Status = QUIC_STATUS_OUT_OF_MEMORY;
                goto Error;
            }
            Oper.API_CALL.Context.Type = QUIC_API_TYPE.QUIC_API_TYPE_STRM_RECV_SET_ENABLED;
            Oper.API_CALL.Context.STRM_RECV_SET_ENABLED.Stream = Stream;
            Oper.API_CALL.Context.STRM_RECV_SET_ENABLED.IsEnabled = IsEnabled;
            QuicStreamAddRef(Stream, QUIC_STREAM_REF.QUIC_STREAM_REF_OPERATION);
            QuicConnQueueOper(Connection, Oper);
            Status = QUIC_STATUS_PENDING;

        Error:
            return Status;
        }

        static void MsQuicStreamReceiveComplete(QUIC_HANDLE Handle, long BufferLength)
        {
            QUIC_STREAM Stream;
            QUIC_CONNECTION Connection;
            QUIC_OPERATION Oper;

            if (!IS_STREAM_HANDLE(Handle))
            {
                goto Exit;
            }

            Stream = (QUIC_STREAM)Handle;
            NetLog.Assert(!Stream.Flags.HandleClosed);
            NetLog.Assert(!Stream.Flags.Freed);
            Connection = Stream.Connection;
            NetLog.Assert((Stream.RecvPendingLength == 0) || BufferLength <= Stream.RecvPendingLength);
            Interlocked.Add(ref Stream.RecvCompletionLength, (int)BufferLength);
            if (Connection.WorkerThreadID == CxPlatCurThreadID() && Stream.Flags.ReceiveCallActive)
            {
                goto Exit;
            }

            Oper = Interlocked.Exchange(ref Stream.ReceiveCompleteOperation, null);
            if (Oper != null)
            {
                QuicStreamAddRef(Stream,  QUIC_STREAM_REF.QUIC_STREAM_REF_OPERATION);
                QuicConnQueueOper(Connection, Oper);
            }

        Exit:
            return;
        }

        public static ulong MsQuicSetParam(QUIC_HANDLE Handle, uint Param, QUIC_SSBuffer Buffer)
        {
            bool IsPriority = BoolOk(Param & QUIC_PARAM_HIGH_PRIORITY);
            Param &= ~QUIC_PARAM_HIGH_PRIORITY;

            if ((Handle == null) ^ QUIC_PARAM_IS_GLOBAL(Param))
            {
                return QUIC_STATUS_INVALID_PARAMETER;
            }

            ulong Status = 0;
            if (QUIC_PARAM_IS_GLOBAL(Param))
            {
                Status = QuicLibrarySetGlobalParam(Param, Buffer.GetSpan());
                goto Error;
            }

            if (Handle.Type == QUIC_HANDLE_TYPE.QUIC_HANDLE_TYPE_REGISTRATION ||
                Handle.Type == QUIC_HANDLE_TYPE.QUIC_HANDLE_TYPE_CONFIGURATION ||
                Handle.Type == QUIC_HANDLE_TYPE.QUIC_HANDLE_TYPE_LISTENER)
            {
                Status = QuicLibrarySetParam(Handle, Param, Buffer.GetSpan());
                goto Error;
            }

            QUIC_CONNECTION Connection;
            CXPLAT_EVENT CompletionEvent = new CXPLAT_EVENT();
            if (Handle.Type == QUIC_HANDLE_TYPE.QUIC_HANDLE_TYPE_STREAM)
            {
                Connection = ((QUIC_STREAM)Handle).Connection;
            }
            else if (Handle.Type == QUIC_HANDLE_TYPE.QUIC_HANDLE_TYPE_CONNECTION_SERVER ||
                Handle.Type == QUIC_HANDLE_TYPE.QUIC_HANDLE_TYPE_CONNECTION_CLIENT)
            {
                Connection = (QUIC_CONNECTION)Handle;
            }
            else
            {
                Status = QUIC_STATUS_INVALID_PARAMETER;
                goto Error;
            }

            NetLog.Assert(!Connection.State.Freed);
            if (Connection.WorkerThreadID == CxPlatCurThreadID())
            {
                bool AlreadyInline = Connection.State.InlineApiExecution;
                if (!AlreadyInline)
                {
                    Connection.State.InlineApiExecution = true;
                }

                Status = QuicLibrarySetParam(Handle, Param, Buffer.GetSpan());
                if (!AlreadyInline)
                {
                    Connection.State.InlineApiExecution = false;
                }
                goto Error;
            }

            QUIC_OPERATION Oper = new QUIC_OPERATION();
            QUIC_API_CONTEXT ApiCtx = new QUIC_API_CONTEXT();

            Oper.Type = QUIC_OPERATION_TYPE.QUIC_OPER_TYPE_API_CALL;
            Oper.FreeAfterProcess = false;
            Oper.API_CALL.Context = ApiCtx;

            ApiCtx.Type = QUIC_API_TYPE.QUIC_API_TYPE_SET_PARAM;
            CxPlatEventInitialize(CompletionEvent, true, false);
            ApiCtx.Completed = CompletionEvent;
            ApiCtx.Status = Status;
            ApiCtx.SET_PARAM.Handle = Handle;
            ApiCtx.SET_PARAM.Param = Param;
            ApiCtx.SET_PARAM.Buffer = Buffer;

            if (IsPriority)
            {
                QuicConnQueuePriorityOper(Connection, Oper);
            }
            else
            {
                QuicConnQueueOper(Connection, Oper);
            }

            CxPlatEventWaitForever(CompletionEvent);
            CxPlatEventUninitialize(CompletionEvent);

        Error:
            return Status;
        }

        public static ulong MsQuicGetParam(QUIC_HANDLE Handle, uint Param, QUIC_SSBuffer Buffer)
        {
            //    bool IsPriority = BoolOk(Param & QUIC_PARAM_HIGH_PRIORITY);
            //    Param &= ~QUIC_PARAM_HIGH_PRIORITY;

            //    if ((Handle == null) ^ QUIC_PARAM_IS_GLOBAL(Param) || BufferLength == 0)
            //    {
            //        return QUIC_STATUS_INVALID_PARAMETER;
            //    }

            //    ulong Status = 0;
            //    if (QUIC_PARAM_IS_GLOBAL(Param))
            //    {
            //        Status = QuicLibraryGetGlobalParam(Param, BufferLength, Buffer);
            //        goto Error;
            //    }

            //    if (Handle.Type == QUIC_HANDLE_TYPE.QUIC_HANDLE_TYPE_REGISTRATION ||
            //        Handle.Type == QUIC_HANDLE_TYPE.QUIC_HANDLE_TYPE_CONFIGURATION ||
            //        Handle.Type == QUIC_HANDLE_TYPE.QUIC_HANDLE_TYPE_LISTENER)
            //    {
            //        Status = QuicLibraryGetParam(Handle, Param, BufferLength, Buffer);
            //        goto Error;
            //    }

            //    QUIC_CONNECTION Connection;
            //    CXPLAT_EVENT CompletionEvent = new CXPLAT_EVENT();
            //    if (Handle.Type == QUIC_HANDLE_TYPE.QUIC_HANDLE_TYPE_STREAM)
            //    {
            //        Connection = ((QUIC_STREAM)Handle).Connection;
            //    }
            //    else if (Handle.Type == QUIC_HANDLE_TYPE.QUIC_HANDLE_TYPE_CONNECTION_SERVER ||
            //        Handle.Type == QUIC_HANDLE_TYPE.QUIC_HANDLE_TYPE_CONNECTION_CLIENT)
            //    {
            //        Connection = (QUIC_CONNECTION)Handle;
            //    }
            //    else
            //    {
            //        Status = QUIC_STATUS_INVALID_PARAMETER;
            //        goto Error;
            //    }

            //    NetLog.Assert(!Connection.State.Freed);
            //    if (Connection.WorkerThreadID == CxPlatCurThreadID())
            //    {
            //        bool AlreadyInline = Connection.State.InlineApiExecution;
            //        if (!AlreadyInline)
            //        {
            //            Connection.State.InlineApiExecution = true;
            //        }
            //        Status = QuicLibraryGetParam(Handle, Param, BufferLength, Buffer);
            //        if (!AlreadyInline)
            //        {
            //            Connection.State.InlineApiExecution = false;
            //        }
            //        goto Error;
            //    }

            //    QUIC_OPERATION Oper = new QUIC_OPERATION();
            //    QUIC_API_CONTEXT ApiCtx;

            //    Oper.Type = QUIC_OPERATION_TYPE.QUIC_OPER_TYPE_API_CALL;
            //    Oper.FreeAfterProcess = false;
            //    Oper.API_CALL.Context = ApiCtx;

            //    ApiCtx.Type = QUIC_API_TYPE.QUIC_API_TYPE_GET_PARAM;
            //    CxPlatEventInitialize(CompletionEvent, true, false);
            //    ApiCtx.Completed = CompletionEvent;
            //    ApiCtx.Status = Status;
            //    ApiCtx.GET_PARAM.Handle = Handle;
            //    ApiCtx.GET_PARAM.Param = Param;
            //    ApiCtx.GET_PARAM.BufferLength = BufferLength;
            //    ApiCtx.GET_PARAM.Buffer = Buffer;

            //    if (IsPriority)
            //    {
            //        QuicConnQueuePriorityOper(Connection, Oper);
            //    }
            //    else
            //    {
            //        QuicConnQueueOper(Connection, Oper);
            //    }

            //    CxPlatEventWaitForever(CompletionEvent);
            //    CxPlatEventUninitialize(CompletionEvent);

            //Error:
            return QUIC_STATUS_SUCCESS;
        }

        static ulong MsQuicDatagramSend(QUIC_HANDLE Handle, QUIC_BUFFER[] Buffers, int BufferCount, QUIC_SEND_FLAGS Flags, object ClientSendContext)
        {
            ulong Status;
            QUIC_CONNECTION Connection;
            int TotalLength;
            QUIC_SEND_REQUEST SendRequest;

            if (!IS_CONN_HANDLE(Handle) || Buffers == null || BufferCount == 0)
            {
                Status = QUIC_STATUS_INVALID_PARAMETER;
                goto Error;
            }

            Connection = (QUIC_CONNECTION)Handle;
            NetLog.Assert(!Connection.State.Freed);

            TotalLength = 0;
            for (int i = 0; i < BufferCount; ++i)
            {
                TotalLength += Buffers[i].Length;
            }

            if (TotalLength > ushort.MaxValue)
            {
                Status = QUIC_STATUS_INVALID_PARAMETER;
                goto Error;
            }

            SendRequest = Connection.Worker.SendRequestPool.CxPlatPoolAlloc();
            if (SendRequest == null)
            {
                Status = QUIC_STATUS_OUT_OF_MEMORY;
                goto Error;
            }

            SendRequest.Next = null;
            SendRequest.Buffers = Buffers;
            SendRequest.Flags = Flags;
            SendRequest.TotalLength = TotalLength;
            SendRequest.ClientContext = ClientSendContext;

            Status = QuicDatagramQueueSend(Connection.Datagram, SendRequest);
        Error:
            return Status;
        }

        static ulong MsQuicConnectionResumptionTicketValidationComplete(QUIC_HANDLE Handle, bool Result)
        {
            ulong Status;
            QUIC_CONNECTION Connection;
            QUIC_OPERATION Oper;

            if (IS_CONN_HANDLE(Handle))
            {
                Connection = (QUIC_CONNECTION)Handle;
            }
            else if (IS_STREAM_HANDLE(Handle))
            {
                QUIC_STREAM Stream = (QUIC_STREAM)Handle;
                NetLog.Assert(!Stream.Flags.HandleClosed);
                NetLog.Assert(!Stream.Flags.Freed);
                Connection = Stream.Connection;
            }
            else
            {
                Status = QUIC_STATUS_INVALID_PARAMETER;
                goto Error;
            }

            NetLog.Assert(!Connection.State.Freed);

            if (QuicConnIsClient(Connection))
            {
                Status = QUIC_STATUS_INVALID_PARAMETER;
                goto Error;
            }

            if (Connection.Crypto.TlsState.HandshakeComplete || Connection.Crypto.TlsState.SessionResumed)
            {
                Status = QUIC_STATUS_INVALID_STATE;
                goto Error;
            }

            Oper = QuicOperationAlloc(Connection.Worker, QUIC_OPERATION_TYPE.QUIC_OPER_TYPE_API_CALL);
            if (Oper == null)
            {
                Status = QUIC_STATUS_OUT_OF_MEMORY;
                goto Error;
            }

            Oper.API_CALL.Context.Type = QUIC_API_TYPE.QUIC_API_TYPE_CONN_COMPLETE_RESUMPTION_TICKET_VALIDATION;
            Oper.API_CALL.Context.CONN_COMPLETE_RESUMPTION_TICKET_VALIDATION.Result = Result;
            QuicConnQueueOper(Connection, Oper);
            Status = QUIC_STATUS_PENDING;

        Error:
            return Status;
        }

        public static ulong MsQuicConnectionCertificateValidationComplete(QUIC_HANDLE Handle, bool Result, QUIC_TLS_ALERT_CODES TlsAlert)
        {
            ulong Status;
            QUIC_CONNECTION Connection;
            QUIC_OPERATION Oper;

            if (IS_CONN_HANDLE(Handle))
            {
                Connection = (QUIC_CONNECTION)Handle;
            }
            else if (IS_STREAM_HANDLE(Handle))
            {
                QUIC_STREAM Stream = (QUIC_STREAM)Handle;
                NetLog.Assert(!Stream.Flags.HandleClosed);
                NetLog.Assert(!Stream.Flags.Freed);
                Connection = Stream.Connection;
            }
            else
            {
                Status = QUIC_STATUS_INVALID_PARAMETER;
                goto Error;
            }

            NetLog.Assert(!Connection.State.Freed);

            if (!Result && TlsAlert > QUIC_TLS_ALERT_CODES.QUIC_TLS_ALERT_CODE_MAX)
            {
                Status = QUIC_STATUS_INVALID_PARAMETER;
                goto Error;
            }

            Oper = QuicOperationAlloc(Connection.Worker, QUIC_OPERATION_TYPE.QUIC_OPER_TYPE_API_CALL);
            if (Oper == null)
            {
                Status = QUIC_STATUS_OUT_OF_MEMORY;
                goto Error;
            }

            Oper.API_CALL.Context.Type = QUIC_API_TYPE.QUIC_API_TYPE_CONN_COMPLETE_CERTIFICATE_VALIDATION;
            Oper.API_CALL.Context.CONN_COMPLETE_CERTIFICATE_VALIDATION.TlsAlert = TlsAlert;
            Oper.API_CALL.Context.CONN_COMPLETE_CERTIFICATE_VALIDATION.Result = Result;
            QuicConnQueueOper(Connection, Oper);
            Status = QUIC_STATUS_PENDING;

        Error:
            return Status;
        }


    }
}

