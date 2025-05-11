using System.Threading;

namespace AKNet.Udp5MSQuic.Common
{
    internal class QUIC_REGISTRATION : QUIC_HANDLE
    {
        public bool IsVerifying;
        public bool NoPartitioning;
        public bool ShuttingDown;
        public QUIC_EXECUTION_PROFILE ExecProfile;
        public QUIC_CONNECTION_SHUTDOWN_FLAGS ShutdownFlags;
        public QUIC_WORKER_POOL WorkerPool;
        public ulong ShutdownErrorCode;
        public string AppName;


        public readonly CXPLAT_LIST_ENTRY Link;
        public readonly object ConfigLock = new object();
        public readonly object ConnectionLock = new object();

        public readonly CXPLAT_LIST_ENTRY Configurations = new CXPLAT_LIST_ENTRY<QUIC_CONFIGURATION>(null);
        public readonly CXPLAT_LIST_ENTRY Connections = new CXPLAT_LIST_ENTRY<QUIC_CONNECTION>(null);
        public readonly CXPLAT_LIST_ENTRY Listeners = new CXPLAT_LIST_ENTRY<QUIC_LISTENER>(null);
        public readonly CXPLAT_RUNDOWN_REF Rundown = new CXPLAT_RUNDOWN_REF();

        public QUIC_REGISTRATION()
        {
            Link = new CXPLAT_LIST_ENTRY<QUIC_REGISTRATION>(this);
        }
    }

    internal static partial class MSQuicFunc
    {
        public static ulong MsQuicRegistrationOpen(QUIC_REGISTRATION_CONFIG Config, ref QUIC_REGISTRATION NewRegistration)
        {
            ulong Status = QUIC_STATUS_SUCCESS;
            bool ExternalRegistration = Config == null || Config.ExecutionProfile != QUIC_EXECUTION_PROFILE.QUIC_EXECUTION_PROFILE_TYPE_INTERNAL;
            int AppNameLength = (Config != null && Config.AppName != null) ? Config.AppName.Length : 0;
            if (AppNameLength >= byte.MaxValue)
            {
                Status = QUIC_STATUS_INVALID_PARAMETER;
                goto Error;
            }

            Status = QuicLibraryLazyInitialize(ExternalRegistration);
            if (QUIC_FAILED(Status))
            {
                goto Error;
            }

            QUIC_REGISTRATION Registration = new QUIC_REGISTRATION();
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
        Error:
            return Status;
        }

        static void QuicRegistrationQueueNewConnection(QUIC_REGISTRATION Registration, QUIC_CONNECTION Connection)
        {
            int Index = Registration.NoPartitioning ? 0 : QuicPartitionIdGetIndex(Connection.PartitionID);
            QuicWorkerAssignConnection(Registration.WorkerPool.Workers[Index], Connection);
        }

        static bool QuicRegistrationAcceptConnection(QUIC_REGISTRATION Registration, QUIC_CONNECTION Connection)
        {
            int Index = Registration.NoPartitioning ? 0 : QuicPartitionIdGetIndex(Connection.PartitionID);
            return !QuicWorkerIsOverloaded(Registration.WorkerPool.Workers[Index]);
        }

        static ulong QuicRegistrationParamSet(QUIC_REGISTRATION Registration, uint Param, QUIC_SSBuffer Buffer)
        {
            return QUIC_STATUS_INVALID_PARAMETER;
        }

        static ulong QuicRegistrationParamGet(QUIC_REGISTRATION Registration, uint Param, QUIC_SSBuffer Buffer)
        {
            return QUIC_STATUS_INVALID_PARAMETER;
        }

        static void QuicRegistrationTraceRundown(QUIC_REGISTRATION Registration)
        {
            CxPlatLockAcquire(Registration.ConfigLock);

            for (CXPLAT_LIST_ENTRY Link = Registration.Configurations.Next;  Link != Registration.Configurations; Link = Link.Next)
            {
               
            }

            CxPlatLockRelease(Registration.ConfigLock);

            CxPlatDispatchLockAcquire(Registration.ConnectionLock);

            for (CXPLAT_LIST_ENTRY Link = Registration.Connections.Next; Link != Registration.Connections; Link = Link.Next)
            {
                QuicConnQueueTraceRundown(CXPLAT_CONTAINING_RECORD<QUIC_CONNECTION>(Link));
            }

            CxPlatDispatchLockRelease(Registration.ConnectionLock);
        }

        static void QuicRegistrationSettingsChanged(QUIC_REGISTRATION Registration)
        {
            CxPlatLockAcquire(Registration.ConfigLock);

            for (CXPLAT_LIST_ENTRY Link = Registration.Configurations.Next; Link != Registration.Configurations; Link = Link.Next)
            {
                QuicConfigurationSettingsChanged(CXPLAT_CONTAINING_RECORD<QUIC_CONFIGURATION>(Link));
            }
            CxPlatLockRelease(Registration.ConfigLock);
        }

    }
}
