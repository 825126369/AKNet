/************************************Copyright*****************************************
*        ProjectName:AKNet
*        Web:https://github.com/825126369/AKNet
*        Description:C#游戏网络库
*        Author:许珂
*        ModifyTime:2025/11/14 8:44:36
*        Copyright:MIT软件许可证
************************************Copyright*****************************************/
using AKNet.Common;
using System.Threading;

namespace MSQuic2
{
    internal class QUIC_REGISTRATION : QUIC_HANDLE
    {
        public bool IsVerifying;
        public bool NoPartitioning;
        public bool ShuttingDown;
        public QUIC_EXECUTION_PROFILE ExecProfile;
        public QUIC_CONNECTION_SHUTDOWN_FLAGS ShutdownFlags;
        public QUIC_WORKER_POOL WorkerPool;
        public int ShutdownErrorCode;
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
        public static int MsQuicRegistrationOpen(QUIC_REGISTRATION_CONFIG Config, out QUIC_REGISTRATION NewRegistration)
        {
            int Status = QUIC_STATUS_SUCCESS;
            QUIC_REGISTRATION Registration = NewRegistration = null;

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

            Registration = new QUIC_REGISTRATION();
            if (Registration == null)
            {
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

            Status = QuicWorkerPoolInitialize(Registration, Registration.ExecProfile, out Registration.WorkerPool);
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
            if (Registration != null)
            {
                CxPlatRundownUninitialize(Registration.Rundown);
                Registration = null;
            }
            
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

        static void MsQuicRegistrationShutdown(QUIC_HANDLE Handle, QUIC_CONNECTION_SHUTDOWN_FLAGS Flags, int ErrorCode)
        {
            NetLog.Assert(Handle != null);
            NetLog.Assert(Handle.Type == QUIC_HANDLE_TYPE.QUIC_HANDLE_TYPE_REGISTRATION);

            if (ErrorCode > (long)QUIC_UINT62_MAX)
            {
                return;
            }

            if (Handle != null && Handle.Type == QUIC_HANDLE_TYPE.QUIC_HANDLE_TYPE_REGISTRATION)
            {
                QUIC_REGISTRATION Registration = (QUIC_REGISTRATION)Handle;
                CxPlatDispatchLockAcquire(Registration.ConnectionLock);
                if (Registration.ShuttingDown)
                {
                    CxPlatDispatchLockRelease(Registration.ConnectionLock);
                    goto Exit;
                }

                Registration.ShutdownErrorCode = ErrorCode;
                Registration.ShutdownFlags = Flags;
                Registration.ShuttingDown = true;

                CXPLAT_LIST_ENTRY Entry = Registration.Connections.Next;
                while (Entry != Registration.Connections)
                {
                    QUIC_CONNECTION Connection = CXPLAT_CONTAINING_RECORD<QUIC_CONNECTION>(Entry);

                    if (Interlocked.CompareExchange(ref Connection.BackUpOperUsed, 1, 0) == 0)
                    {
                        QUIC_OPERATION Oper = Connection.BackUpOper;
                        Oper.FreeAfterProcess = false;
                        Oper.Type = QUIC_OPERATION_TYPE.QUIC_OPER_TYPE_API_CALL;
                        Oper.API_CALL.Context = Connection.BackupApiContext;
                        Oper.API_CALL.Context.Type = QUIC_API_TYPE.QUIC_API_TYPE_CONN_SHUTDOWN;
                        Oper.API_CALL.Context.CONN_SHUTDOWN.Flags = Flags;
                        Oper.API_CALL.Context.CONN_SHUTDOWN.ErrorCode = ErrorCode;
                        Oper.API_CALL.Context.CONN_SHUTDOWN.RegistrationShutdown = true;
                        Oper.API_CALL.Context.CONN_SHUTDOWN.TransportShutdown = false;

                        NetLog.LogError("MsQuicRegistrationShutdown QUIC_API_TYPE_CONN_SHUTDOWN");
                        QuicConnQueueHighestPriorityOper(Connection, Oper);
                    }

                    Entry = Entry.Next;
                }

                CxPlatDispatchLockRelease(Registration.ConnectionLock);

                Entry = Registration.Listeners.Next;
                while (Entry != Registration.Listeners)
                {
                    QUIC_LISTENER Listener = CXPLAT_CONTAINING_RECORD<QUIC_LISTENER>(Entry);
                    Entry = Entry.Next;
                    MsQuicListenerStop(Listener);
                }
            }

        Exit:
            return;
        }

        static void MsQuicRegistrationClose(QUIC_HANDLE Handle)
        {
            if (Handle != null && Handle.Type ==  QUIC_HANDLE_TYPE.QUIC_HANDLE_TYPE_REGISTRATION)
            {
                QUIC_REGISTRATION Registration = (QUIC_REGISTRATION)Handle;

                if (Registration.ExecProfile !=  QUIC_EXECUTION_PROFILE.QUIC_EXECUTION_PROFILE_TYPE_INTERNAL)
                {
                    CxPlatLockAcquire(MsQuicLib.Lock);
                    CxPlatListEntryRemove(Registration.Link);
                    CxPlatLockRelease(MsQuicLib.Lock);
                }

                CxPlatRundownReleaseAndWait(Registration.Rundown);
                QuicWorkerPoolUninitialize(Registration.WorkerPool);
                CxPlatRundownUninitialize(Registration.Rundown);
            }
        }

    }
}
