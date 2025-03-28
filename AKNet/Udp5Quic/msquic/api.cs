using AKNet.Common;
using AKNet.Udp5Quic.Common;
using System;
using System.IO;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Threading;

namespace AKNet.Udp5Quic.Common
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

        static long MsQuicConnectionOpen(QUIC_REGISTRATION RegistrationHandle, QUIC_CONNECTION_CALLBACK Handler, QUIC_API_CONTEXT Context, QUIC_CONNECTION NewConnection)
        {
            long Status;
            QUIC_REGISTRATION Registration;
            QUIC_CONNECTION Connection = null;

            if (!IS_REGISTRATION_HANDLE(RegistrationHandle) ||
                NewConnection == null ||
                Handler == null)
            {
                Status = QUIC_STATUS_INVALID_PARAMETER;
                goto Error;
            }

            Registration = (QUIC_REGISTRATION)RegistrationHandle;

            Status =
                QuicConnAlloc(
                    Registration,
                    null,
                    null,
                    Connection);
            if (QUIC_FAILED(Status))
            {
                goto Error;
            }

            Connection.ClientCallbackHandler = Handler;
            Connection.ClientContext = Context;

            NewConnection = Connection;
            Status = QUIC_STATUS_SUCCESS;

        Error:
            QuicTraceEvent(QuicEventId.ApiExitStatus, "[ api] Exit %u", Status);
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

        static void MsQuicConnectionShutdown(QUIC_HANDLE Handle, QUIC_CONNECTION_SHUTDOWN_FLAGS Flags, ulong ErrorCode)
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

        static ulong MsQuicConnectionStart(QUIC_HANDLE Handle, QUIC_HANDLE ConfigHandle, AddressFamily Family, string ServerName, short ServerPort)
        {
            ulong Status;
            QUIC_CONNECTION Connection;
            QUIC_CONFIGURATION Configuration;
            QUIC_OPERATION Oper;
            string ServerNameCopy = null;

            QuicTraceEvent(QuicEventId.ApiEnter, "[ api] Enter %u (%p).", QUIC_TRACE_API_TYPE.QUIC_TRACE_API_CONNECTION_START, Handle);

            if (ConfigHandle == null || ConfigHandle.Type != QUIC_HANDLE_TYPE.QUIC_HANDLE_TYPE_CONFIGURATION || ServerPort == 0)
            {
                Status = QUIC_STATUS_INVALID_PARAMETER;
                goto Error;
            }

            if (Family != AddressFamily.InterNetwork && Family != AddressFamily.InterNetworkV6)
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

            if (QuicConnIsServer(Connection) || (!Connection.State.RemoteAddressSet && ServerName == null))
            {
                Status = QUIC_STATUS_INVALID_PARAMETER;
                goto Error;
            }

            if (Connection.State.Started || Connection.State.ClosedLocally)
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

            if (ServerName != null)
            {

                int ServerNameLength = ServerName.Length;
                if (ServerNameLength == QUIC_MAX_SNI_LENGTH + 1)
                {
                    Status = QUIC_STATUS_INVALID_PARAMETER;
                    goto Error;
                }

                ServerNameCopy = ServerName;
            }

            NetLog.Assert(!Connection.State.HandleClosed);
            NetLog.Assert(QuicConnIsClient(Connection));
            Oper = QuicOperationAlloc(Connection.Worker, QUIC_OPERATION_TYPE.QUIC_OPER_TYPE_API_CALL);
            if (Oper == null)
            {
                Status = QUIC_STATUS_OUT_OF_MEMORY;
                QuicTraceEvent(QuicEventId.AllocFailure, "Allocation of '%s' failed. (%llu bytes)", "CONN_START operation", 0);
                goto Error;
            }

            QuicConfigurationAddRef(Configuration);
            Oper.API_CALL.Context.Type = QUIC_API_TYPE.QUIC_API_TYPE_CONN_START;
            Oper.API_CALL.Context.CONN_START.Configuration = Configuration;
            Oper.API_CALL.Context.CONN_START.ServerName = ServerNameCopy;
            Oper.API_CALL.Context.CONN_START.ServerPort = ServerPort;
            Oper.API_CALL.Context.CONN_START.Family = Family;
            ServerNameCopy = null;
            QuicConnQueueOper(Connection, Oper);
            Status = QUIC_STATUS_PENDING;

        Error:
            QuicTraceEvent(QuicEventId.ApiExitStatus, "[ api] Exit %u", Status);
            return Status;
        }

        static ulong MsQuicConnectionSetConfiguration(QUIC_HANDLE Handle, QUIC_HANDLE ConfigHandle)
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
            QuicTraceEvent(QuicEventId.ApiExitStatus, "[ api] Exit %u", Status);
            return Status;
        }

        static ulong MsQuicConnectionSendResumptionTicket(QUIC_HANDLE Handle, QUIC_SEND_RESUMPTION_FLAGS Flags, ushort DataLength, byte[] ResumptionData)
        {
            ulong Status;
            QUIC_CONNECTION Connection;
            QUIC_OPERATION Oper;
            byte[] ResumptionDataCopy = null;

            QuicTraceEvent(QuicEventId.ApiEnter, "[ api] Enter %u (%p).", QUIC_TRACE_API_TYPE.QUIC_TRACE_API_CONNECTION_SEND_RESUMPTION_TICKET, Handle);

            if (DataLength > QUIC_MAX_RESUMPTION_APP_DATA_LENGTH || (ResumptionData == null && DataLength != 0))
            {
                Status = QUIC_STATUS_INVALID_PARAMETER;
                goto Error;
            }

            if (Flags > QUIC_SEND_RESUMPTION_FLAGS.QUIC_SEND_RESUMPTION_FLAG_FINAL)
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
                if (ResumptionDataCopy == null)
                {
                    Status = QUIC_STATUS_OUT_OF_MEMORY;
                    QuicTraceEvent(QuicEventId.AllocFailure, "Allocation of '%s' failed. (%llu bytes)", "Resumption data copy", DataLength);
                    goto Error;
                }
                CxPlatCopyMemory(ResumptionDataCopy, ResumptionData, DataLength);
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
            Oper.API_CALL.Context.CONN_SEND_RESUMPTION_TICKET.AppDataLength = DataLength;

            QuicConnQueueOper(Connection, Oper);
            Status = QUIC_STATUS_SUCCESS;
            ResumptionDataCopy = null;

        Error:
            QuicTraceEvent(QuicEventId.ApiExitStatus, "[ api] Exit %u", Status);
            return Status;
        }

        static ulong MsQuicStreamOpen(QUIC_HANDLE Handle, QUIC_STREAM_OPEN_FLAGS Flags, QUIC_STREAM_CALLBACK Handler, void* Context, QUIC_HANDLE NewStream)
        {
            ulong Status;
            QUIC_CONNECTION Connection;

            QuicTraceEvent(QuicEventId.ApiEnter, "[ api] Enter %u (%p).", QUIC_TRACE_API_TYPE.QUIC_TRACE_API_STREAM_OPEN, Handle);

            if (NewStream == null || Handler == null)
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

            bool ClosedLocally = Connection.State.ClosedLocally;
            if (ClosedLocally || Connection.State.ClosedRemotely)
            {
                Status = ClosedLocally ? QUIC_STATUS_INVALID_STATE : QUIC_STATUS_ABORTED;
                goto Error;
            }

            Status = QuicStreamInitialize(Connection, false, Flags, (QUIC_STREAM)NewStream);
            if (QUIC_FAILED(Status))
            {
                goto Error;
            }

            ((QUIC_STREAM)NewStream).ClientCallbackHandler = Handler;
            ((QUIC_STREAM)NewStream).ClientContext = Context;

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
                    QUIC_OPERATION Oper = QuicOperationAlloc(Connection.Worker, QUIC_OPERATION_TYPE.QUIC_OPER_TYPE_API_CALL);
                    if (Oper != null)
                    {
                        Oper.API_CALL.Context.Type = QUIC_API_TYPE.QUIC_API_TYPE_STRM_CLOSE;
                        Oper.API_CALL.Context.STRM_CLOSE.Stream = Stream;
                        QuicConnQueueOper(Connection, Oper);
                        goto Error;
                    }
                }

                CXPLAT_EVENT CompletionEvent = new CXPLAT_EVENT();
                QUIC_OPERATION Oper = new QUIC_OPERATION();
                QUIC_API_CONTEXT ApiCtx;

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


        static ulong MsQuicStreamStart(QUIC_HANDLE Handle, QUIC_STREAM_START_FLAGS Flags)
        {
            ulong Status;
            QUIC_STREAM Stream;
            QUIC_CONNECTION Connection;

            if (!IS_STREAM_HANDLE(Handle))
            {
                Status = QUIC_STATUS_INVALID_PARAMETER;
                goto Exit;
            }

            Stream = (QUIC_STREAM)Handle;
            NetLog.Assert(!Stream.Flags.HandleClosed);
            NetLog.Assert(!Stream.Flags.Freed);
            Connection = Stream.Connection;
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

        static ulong MsQuicStreamShutdown(QUIC_HANDLE Handle, QUIC_STREAM_SHUTDOWN_FLAGS Flags, ulong ErrorCode)
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

            //    if(BoolOk(Flags &  QUIC_STREAM_SHUTDOWN_FLAGS.QUIC_STREAM_SHUTDOWN_FLAG_INLINE && Connection.WorkerThreadID == CxPlatCurThreadID())
            //    {

            //        CXPLAT_PASSIVE_CODE();

            //        //
            //        // Execute this blocking API call inline if called on the worker thread.
            //        //
            //        BOOLEAN AlreadyInline = Connection->State.InlineApiExecution;
            //        if (!AlreadyInline)
            //        {
            //            Connection->State.InlineApiExecution = TRUE;
            //        }
            //        QuicStreamShutdown(Stream, Flags, ErrorCode);
            //        if (!AlreadyInline)
            //        {
            //            Connection->State.InlineApiExecution = FALSE;
            //        }

            //        Status = QUIC_STATUS_SUCCESS;
            //        goto Error;
            //    }

            //    Oper = QuicOperationAlloc(Connection->Worker, QUIC_OPER_TYPE_API_CALL);
            //    if (Oper == NULL)
            //    {
            //        Status = QUIC_STATUS_OUT_OF_MEMORY;
            //        QuicTraceEvent(
            //            AllocFailure,
            //            "Allocation of '%s' failed. (%llu bytes)",
            //            "STRM_SHUTDOWN operation",
            //            0);
            //        goto Error;
            //    }
            //    Oper->API_CALL.Context->Type = QUIC_API_TYPE_STRM_SHUTDOWN;
            //    Oper->API_CALL.Context->STRM_SHUTDOWN.Stream = Stream;
            //    Oper->API_CALL.Context->STRM_SHUTDOWN.Flags = Flags;
            //    Oper->API_CALL.Context->STRM_SHUTDOWN.ErrorCode = ErrorCode;

            //    //
            //    // Async stream operations need to hold a ref on the stream so that the
            //    // stream isn't freed before the operation can be processed. The ref is
            //    // released after the operation is processed.
            //    //
            //    QuicStreamAddRef(Stream, QUIC_STREAM_REF_OPERATION);

            //    //
            //    // Queue the operation but don't wait for the completion.
            //    //
            //    QuicConnQueueOper(Connection, Oper);
            //    Status = QUIC_STATUS_PENDING;

            //Error:

            //    QuicTraceEvent(
            //        ApiExitStatus,
            //        "[ api] Exit %u",
            //        Status);

            //    return Status;
            //}

            return 0;
        }

        static ulong MsQuicStreamSend(QUIC_HANDLE Handle, QUIC_BUFFER[] Buffers, int BufferCount, QUIC_SEND_FLAGS Flags, void* ClientSendContext)
{
    ulong Status;
        QUIC_STREAM Stream;
        QUIC_CONNECTION Connection;
        long TotalLength;
        QUIC_SEND_REQUEST SendRequest;
        bool QueueOper = true;
        bool IsPriority = BoolOk((uint)(Flags &  QUIC_SEND_FLAGS.QUIC_SEND_FLAG_PRIORITY_WORK));
        bool SendInline;
        QUIC_OPERATION Oper;

            if (!IS_STREAM_HANDLE(Handle) || (Buffers == null && BufferCount != 0))
            {
                Status = QUIC_STATUS_INVALID_PARAMETER;
                goto Exit;
            }

    Stream = (QUIC_STREAM) Handle;

    NetLog.Assert(!Stream.Flags.HandleClosed);
    NetLog.Assert(!Stream.Flags.Freed);

    Connection = Stream.Connection;

            if (Connection.State.ClosedRemotely) {
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

            SendRequest = CxPlatPoolAlloc(Connection.Worker.SendRequestPool);
            if (SendRequest == null)
            {
                Status = QUIC_STATUS_OUT_OF_MEMORY;
                goto Exit;
            }

SendRequest.Next = null;
SendRequest.Buffers = Buffers;
SendRequest.BufferCount = BufferCount;
SendRequest.Flags = Flags & ~QUIC_SEND_FLAGS_INTERNAL;
SendRequest.TotalLength = TotalLength;
SendRequest.ClientContext = ClientSendContext;

SendInline = !Connection.Settings.SendBufferingEnabled && !CXPLAT_AT_DISPATCH() && Connection.WorkerThreadID == CxPlatCurThreadID();

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
                    QuicStreamAddRef(Stream,  QUIC_STREAM_REF.QUIC_STREAM_REF_OPERATION);
                }
            }
            Monitor.Exit(Stream.ApiSendRequestLock);

            if (QUIC_FAILED(Status))
            {
                CxPlatPoolFree(&Connection.Worker.SendRequestPool, SendRequest);
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
                Oper = QuicOperationAlloc(Connection->Worker, QUIC_OPER_TYPE_API_CALL);
                if (Oper == NULL)
                {
                    QuicTraceEvent(
                        AllocFailure,
                        "Allocation of '%s' failed. (%llu bytes)",
                        "STRM_SEND operation",
                        0);

                    //
                    // We failed to alloc the operation we needed to queue, so make sure
                    // to release the ref we took above.
                    //
                    QuicStreamRelease(Stream, QUIC_STREAM_REF_OPERATION);

                    //
                    // We can't fail the send at this point, because we're already queued
                    // the send above. So instead, we're just going to abort the whole
                    // connection.
                    //
                    if (InterlockedCompareExchange16(
                            (short*)&Connection->BackUpOperUsed, 1, 0) != 0)
                    {
                        goto Exit; // It's already started the shutdown.
                    }
                    Oper = &Connection->BackUpOper;
                    Oper->FreeAfterProcess = FALSE;
                    Oper->Type = QUIC_OPER_TYPE_API_CALL;
                    Oper->API_CALL.Context = &Connection->BackupApiContext;
                    Oper->API_CALL.Context->Type = QUIC_API_TYPE_CONN_SHUTDOWN;
                    Oper->API_CALL.Context->CONN_SHUTDOWN.Flags = QUIC_CONNECTION_SHUTDOWN_FLAG_SILENT;
                    Oper->API_CALL.Context->CONN_SHUTDOWN.ErrorCode = (QUIC_VAR_INT)QUIC_STATUS_OUT_OF_MEMORY;
                    Oper->API_CALL.Context->CONN_SHUTDOWN.RegistrationShutdown = FALSE;
                    Oper->API_CALL.Context->CONN_SHUTDOWN.TransportShutdown = TRUE;
                    QuicConnQueueHighestPriorityOper(Connection, Oper);
                    goto Exit;
                }

                Oper->API_CALL.Context->Type = QUIC_API_TYPE_STRM_SEND;
                Oper->API_CALL.Context->STRM_SEND.Stream = Stream;

                //
                // Queue the operation but don't wait for the completion.
                //
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

QuicTraceEvent(
    ApiExitStatus,
    "[ api] Exit %u",
    Status);

return Status;
}


    }
}

