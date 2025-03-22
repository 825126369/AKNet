using System.Threading;

namespace AKNet.Udp5Quic.Common
{
    internal class QUIC_REGISTRATION : QUIC_HANDLE
    {
        public bool IsVerifying;
        public bool NoPartitioning;
        public bool ShuttingDown;
        public QUIC_EXECUTION_PROFILE ExecProfile;
        public QUIC_CONNECTION_SHUTDOWN_FLAGS ShutdownFlags;
        public CXPLAT_LIST_ENTRY Link;
        public QUIC_WORKER_POOL WorkerPool;

        public readonly object ConfigLock = new object();
        public readonly object ConnectionLock = new object();

        public CXPLAT_LIST_ENTRY Configurations;
        public CXPLAT_LIST_ENTRY Connections;
        public CXPLAT_LIST_ENTRY Listeners;
        public CXPLAT_RUNDOWN_REF Rundown;
        public ulong ShutdownErrorCode;
        public byte AppNameLength;
        public string AppName;
    }

    internal static partial class MSQuicFunc
    {
        public static long MsQuicRegistrationOpen(QUIC_REGISTRATION_CONFIG Config, QUIC_HANDLE NewRegistration)
        {
            long Status;
            QUIC_REGISTRATION Registration = null;
            bool ExternalRegistration = Config == null || Config.ExecutionProfile != (QUIC_EXECUTION_PROFILE)QUIC_EXECUTION_PROFILE_TYPE_INTERNAL;
            int AppNameLength = (Config != null && Config.AppName != null) ? Config.AppName.Length : 0;

            if (NewRegistration == null || AppNameLength >= byte.MaxValue)
            {
                Status = QUIC_STATUS_INVALID_PARAMETER;
                goto Error;
            }

            Status = QuicLibraryLazyInitialize(ExternalRegistration);
            if (QUIC_FAILED(Status))
            {
                goto Error;
            }

            Registration = new_QUIC_REGISTRATION();
            if (Registration == null)
            {
                QuicTraceEvent(QuicEventId.AllocFailure, "Allocation of '%s' failed. (%llu bytes)", "registration");
                Status = QUIC_STATUS_OUT_OF_MEMORY;
                goto Error;
            }

            Registration.Type = QUIC_HANDLE_TYPE.QUIC_HANDLE_TYPE_REGISTRATION;
            Registration.AppName = Config.AppName;
            Registration.ExecProfile = Config == null ? QUIC_EXECUTION_PROFILE.QUIC_EXECUTION_PROFILE_LOW_LATENCY : Config.ExecutionProfile;
            Registration.NoPartitioning = Registration.ExecProfile == QUIC_EXECUTION_PROFILE.QUIC_EXECUTION_PROFILE_TYPE_SCAVENGER;

            CxPlatListInitializeHead(Registration.Configurations);
            CxPlatListInitializeHead(Registration.Connections);
            CxPlatListInitializeHead(Registration.Listeners);
            CxPlatRundownInitialize(Registration.Rundown);

            Status = QuicWorkerPoolInitialize(Registration, Registration.ExecProfile, ref Registration.WorkerPool);
            if (QUIC_FAILED(Status))
            {
                goto Error;
            }

            if (ExternalRegistration)
            {
                Monitor.Enter(MsQuicLib.Lock);
                CxPlatListInsertTail(MsQuicLib.Registrations, Registration.Link);
                Monitor.Exit(MsQuicLib.Lock);
            }

            NewRegistration = Registration;
            Registration = null;

        Error:
            QuicTraceEvent(QuicEventId.ApiExitStatus, "[ api] Exit %u", Status);
            return Status;
        }
    }
}
