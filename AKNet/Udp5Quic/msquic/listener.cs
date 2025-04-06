using AKNet.Common;
using AKNet.Udp5Quic.Common;
using System;
using System.Collections.Generic;
using System.Net;

namespace AKNet.Udp5Quic.Common
{
    internal class QUIC_LISTENER : QUIC_HANDLE
    {
        public bool WildCard;
        public bool AppClosed;
        public bool Stopped;
        public bool NeedsCleanup;
        public int StopCompleteThreadID;
        public CXPLAT_LIST_ENTRY Link;
        public QUIC_REGISTRATION Registration;
        public CXPLAT_LIST_ENTRY RegistrationLink;
        public long RefCount;
        public CXPLAT_EVENT StopEvent;
        public IPEndPoint LocalAddress;
        public QUIC_BINDING Binding;
        public QUIC_LISTENER_CALLBACK ClientCallbackHandler;
        public ulong TotalAcceptedConnections;
        public ulong TotalRejectedConnections;

        public byte[] AlpnList = null
        public int AlpnListLength = 0;
        public byte[] CibirId = new byte[2 + MSQuicFunc.QUIC_MAX_CIBIR_LENGTH];
    }

    internal enum QUIC_LISTENER_EVENT_TYPE
    {
        QUIC_LISTENER_EVENT_NEW_CONNECTION = 0,
        QUIC_LISTENER_EVENT_STOP_COMPLETE = 1,
    }

    internal class QUIC_LISTENER_EVENT
    {
        public QUIC_LISTENER_EVENT_TYPE Type;
        public NEW_CONNECTION_DATA NEW_CONNECTION;
        public STOP_COMPLETE_DATA STOP_COMPLETE;

        public class NEW_CONNECTION_DATA
        {
            public QUIC_NEW_CONNECTION_INFO Info;
            public QUIC_HANDLE Connection;
        }
        public class STOP_COMPLETE_DATA
        {
            public bool AppCloseInProgress;
            public bool RESERVED;
        }
    }

    internal static partial class MSQuicFunc
    {
        static ulong MsQuicListenerOpen(QUIC_REGISTRATION RegistrationHandle, QUIC_LISTENER_CALLBACK_HANDLER Handler, object Context, QUIC_HANDLE NewListener)
        {
            ulong Status;
            QUIC_REGISTRATION Registration;
            QUIC_LISTENER Listener = null;

            if (RegistrationHandle == null || RegistrationHandle.Type != QUIC_HANDLE_TYPE.QUIC_HANDLE_TYPE_REGISTRATION || NewListener == null || Handler == null)
            {
                Status = QUIC_STATUS_INVALID_PARAMETER;
                goto Error;
            }

            Registration = (QUIC_REGISTRATION)RegistrationHandle;

            Listener = new QUIC_LISTENER();
            if (Listener == null)
            {
                Status = QUIC_STATUS_OUT_OF_MEMORY;
                goto Error;
            }

            Listener.Type = QUIC_HANDLE_TYPE.QUIC_HANDLE_TYPE_LISTENER;
            Listener.Registration = Registration;
            Listener.ClientCallbackHandler = Handler;
            Listener.ClientContext = Context;
            Listener.Stopped = true;
            CxPlatEventInitialize(Listener.StopEvent, true, true);

            bool RegistrationShuttingDown;
            bool Result = CxPlatRundownAcquire(Registration.Rundown);
            NetLog.Assert(Result);

            CxPlatDispatchLockAcquire(Registration.ConnectionLock);
            RegistrationShuttingDown = Registration.ShuttingDown;
            if (!RegistrationShuttingDown)
            {
                CxPlatListInsertTail(Registration.Listeners, Listener.RegistrationLink);
            }
            CxPlatDispatchLockRelease(Registration.ConnectionLock);

            if (RegistrationShuttingDown)
            {
                CxPlatRundownRelease(Registration.Rundown);
                CxPlatEventUninitialize(Listener.StopEvent);
                Listener = null;
                Status = QUIC_STATUS_INVALID_STATE;
                goto Error;
            }

            NewListener = Listener;
            Status = QUIC_STATUS_SUCCESS;
        Error:
            NetLog.Assert(QUIC_SUCCEEDED(Status) || Listener == null);
            return Status;
        }

        static void QuicListenerFree(QUIC_LISTENER Listener)
        {
            QUIC_REGISTRATION Registration = Listener.Registration;
            NetLog.Assert(Listener.Stopped);

            CxPlatDispatchLockAcquire(Listener.Registration.ConnectionLock);
            if (!Listener.Registration.ShuttingDown)
            {
                CxPlatListEntryRemove(Listener.RegistrationLink);
            }
            CxPlatDispatchLockRelease(Listener.Registration.ConnectionLock);
            CxPlatEventUninitialize(Listener.StopEvent);
            NetLog.Assert(Listener.AlpnList == null);
            CxPlatRundownRelease(Registration.Rundown);
        }

        static void MsQuicListenerClose(QUIC_HANDLE Handle)
        {
            NetLog.Assert(Handle == null || Handle.Type ==  QUIC_HANDLE_TYPE.QUIC_HANDLE_TYPE_LISTENER);
            if (Handle == null || Handle.Type !=  QUIC_HANDLE_TYPE.QUIC_HANDLE_TYPE_LISTENER)
            {
                return;
            }

            QUIC_LISTENER Listener = (QUIC_LISTENER)Handle;
            NetLog.Assert(!Listener.AppClosed);
            Listener.AppClosed = true;

            if (Listener.StopCompleteThreadID == CxPlatCurThreadID())
            {
                Listener.NeedsCleanup = true;
            }
            else
            {
                QuicListenerStopAsync(Listener);
                CxPlatEventWaitForever(Listener.StopEvent);
                QuicListenerFree(Listener);
            }
        }

        static void QuicListenerStopAsync(QUIC_LISTENER Listener)
        {
            if (Listener.Binding != null)
            {
                QuicBindingUnregisterListener(Listener.Binding, Listener);
                QuicLibraryReleaseBinding(Listener.Binding);
                Listener.Binding = null;
                QuicListenerRelease(Listener, TRUE);
            }
        }

         static void QuicListenerRelease(QUIC_LISTENER Listener, bool IndicateEvent)
        {
            if (CxPlatRefDecrement(ref Listener.RefCount))
            {
                QuicListenerStopComplete(Listener, IndicateEvent);
            }
        }

        static ulong QuicListenerIndicateEvent(QUIC_LISTENER Listener,QUIC_LISTENER_EVENT Event)
        {
            NetLog.Assert(Listener.ClientCallbackHandler != null);
            return Listener.ClientCallbackHandler(Listener, Listener.ClientContext, Event);
        }

        static void QuicListenerStopComplete(QUIC_LISTENER Listener,bool IndicateEvent)
        {
            if (Listener.AlpnList != null)
            {
                Listener.AlpnList = null;
            }

            if (IndicateEvent)
            {
                QUIC_LISTENER_EVENT Event = new QUIC_LISTENER_EVENT();
                Event.Type =  QUIC_LISTENER_EVENT_TYPE.QUIC_LISTENER_EVENT_STOP_COMPLETE;
                Event.STOP_COMPLETE.AppCloseInProgress = Listener.AppClosed;

                Listener.StopCompleteThreadID = CxPlatCurThreadID();
                QuicListenerIndicateEvent(Listener, Event);
                Listener.StopCompleteThreadID = 0;
            }

            bool CleanupOnExit = Listener.NeedsCleanup;
            Listener.Stopped = true;
            CxPlatEventSet(Listener.StopEvent);

            if (CleanupOnExit)
            {
                QuicListenerFree(Listener);
            }
        }

        static void MsQuicListenerStop(QUIC_HANDLE Handle)
        {
            if (Handle != null && Handle.Type ==  QUIC_HANDLE_TYPE.QUIC_HANDLE_TYPE_LISTENER)
            {
                QUIC_LISTENER Listener = (QUIC_LISTENER)Handle;
                QuicListenerStopAsync(Listener);
            }
        }

        static byte[] QuicListenerFindAlpnInList(QUIC_LISTENER Listener, int OtherAlpnListLength, byte[] OtherAlpnList)
        {
            byte[] AlpnList = Listener.AlpnList;
            int AlpnListLength = Listener.AlpnListLength;

            int nIndex = 0;
            while (AlpnListLength != 0)
            {
                NetLog.Assert(AlpnList[0] + 1 <= AlpnListLength);
                byte[] Result = CxPlatTlsAlpnFindInList(OtherAlpnListLength, OtherAlpnList, AlpnList[0], AlpnList + 1);
                if (Result != null)
                {
                    return AlpnList;
                }
                AlpnListLength -= AlpnList[0] + 1;
                AlpnList += AlpnList[0] + 1;
            }
            return null;
        }

        static bool QuicListenerHasAlpnOverlap(QUIC_LISTENER Listener1, QUIC_LISTENER Listener2)
        {
            return QuicListenerFindAlpnInList(Listener1, Listener2.AlpnListLength, Listener2.AlpnList) != null;
        }

        static bool QuicListenerMatchesAlpn(QUIC_LISTENER Listener, QUIC_NEW_CONNECTION_INFO Info)
        {
            byte[] Alpn = QuicListenerFindAlpnInList(Listener, Info.ClientAlpnListLength, Info.ClientAlpnList);
            if (Alpn != null)
            {
                Info.NegotiatedAlpnLength = Alpn[0]; // The length prefixed to the ALPN buffer.
                Info.NegotiatedAlpn = Alpn + 1;
                return true;
            }
            return false;
        }

        static bool QuicListenerClaimConnection(QUIC_LISTENER Listener, QUIC_CONNECTION Connection, QUIC_NEW_CONNECTION_INFO Info)
        {
            NetLog.Assert(Listener != null);
            NetLog.Assert(Connection.State.ExternalOwner == false);

            Connection.State.ListenerAccepted = true;
            Connection.State.ExternalOwner = true;

            QUIC_LISTENER_EVENT Event = new QUIC_LISTENER_EVENT();
            Event.Type = QUIC_LISTENER_EVENT_TYPE.QUIC_LISTENER_EVENT_NEW_CONNECTION;
            Event.NEW_CONNECTION.Info = Info;
            Event.NEW_CONNECTION.Connection = (QUIC_HANDLE)Connection;

            ulong Status = QuicListenerIndicateEvent(Listener, Event);

            if (QUIC_FAILED(Status))
            {
                NetLog.Assert(!Connection.State.HandleClosed, "App MUST not close and reject connection!");
                Connection.State.ExternalOwner = false;
                QuicConnTransportError(Connection, QUIC_ERROR_CONNECTION_REFUSED);
                return false;
            }

            NetLog.Assert(Connection.State.HandleClosed || Connection.ClientCallbackHandler != null, "App MUST set callback handler or close connection!");
            if (!Connection.State.ShutdownComplete)
            {
                Connection.State.UpdateWorker = true;
            }
            return !Connection.State.HandleClosed;
        }

        static void QuicListenerAcceptConnection(QUIC_LISTENER Listener, QUIC_CONNECTION Connection, QUIC_NEW_CONNECTION_INFO Info)
        {
            if (!QuicRegistrationAcceptConnection(Listener.Registration, Connection))
            {
                QuicConnTransportError(Connection, QUIC_ERROR_CONNECTION_REFUSED);
                Listener.TotalRejectedConnections++;
                QuicPerfCounterIncrement(QUIC_PERFORMANCE_COUNTERS.QUIC_PERF_COUNTER_CONN_LOAD_REJECT);
                return;
            }

            if (!QuicConnRegister(Connection, Listener.Registration))
            {
                return;
            }

            Array.Copy(Listener.CibirId, Connection.CibirId, Listener.CibirId.Length);

            if (Connection.CibirId[0] != 0)
            {

            }

            if (QuicConnGenerateNewSourceCid(Connection, true) == null)
            {
                return;
            }

            if (!QuicListenerClaimConnection(Listener, Connection, Info))
            {
                Listener.TotalRejectedConnections++;
                QuicPerfCounterIncrement(QUIC_PERFORMANCE_COUNTERS.QUIC_PERF_COUNTER_CONN_APP_REJECT);
                return;
            }
            Listener.TotalAcceptedConnections++;
        }

        static ulong QuicListenerParamSet(QUIC_LISTENER Listener, uint Param, int BufferLength, byte[] Buffer)
        {
            if (Param == QUIC_PARAM_LISTENER_CIBIR_ID)
            {
                if (BufferLength > QUIC_MAX_CIBIR_LENGTH + 1) 
                {
                    return QUIC_STATUS_INVALID_PARAMETER;
                }

                if (BufferLength == 0)
                {
                    Array.Clear(Listener.CibirId, 0, Listener.CibirId.Length);
                    return QUIC_STATUS_SUCCESS;
                }

                if (BufferLength < 2)
                {
                    return QUIC_STATUS_INVALID_PARAMETER;
                }

                if ((Buffer)[0] != 0)
                {
                    return QUIC_STATUS_NOT_SUPPORTED; // Not yet supproted.
                }

                Listener.CibirId[0] = (byte)(BufferLength - 1);
                Array.Copy(Buffer, 0, Listener.CibirId, 1, BufferLength);
                return QUIC_STATUS_SUCCESS;
            }

            return QUIC_STATUS_INVALID_PARAMETER;
        }

        static ulong QuicListenerParamGet(QUIC_LISTENER Listener, uint Param, int BufferLength, byte[] Buffer)
        {
            ulong Status;
            switch (Param)
            {
                case QUIC_PARAM_LISTENER_LOCAL_ADDRESS:

                    if (BufferLength < sizeof(QUIC_ADDR))
                    {
                        BufferLength = sizeof(QUIC_ADDR);
                        Status = QUIC_STATUS_BUFFER_TOO_SMALL;
                        break;
                    }

                    if (Buffer == null)
                    {
                        Status = QUIC_STATUS_INVALID_PARAMETER;
                        break;
                    }

                    BufferLength = sizeof(QUIC_ADDR);

                    Array.Copy(Listener.LocalAddress, Buffer, 4);
                    CxPlatCopyMemory(Buffer, Listener.LocalAddress, sizeof(QUIC_ADDR));

                    Status = QUIC_STATUS_SUCCESS;
                    break;

                case QUIC_PARAM_LISTENER_STATS:

                    if (*BufferLength < sizeof(QUIC_LISTENER_STATISTICS))
                    {
                        *BufferLength = sizeof(QUIC_LISTENER_STATISTICS);
                        Status = QUIC_STATUS_BUFFER_TOO_SMALL;
                        break;
                    }

                    if (Buffer == NULL)
                    {
                        Status = QUIC_STATUS_INVALID_PARAMETER;
                        break;
                    }

                    *BufferLength = sizeof(QUIC_LISTENER_STATISTICS);
                    QUIC_LISTENER_STATISTICS* Stats = (QUIC_LISTENER_STATISTICS*)Buffer;

                    Stats->TotalAcceptedConnections = Listener->TotalAcceptedConnections;
                    Stats->TotalRejectedConnections = Listener->TotalRejectedConnections;

                    if (Listener->Binding != NULL)
                    {
                        Stats->BindingRecvDroppedPackets = Listener->Binding->Stats.Recv.DroppedPackets;
                    }
                    else
                    {
                        Stats->BindingRecvDroppedPackets = 0;
                    }

                    Status = QUIC_STATUS_SUCCESS;
                    break;

                case QUIC_PARAM_LISTENER_CIBIR_ID:

                    if (Listener->CibirId[0] == 0)
                    {
                        *BufferLength = 0;
                        return QUIC_STATUS_SUCCESS;
                    }

                    if (*BufferLength < (uint32_t)Listener->CibirId[0] + 1)
                    {
                        *BufferLength = Listener->CibirId[0] + 1;
                        return QUIC_STATUS_BUFFER_TOO_SMALL;
                    }

                    if (Buffer == NULL)
                    {
                        return QUIC_STATUS_INVALID_PARAMETER;
                    }

                    *BufferLength = Listener->CibirId[0] + 1;
                    memcpy(Buffer, Listener->CibirId + 1, Listener->CibirId[0]);

                    Status = QUIC_STATUS_SUCCESS;
                    break;

                default:
                    Status = QUIC_STATUS_INVALID_PARAMETER;
                    break;
            }

            return Status;
        }

    }
}
