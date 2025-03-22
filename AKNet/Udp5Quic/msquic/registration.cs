namespace AKNet.Udp5Quic.Common
{
    internal class QUIC_REGISTRATION :QUIC_HANDLE
    {
        public bool IsVerifying;
        public bool NoPartitioning;
        public bool ShuttingDown;
        public QUIC_EXECUTION_PROFILE ExecProfile;
        public QUIC_CONNECTION_SHUTDOWN_FLAGS ShutdownFlags;
        public CXPLAT_LIST_ENTRY Link;
        public QUIC_WORKER_POOL WorkerPool;
        public readonly object ConfigLock = new object();
        public CXPLAT_LIST_ENTRY Configurations;
        public readonly object onnectionLock = new object();
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
            const int RegistrationSize = sizeof(QUIC_REGISTRATION) + AppNameLength + 1;

            if (NewRegistration == null || AppNameLength >= byte.MaxValue) 
            {
                Status = QUIC_STATUS_INVALID_PARAMETER;
                goto Error;
            }

            Status = QuicLibraryLazyInitialize(ExternalRegistration);
            if (QUIC_FAILED(Status)) {
                goto Error;
            }

Registration = CXPLAT_ALLOC_NONPAGED(RegistrationSize, QUIC_POOL_REGISTRATION);
if (Registration == NULL)
{
    QuicTraceEvent(
        AllocFailure,
        "Allocation of '%s' failed. (%llu bytes)",
        "registration",
        sizeof(QUIC_REGISTRATION) + AppNameLength + 1);
    Status = QUIC_STATUS_OUT_OF_MEMORY;
    goto Error;
}

CxPlatZeroMemory(Registration, RegistrationSize);
Registration->Type = QUIC_HANDLE_TYPE_REGISTRATION;
Registration->ExecProfile =
    Config == NULL ? QUIC_EXECUTION_PROFILE_LOW_LATENCY : Config->ExecutionProfile;
Registration->NoPartitioning =
    Registration->ExecProfile == QUIC_EXECUTION_PROFILE_TYPE_SCAVENGER;
CxPlatLockInitialize(&Registration->ConfigLock);
CxPlatListInitializeHead(&Registration->Configurations);
CxPlatDispatchLockInitialize(&Registration->ConnectionLock);
CxPlatListInitializeHead(&Registration->Connections);
CxPlatListInitializeHead(&Registration->Listeners);
CxPlatRundownInitialize(&Registration->Rundown);
Registration->AppNameLength = (uint8_t)(AppNameLength + 1);
if (AppNameLength != 0)
{
    CxPlatCopyMemory(Registration->AppName, Config->AppName, AppNameLength + 1);
}

Status =
QuicWorkerPoolInitialize(
Registration, Registration->ExecProfile, &Registration->WorkerPool);
if (QUIC_FAILED(Status))
{
    goto Error;
}
QuicTraceEvent(
RegistrationCreatedV2,
    "[ reg][%p] Created, AppName=%s, ExecProfile=%u",
    Registration,
    Registration->AppName,
    Registration->ExecProfile);

# ifdef CxPlatVerifierEnabledByAddr
#pragma prefast(suppress:6001, "SAL doesn't understand checking whether memory is tracked by Verifier.")
if (MsQuicLib.IsVerifying &&
    CxPlatVerifierEnabledByAddr(NewRegistration))
{
    Registration->IsVerifying = TRUE;
    QuicTraceLogInfo(
        RegistrationVerifierEnabled,
        "[ reg][%p] Verifing enabled!",
        Registration);
}
else
{
    Registration->IsVerifying = FALSE;
}
#endif

if (ExternalRegistration)
{
    CxPlatLockAcquire(&MsQuicLib.Lock);
    CxPlatListInsertTail(&MsQuicLib.Registrations, &Registration->Link);
    CxPlatLockRelease(&MsQuicLib.Lock);
}

*NewRegistration = (HQUIC)Registration;
Registration = NULL;
Error:

if (Registration != NULL)
{
    CxPlatRundownUninitialize(&Registration->Rundown);
    CxPlatDispatchLockUninitialize(&Registration->ConnectionLock);
    CxPlatLockUninitialize(&Registration->ConfigLock);
    CXPLAT_FREE(Registration, QUIC_POOL_REGISTRATION);
}

QuicTraceEvent(
    ApiExitStatus,
    "[ api] Exit %u",
    Status);

return Status;
}
    }
}
