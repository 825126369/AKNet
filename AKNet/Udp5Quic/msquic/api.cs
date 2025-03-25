using AKNet.Common;
using System;
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

            Oper = QuicOperationAlloc(Connection.Worker,  QUIC_OPERATION_TYPE.QUIC_OPER_TYPE_API_CALL);
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
    }
}
