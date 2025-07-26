using AKNet.Common;
using System;
using System.Threading;

namespace AKNet.Udp1MSQuic.Common
{
    internal class QUIC_LISTENER : QUIC_HANDLE
    {
        public QUIC_REGISTRATION Registration;
        public QUIC_BINDING Binding;

        public bool WildCard;
        public bool AppClosed;
        public bool Stopped;
        public bool NeedsCleanup;
        public int StopCompleteThreadID;
        public readonly CXPLAT_LIST_ENTRY Link;
        public readonly CXPLAT_LIST_ENTRY RegistrationLink;
        public long RefCount;
        public EventWaitHandle StopEvent;
        public readonly QUIC_ADDR LocalAddress = new QUIC_ADDR();
        public QUIC_LISTENER_CALLBACK ClientCallbackHandler;
        public ulong TotalAcceptedConnections;
        public ulong TotalRejectedConnections;

        public QUIC_ALPN_BUFFER AlpnList = null;
        public byte[] CibirId = new byte[2 + MSQuicFunc.QUIC_MAX_CIBIR_LENGTH];

        public QUIC_LISTENER()
        {
            RegistrationLink = new CXPLAT_LIST_ENTRY<QUIC_LISTENER>(this);
            Link = new CXPLAT_LIST_ENTRY<QUIC_LISTENER>(this);
        }
    }

    internal enum QUIC_LISTENER_EVENT_TYPE
    {
        QUIC_LISTENER_EVENT_NEW_CONNECTION = 0,
        QUIC_LISTENER_EVENT_STOP_COMPLETE = 1,
    }

    internal struct QUIC_LISTENER_EVENT
    {
        public QUIC_LISTENER_EVENT_TYPE Type;
        public NEW_CONNECTION_DATA NEW_CONNECTION;
        public STOP_COMPLETE_DATA STOP_COMPLETE;

        public struct NEW_CONNECTION_DATA
        {
            public QUIC_NEW_CONNECTION_INFO Info;
            public QUIC_CONNECTION Connection;
        }
        public struct STOP_COMPLETE_DATA
        {
            public bool AppCloseInProgress;
            public bool RESERVED;
        }
    }

    internal static partial class MSQuicFunc
    {
        public static int MsQuicListenerOpen(QUIC_REGISTRATION RegistrationHandle, QUIC_LISTENER_CALLBACK Handler, object Context, out QUIC_LISTENER NewListener)
        {
            int Status;
            QUIC_REGISTRATION Registration;
            QUIC_LISTENER Listener = NewListener = null;
            if (RegistrationHandle == null || Handler == null)
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
            CxPlatEventInitialize(out Listener.StopEvent, true, true);

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

        public static int MsQuicListenerStart(QUIC_LISTENER Listener, QUIC_BUFFER[] AlpnBuffers, int AlpnBufferCount, QUIC_ADDR LocalAddress)
        {
            int Status;
            if (LocalAddress != null && !QuicAddrIsValid(LocalAddress))
            {
                Status = QUIC_STATUS_INVALID_PARAMETER;
                goto Exit;
            }

            if (!Listener.Stopped)
            {
                Status = QUIC_STATUS_INVALID_STATE;
                goto Exit;
            }

            if (AlpnBuffers == null || AlpnBufferCount == 0)
            {
                Status = QUIC_STATUS_INVALID_PARAMETER;
                goto Exit;
            }

            int AlpnListCombineLength = 0;
            for (int i = 0; i < AlpnBufferCount; ++i)
            {
                if (AlpnBuffers[i].Length == 0 || AlpnBuffers[i].Length > QUIC_MAX_ALPN_LENGTH)
                {
                    Status = QUIC_STATUS_INVALID_PARAMETER;
                    goto Exit;
                }

                AlpnListCombineLength += sizeof(byte) + AlpnBuffers[i].Length;
            }

            if (AlpnListCombineLength > ushort.MaxValue)
            {
                Status = QUIC_STATUS_INVALID_PARAMETER;
                goto Exit;
            }

            QUIC_SSBuffer AlpnList = new byte[AlpnListCombineLength];
            Span<byte> AlpnListSpan = AlpnList.GetSpan();
            for (int i = 0; i < AlpnBufferCount; ++i)
            {
                int nBufferLength = AlpnBuffers[i].Length;
                AlpnListSpan[0] = (byte)nBufferLength;
                AlpnListSpan = AlpnListSpan.Slice(1);
                AlpnBuffers[i].GetSpan().CopyTo(AlpnListSpan);
                AlpnListSpan = AlpnListSpan.Slice(nBufferLength);
            }
            Listener.AlpnList = AlpnList;
            
            bool PortUnspecified = false;
            if (LocalAddress != null)
            {
                Listener.LocalAddress.CopyFrom(LocalAddress);
                Listener.WildCard = QuicAddrIsWildCard(LocalAddress);
                PortUnspecified = QuicAddrGetPort(LocalAddress) == 0;
            }
            else
            {
                Listener.LocalAddress.Reset();
                Listener.WildCard = true;
                PortUnspecified = true;
            }

            if (!QuicLibraryOnListenerRegistered(Listener))
            {
                Status = QUIC_STATUS_OUT_OF_MEMORY;
                goto Error;
            }

            CXPLAT_UDP_CONFIG UdpConfig = new CXPLAT_UDP_CONFIG();
            UdpConfig.LocalAddress = LocalAddress;
            UdpConfig.RemoteAddress = null;
            UdpConfig.Flags = CXPLAT_SOCKET_FLAG_SHARE | CXPLAT_SOCKET_SERVER_OWNED; // Listeners always share the binding.
            UdpConfig.InterfaceIndex = 0;
            UdpConfig.CibirIdLength = Listener.CibirId[0];
            UdpConfig.CibirIdOffsetSrc = MsQuicLib.CidServerIdLength + 2;
            UdpConfig.CibirIdOffsetDst = MsQuicLib.CidServerIdLength + 2;

            if (UdpConfig.CibirIdLength > 0)
            {
                NetLog.Assert(UdpConfig.CibirIdLength <= UdpConfig.CibirId.Length);
                Array.Copy(Listener.CibirId, 2, UdpConfig.CibirId, 0, UdpConfig.CibirIdLength);
            }

            NetLog.Assert(Listener.Binding == null);
            Status = QuicLibraryGetBinding(UdpConfig, out Listener.Binding);
            if (QUIC_FAILED(Status))
            {
                goto Error;
            }

            Listener.Stopped = false;
            CxPlatEventReset(Listener.StopEvent);
            CxPlatRefInitialize(ref Listener.RefCount);

            Status = QuicBindingRegisterListener(Listener.Binding, Listener);
            if (QUIC_FAILED(Status))
            {
                QuicListenerRelease(Listener, false);
                goto Error;
            }

            if (PortUnspecified)
            {
                QuicBindingGetLocalAddress(Listener.Binding, out LocalAddress);
                QuicAddrSetPort(Listener.LocalAddress, QuicAddrGetPort(LocalAddress));
            }
        Error:
            if (QUIC_FAILED(Status))
            {
                if (Listener.Binding != null)
                {
                    QuicLibraryReleaseBinding(Listener.Binding);
                    Listener.Binding = null;
                }

                if (Listener.AlpnList != null)
                {
                    Listener.AlpnList = null;
                }
            }
        Exit:
            return Status;
        }

        static void QuicListenerStopComplete(QUIC_LISTENER Listener, bool IndicateEvent)
        {
            if (Listener.AlpnList != null)
            {
                Listener.AlpnList = null;
            }

            NetLog.LogError("QuicListenerStopComplete");
            if (IndicateEvent)
            {
                QUIC_LISTENER_EVENT Event = new QUIC_LISTENER_EVENT();
                Event.Type = QUIC_LISTENER_EVENT_TYPE.QUIC_LISTENER_EVENT_STOP_COMPLETE;
                Event.STOP_COMPLETE.AppCloseInProgress = Listener.AppClosed;

                Listener.StopCompleteThreadID = CxPlatCurThreadID();
                QuicListenerIndicateEvent(Listener, ref Event);
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
        
        static void QuicListenerRelease(QUIC_LISTENER Listener, bool IndicateEvent)
        {
            if (CxPlatRefDecrement(ref Listener.RefCount))
            {
                QuicListenerStopComplete(Listener, IndicateEvent);
            }
        }

        static void QuicListenerStopAsync(QUIC_LISTENER Listener)
        {
            if (Listener.Binding != null)
            {
                QuicBindingUnregisterListener(Listener.Binding, Listener);
                QuicLibraryReleaseBinding(Listener.Binding);
                Listener.Binding = null;
                QuicListenerRelease(Listener, true);
            }
        }

        public static void MsQuicListenerStop(QUIC_HANDLE Handle)
        {
            if (Handle != null && Handle.Type ==  QUIC_HANDLE_TYPE.QUIC_HANDLE_TYPE_LISTENER)
            {
                QUIC_LISTENER Listener = (QUIC_LISTENER)Handle;
                QuicListenerStopAsync(Listener);
            }
        }

        static int QuicListenerIndicateEvent(QUIC_LISTENER Listener, ref QUIC_LISTENER_EVENT Event)
        {
            NetLog.Assert(Listener.ClientCallbackHandler != null);
            return Listener.ClientCallbackHandler(Listener, Listener.ClientContext, ref Event);
        }

        static QUIC_SSBuffer QuicListenerFindAlpnInList(QUIC_LISTENER Listener, QUIC_ALPN_BUFFER OtherAlpnList)
        {
            QUIC_SSBuffer AlpnList = Listener.AlpnList;
            while (AlpnList.Length != 0)
            {
                NetLog.Assert(AlpnList[0] + 1 <= AlpnList.Length);
                QUIC_SSBuffer Result = CxPlatTlsAlpnFindInList(OtherAlpnList, AlpnList.Slice(1, AlpnList[0]));
                if (!Result.IsEmpty)
                {
                    return AlpnList;
                }
                AlpnList += (AlpnList[0] + 1);
            }
            return QUIC_SSBuffer.Empty;
        }

        static bool QuicListenerHasAlpnOverlap(QUIC_LISTENER Listener1, QUIC_LISTENER Listener2)
        {
            return QuicListenerFindAlpnInList(Listener1, Listener2.AlpnList) != QUIC_SSBuffer.Empty;
        }

        static bool QuicListenerMatchesAlpn(QUIC_LISTENER Listener, QUIC_NEW_CONNECTION_INFO Info)
        {
            QUIC_SSBuffer Alpn = QuicListenerFindAlpnInList(Listener, Info.ClientAlpnList);
            if (Alpn != QUIC_SSBuffer.Empty)
            {
                int nLength = Alpn[0];
                Info.NegotiatedAlpn = Alpn.Slice(1);
                Info.NegotiatedAlpn.Length = nLength;
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
            Event.NEW_CONNECTION.Connection = Connection;

            int Status = QuicListenerIndicateEvent(Listener, ref Event);
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
                QuicConnTransportError(Connection, QUIC_ERROR_CONNECTION_REFUSED, 
                    $"连接被拒绝，负载太大: {Connection.Worker.Partition.Index}, {Connection.Worker.PartitionIndex}," +
                    $"{Connection.Worker.AverageQueueDelay} {MsQuicLib.Settings.MaxWorkerQueueDelayUs}");

                Listener.TotalRejectedConnections++;
                QuicPerfCounterIncrement(Connection.Partition, QUIC_PERFORMANCE_COUNTERS.QUIC_PERF_COUNTER_CONN_LOAD_REJECT);
                return;
            }

            if (!QuicConnRegister(Connection, Listener.Registration))
            {
                return;
            }
            
            Listener.CibirId.AsSpan().CopyTo(Connection.CibirId);
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
                QuicPerfCounterIncrement(Connection.Partition, QUIC_PERFORMANCE_COUNTERS.QUIC_PERF_COUNTER_CONN_APP_REJECT);
                return;
            }
            Listener.TotalAcceptedConnections++;
        }

    }
}
