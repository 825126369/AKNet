using AKNet.Common;
using System;
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
        public QUIC_ADDR LocalAddress;
        public QUIC_BINDING Binding;
        public QUIC_LISTENER_CALLBACK ClientCallbackHandler;
        public ulong TotalAcceptedConnections;
        public ulong TotalRejectedConnections;

        public byte[] AlpnList = null;
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
        public static ulong MsQuicListenerOpen(QUIC_REGISTRATION RegistrationHandle, QUIC_LISTENER_CALLBACK Handler, object Context, ref QUIC_HANDLE NewListener)
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

        public static ulong MsQuicListenerStart(QUIC_HANDLE Handle, QUIC_BUFFER[] AlpnBuffers, int AlpnBufferCount, QUIC_ADDR LocalAddress)
        {
            ulong Status;
            QUIC_LISTENER Listener;
            byte[] AlpnList;
            int AlpnListLength;
            bool PortUnspecified;
            QUIC_ADDR BindingLocalAddress = null;

            if (Handle == null || Handle.Type != QUIC_HANDLE_TYPE.QUIC_HANDLE_TYPE_LISTENER ||
                AlpnBuffers == null ||
                AlpnBufferCount == 0)
            {
                Status = QUIC_STATUS_INVALID_PARAMETER;
                goto Exit;
            }

            AlpnListLength = 0;
            for (int i = 0; i < AlpnBufferCount; ++i)
            {
                if (AlpnBuffers[i].Length == 0 || AlpnBuffers[i].Length > QUIC_MAX_ALPN_LENGTH)
                {
                    Status = QUIC_STATUS_INVALID_PARAMETER;
                    goto Exit;
                }
                AlpnListLength += sizeof(byte) + AlpnBuffers[i].Length;
            }
            if (AlpnListLength > ushort.MaxValue)
            {
                Status = QUIC_STATUS_INVALID_PARAMETER;
                goto Exit;
            }
            NetLog.Assert(AlpnListLength <= ushort.MaxValue);

            if (LocalAddress != null && !QuicAddrIsValid(LocalAddress))
            {
                Status = QUIC_STATUS_INVALID_PARAMETER;
                goto Exit;
            }

            Listener = (QUIC_LISTENER)Handle;

            if (!Listener.Stopped)
            {
                Status = QUIC_STATUS_INVALID_STATE;
                goto Exit;
            }

            AlpnList = new byte[AlpnListLength];
            if (AlpnList == null)
            {
                Status = QUIC_STATUS_OUT_OF_MEMORY;
                goto Exit;
            }

            Listener.AlpnList = AlpnList;
            Listener.AlpnListLength = (ushort)AlpnListLength;

            int AlpnListOffset = 0;
            for (int i = 0; i < AlpnBufferCount; ++i)
            {
                AlpnList[AlpnListOffset] = (byte)AlpnBuffers[i].Length;
                AlpnListOffset++;
                Array.Copy(AlpnBuffers[i].Buffer, 0, AlpnList, AlpnListOffset, AlpnBuffers[i].Length);
                AlpnListOffset += AlpnBuffers[i].Length;
            }

            if (LocalAddress != null)
            {
                Listener.LocalAddress = LocalAddress;
                Listener.WildCard = QuicAddrIsWildCard(LocalAddress);
                PortUnspecified = QuicAddrGetPort(LocalAddress) == 0;
            }
            else
            {
                Listener.LocalAddress = null;
                Listener.WildCard = true;
                PortUnspecified = true;
            }


            BindingLocalAddress = new QUIC_ADDR();
            BindingLocalAddress.Ip = IPAddress.IPv6Any;
            BindingLocalAddress.nPort = QuicAddrGetPort(LocalAddress);

            if (!QuicLibraryOnListenerRegistered(Listener))
            {
                Status = QUIC_STATUS_OUT_OF_MEMORY;
                goto Error;
            }

            CXPLAT_UDP_CONFIG UdpConfig = new CXPLAT_UDP_CONFIG();
            UdpConfig.LocalAddress = BindingLocalAddress;
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
            Status = QuicLibraryGetBinding(UdpConfig, ref Listener.Binding);
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
                QuicBindingGetLocalAddress(Listener.Binding, ref BindingLocalAddress);
                QuicAddrSetPort(Listener.LocalAddress, QuicAddrGetPort(BindingLocalAddress));
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
                Listener.AlpnListLength = 0;
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

            if (IndicateEvent)
            {
                QUIC_LISTENER_EVENT Event = new QUIC_LISTENER_EVENT();
                Event.Type = QUIC_LISTENER_EVENT_TYPE.QUIC_LISTENER_EVENT_STOP_COMPLETE;
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

        static ulong QuicListenerIndicateEvent(QUIC_LISTENER Listener, QUIC_LISTENER_EVENT Event)
        {
            NetLog.Assert(Listener.ClientCallbackHandler != null);
            return Listener.ClientCallbackHandler(Listener, Listener.ClientContext, Event);
        }

        static QUIC_SSBuffer QuicListenerFindAlpnInList(QUIC_LISTENER Listener, int OtherAlpnListLength, byte[] OtherAlpnList)
        {
            QUIC_SSBuffer AlpnList = Listener.AlpnList;
            int AlpnListLength = Listener.AlpnListLength;
            
            while (AlpnListLength != 0)
            {
                NetLog.Assert(AlpnList[0] + 1 <= AlpnListLength);
                QUIC_SSBuffer Result = CxPlatTlsAlpnFindInList(OtherAlpnListLength, OtherAlpnList, AlpnList[0], AlpnList.Slice(1));
                if (!Result.IsEmpty)
                {
                    return AlpnList;
                }
                AlpnListLength -= AlpnList[0] + 1;
                AlpnList = AlpnList.Slice(AlpnList[0] + 1);
            }
            return QUIC_SSBuffer.Empty;
        }

        static bool QuicListenerHasAlpnOverlap(QUIC_LISTENER Listener1, QUIC_LISTENER Listener2)
        {
            return QuicListenerFindAlpnInList(Listener1, Listener2.AlpnListLength, Listener2.AlpnList) != QUIC_SSBuffer.Empty;
        }

        static bool QuicListenerMatchesAlpn(QUIC_LISTENER Listener, QUIC_NEW_CONNECTION_INFO Info)
        {
            QUIC_SSBuffer Alpn = QuicListenerFindAlpnInList(Listener, Info.ClientAlpnListLength, Info.ClientAlpnList);
            if (Alpn != QUIC_SSBuffer.Empty)
            {
                Info.NegotiatedAlpnLength = Alpn[0]; // The length prefixed to the ALPN buffer.
                Alpn.Slice(1, Alpn[0]).CopyTo(Info.NegotiatedAlpn);
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

        static ulong QuicListenerParamGet(QUIC_LISTENER Listener, uint Param, QUIC_BUFFER Buffer)
        {
            ulong Status = QUIC_STATUS_SUCCESS;
            //switch (Param)
            //{
            //    case QUIC_PARAM_LISTENER_LOCAL_ADDRESS:

            //        if (BufferLength < sizeof(QUIC_ADDR))
            //        {
            //            BufferLength = sizeof(QUIC_ADDR);
            //            Status = QUIC_STATUS_BUFFER_TOO_SMALL;
            //            break;
            //        }

            //        if (Buffer == null)
            //        {
            //            Status = QUIC_STATUS_INVALID_PARAMETER;
            //            break;
            //        }

            //        BufferLength = sizeof(QUIC_ADDR);
            //        Listener.LocalAddress.WriteTo(Buffer);
            //        Status = QUIC_STATUS_SUCCESS;
            //        break;

            //    case QUIC_PARAM_LISTENER_STATS:

            //        if (BufferLength < sizeof(QUIC_LISTENER_STATISTICS))
            //        {
            //            BufferLength = sizeof(QUIC_LISTENER_STATISTICS);
            //            Status = QUIC_STATUS_BUFFER_TOO_SMALL;
            //            break;
            //        }

            //        if (Buffer == null)
            //        {
            //            Status = QUIC_STATUS_INVALID_PARAMETER;
            //            break;
            //        }

            //        BufferLength = sizeof(QUIC_LISTENER_STATISTICS);
            //        QUIC_LISTENER_STATISTICS Stats = (QUIC_LISTENER_STATISTICS)Buffer;

            //        Stats.TotalAcceptedConnections = Listener.TotalAcceptedConnections;
            //        Stats.TotalRejectedConnections = Listener.TotalRejectedConnections;

            //        if (Listener.Binding != null)
            //        {
            //            Stats.BindingRecvDroppedPackets = Listener.Binding.Stats.Recv.DroppedPackets;
            //        }
            //        else
            //        {
            //            Stats.BindingRecvDroppedPackets = 0;
            //        }

            //        Status = QUIC_STATUS_SUCCESS;
            //        break;

            //    case QUIC_PARAM_LISTENER_CIBIR_ID:

            //        if (Listener.CibirId[0] == 0)
            //        {
            //            BufferLength = 0;
            //            return QUIC_STATUS_SUCCESS;
            //        }

            //        if (BufferLength < Listener.CibirId[0] + 1)
            //        {
            //            BufferLength = Listener.CibirId[0] + 1;
            //            return QUIC_STATUS_BUFFER_TOO_SMALL;
            //        }

            //        if (Buffer == null)
            //        {
            //            return QUIC_STATUS_INVALID_PARAMETER;
            //        }

            //        BufferLength = Listener.CibirId[0] + 1;
            //        memcpy(Buffer, Listener.CibirId + 1, Listener.CibirId[0]);

            //        Status = QUIC_STATUS_SUCCESS;
            //        break;

            //    default:
            //        Status = QUIC_STATUS_INVALID_PARAMETER;
            //        break;
            //}
            return Status;
        }

    }
}
