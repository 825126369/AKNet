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
                Handle.Type ==  QUIC_HANDLE_TYPE.QUIC_HANDLE_TYPE_CONNECTION_SERVER);
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
                Handler == null) {
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
            if (QUIC_FAILED(Status)) {
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
    }
}
