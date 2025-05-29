using AKNet.Common;
using System;
namespace AKNet.Udp5MSQuic.Common
{
    internal static partial class MSQuicFunc
    {
        static ulong QuicLibrarySetGlobalParam(uint Param, ReadOnlySpan<byte> Buffer)
        {
            ulong Status = QUIC_STATUS_SUCCESS;
            QUIC_SETTINGS InternalSettings = new QUIC_SETTINGS();

            switch (Param)
            {
                case QUIC_PARAM_GLOBAL_RETRY_MEMORY_PERCENT:
                    Status = QUIC_STATUS_SUCCESS;
                    break;
                case QUIC_PARAM_GLOBAL_LOAD_BALACING_MODE:
                    break;
                case QUIC_PARAM_GLOBAL_SETTINGS:
                    break;
                case QUIC_PARAM_GLOBAL_GLOBAL_SETTINGS:
                    break;
                case QUIC_PARAM_GLOBAL_VERSION_SETTINGS:
                    break;
                case QUIC_PARAM_GLOBAL_EXECUTION_CONFIG:
                    break;
                case QUIC_PARAM_GLOBAL_STATELESS_RESET_KEY:
                    break;

                default:
                    Status = QUIC_STATUS_INVALID_PARAMETER;
                    break;
            }

            return Status;
        }

        static ulong QuicLibrarySetParam(QUIC_HANDLE Handle, uint Param, ReadOnlySpan<byte> Buffer)
        {
            ulong Status;
            QUIC_REGISTRATION Registration;
            QUIC_CONFIGURATION Configuration;
            QUIC_LISTENER Listener;
            QUIC_CONNECTION Connection;
            QUIC_STREAM Stream;

            switch (Handle.Type)
            {
                case QUIC_HANDLE_TYPE.QUIC_HANDLE_TYPE_REGISTRATION:
                    Stream = null;
                    Connection = null;
                    Listener = null;
                    Configuration = null;
                    Registration = (QUIC_REGISTRATION)Handle;
                    break;
                case QUIC_HANDLE_TYPE.QUIC_HANDLE_TYPE_CONFIGURATION:
                    Stream = null;
                    Connection = null;
                    Listener = null;
                    Configuration = (QUIC_CONFIGURATION)Handle;
                    Registration = Configuration.Registration;
                    break;
                case QUIC_HANDLE_TYPE.QUIC_HANDLE_TYPE_LISTENER:
                    Stream = null;
                    Connection = null;
                    Listener = (QUIC_LISTENER)Handle;
                    Configuration = null;
                    Registration = Listener.Registration;
                    break;

                case QUIC_HANDLE_TYPE.QUIC_HANDLE_TYPE_CONNECTION_CLIENT:
                case QUIC_HANDLE_TYPE.QUIC_HANDLE_TYPE_CONNECTION_SERVER:
                    Stream = null;
                    Listener = null;
                    Connection = (QUIC_CONNECTION)Handle;
                    Configuration = Connection.Configuration;
                    Registration = Connection.Registration;
                    break;

                case QUIC_HANDLE_TYPE.QUIC_HANDLE_TYPE_STREAM:
                    Listener = null;
                    Stream = (QUIC_STREAM)Handle;
                    Connection = Stream.Connection;
                    Configuration = Connection.Configuration;
                    Registration = Connection.Registration;
                    break;

                default:
                    NetLog.Assert(false);
                    Status = QUIC_STATUS_INVALID_PARAMETER;
                    goto Error;
            }

            switch (Param & 0x7F000000)
            {
                case QUIC_PARAM_PREFIX_REGISTRATION:
                    if (Registration == null)
                    {
                        Status = QUIC_STATUS_INVALID_PARAMETER;
                    }
                    else
                    {
                        Status = QuicRegistrationParamSet(Registration, Param, Buffer);
                    }
                    break;

                case QUIC_PARAM_PREFIX_CONFIGURATION:
                    if (Configuration == null)
                    {
                        Status = QUIC_STATUS_INVALID_PARAMETER;
                    }
                    else
                    {
                        Status = QuicConfigurationParamSet(Configuration, Param, Buffer);
                    }
                    break;

                case QUIC_PARAM_PREFIX_LISTENER:
                    if (Listener == null)
                    {
                        Status = QUIC_STATUS_INVALID_PARAMETER;
                    }
                    else
                    {
                        Status = QuicListenerParamSet(Listener, Param, Buffer);
                    }
                    break;

                case QUIC_PARAM_PREFIX_CONNECTION:
                    if (Connection == null)
                    {
                        Status = QUIC_STATUS_INVALID_PARAMETER;
                    }
                    else
                    {
                        Status = QuicConnParamSet(Connection, Param, Buffer);
                    }
                    break;

                case QUIC_PARAM_PREFIX_TLS:
                case QUIC_PARAM_PREFIX_TLS_SCHANNEL:
                    if (Connection == null || Connection.Crypto.TLS == null)
                    {
                        Status = QUIC_STATUS_INVALID_PARAMETER;
                    }
                    else
                    {
                        Status = CxPlatTlsParamSet(Connection.Crypto.TLS, Param, Buffer);
                    }
                    break;

                case QUIC_PARAM_PREFIX_STREAM:
                    if (Stream == null)
                    {
                        Status = QUIC_STATUS_INVALID_PARAMETER;
                    }
                    else
                    {
                        Status = QuicStreamParamSet(Stream, Param, Buffer);
                    }
                    break;

                default:
                    Status = QUIC_STATUS_INVALID_PARAMETER;
                    break;
            }
        Error:
            return Status;
        }

        static ulong QuicRegistrationParamSet(QUIC_REGISTRATION Registration, uint Param, ReadOnlySpan<byte> Buffer)
        {
            return QUIC_STATUS_INVALID_PARAMETER;
        }

        static ulong QuicRegistrationParamGet(QUIC_REGISTRATION Registration, uint Param, QUIC_SSBuffer Buffer)
        {
            return QUIC_STATUS_INVALID_PARAMETER;
        }

        static ulong QuicListenerParamSet(QUIC_LISTENER Listener, uint Param, ReadOnlySpan<byte> Buffer)
        {
            //if (Param == QUIC_PARAM_LISTENER_CIBIR_ID)
            //{
            //    if (Buffer.Length > QUIC_MAX_CIBIR_LENGTH + 1)
            //    {
            //        return QUIC_STATUS_INVALID_PARAMETER;
            //    }

            //    if (Buffer.Length == 0)
            //    {
            //        Array.Clear(Listener.CibirId, 0, Listener.CibirId.Length);
            //        return QUIC_STATUS_SUCCESS;
            //    }

            //    if (Buffer.Length < 2)
            //    {
            //        return QUIC_STATUS_INVALID_PARAMETER;
            //    }

            //    if (Buffer[0] != 0)
            //    {
            //        return QUIC_STATUS_NOT_SUPPORTED; // Not yet supproted.
            //    }

            //    Listener.CibirId[0] = (byte)(Buffer.Length - 1);
            //    Array.Copy(Buffer, 0, Listener.CibirId, 1, Buffer.Length);
            //    return QUIC_STATUS_SUCCESS;
            //}

            return QUIC_STATUS_INVALID_PARAMETER;
        }

        static ulong QuicListenerParamGet(QUIC_LISTENER Listener, uint Param, QUIC_BUFFER Buffer)
        {
            ulong Status = QUIC_STATUS_SUCCESS;
            return Status;
        }

        static ulong QuicConnParamSet(QUIC_CONNECTION Connection, uint Param, ReadOnlySpan<byte> Buffer)
        {
            ulong Status;
            QUIC_SETTINGS InternalSettings = new QUIC_SETTINGS();

            switch (Param)
            {
                case QUIC_PARAM_CONN_LOCAL_ADDRESS:
                    {
                        if (Buffer.Length != QUIC_ADDR.sizeof_QUIC_ADDR)
                        {
                            Status = QUIC_STATUS_INVALID_PARAMETER;
                            break;
                        }

                        if (Connection.State.ClosedLocally || QuicConnIsServer(Connection))
                        {
                            Status = QUIC_STATUS_INVALID_STATE;
                            break;
                        }

                        if (Connection.State.Started && !Connection.State.HandshakeConfirmed)
                        {
                            Status = QUIC_STATUS_INVALID_STATE;
                            break;
                        }

                        QUIC_ADDR LocalAddress = new QUIC_ADDR();
                        LocalAddress.WriteFrom(Buffer);

                        if (!QuicAddrIsValid(LocalAddress))
                        {
                            Status = QUIC_STATUS_INVALID_PARAMETER;
                            break;
                        }

                        Connection.State.LocalAddressSet = true;
                        Connection.Paths[0].Route.LocalAddress.WriteFrom(Buffer);

                        if (Connection.State.Started)
                        {
                            NetLog.Assert(Connection.Paths[0].Binding != null);
                            NetLog.Assert(Connection.State.RemoteAddressSet);
                            NetLog.Assert(Connection.Configuration != null);

                            QUIC_BINDING OldBinding = Connection.Paths[0].Binding;
                            CXPLAT_UDP_CONFIG UdpConfig = new CXPLAT_UDP_CONFIG();
                            UdpConfig.LocalAddress = LocalAddress;
                            UdpConfig.RemoteAddress = Connection.Paths[0].Route.RemoteAddress;
                            UdpConfig.Flags = Connection.State.ShareBinding ? CXPLAT_SOCKET_FLAG_SHARE : 0;
                            UdpConfig.InterfaceIndex = 0;
                            Status = QuicLibraryGetBinding(UdpConfig, ref Connection.Paths[0].Binding);
                            if (QUIC_FAILED(Status))
                            {
                                Connection.Paths[0].Binding = OldBinding;
                                break;
                            }

                            Connection.Paths[0].Route.Queue = null;
                            QuicBindingMoveSourceConnectionIDs(OldBinding, Connection.Paths[0].Binding, Connection);
                            QuicLibraryReleaseBinding(OldBinding);
                            QuicBindingGetLocalAddress(Connection.Paths[0].Binding, out Connection.Paths[0].Route.LocalAddress);
                            QuicSendSetSendFlag(Connection.Send, QUIC_CONN_SEND_FLAG_PING);
                        }

                        Status = QUIC_STATUS_SUCCESS;
                        break;
                    }

                case QUIC_PARAM_CONN_REMOTE_ADDRESS:
                    if (QUIC_CONN_BAD_START_STATE(Connection))
                    {
                        Status = QUIC_STATUS_INVALID_STATE;
                        break;
                    }

                    if (Buffer.Length != QUIC_ADDR.sizeof_QUIC_ADDR || QuicAddrIsWildCard((QUIC_ADDR)Buffer) || QuicConnIsServer(Connection))
                    {
                        Status = QUIC_STATUS_INVALID_PARAMETER;
                        break;
                    }

                    Connection.State.RemoteAddressSet = true;
                    Connection.Paths[0].Route.RemoteAddress.WriteFrom(Buffer);
                    Status = QUIC_STATUS_SUCCESS;
                    break;

                case QUIC_PARAM_CONN_TLS_SECRETS:
                    if (Buffer.Length != QUIC_TLS_SECRETS.sizeof_QUIC_TLS_SECRETS || Buffer == null)
                    {
                        Status = QUIC_STATUS_INVALID_PARAMETER;
                        break;
                    }

                    if (QUIC_CONN_BAD_START_STATE(Connection))
                    {
                        Status = QUIC_STATUS_INVALID_STATE;
                        break;
                    }

                    Connection.TlsSecrets = (QUIC_TLS_SECRETS)Buffer;
                    Status = QUIC_STATUS_SUCCESS;
                    break;
                default:
                    Status = QUIC_STATUS_INVALID_PARAMETER;
                    break;
            }

            return Status;
        }

        static ulong CxPlatTlsParamSet(CXPLAT_TLS SecConfig, uint Param, ReadOnlySpan<byte> Buffer)
        {
            return QUIC_STATUS_NOT_SUPPORTED;
        }

        static ulong QuicStreamParamSet(QUIC_STREAM Stream, uint Param, ReadOnlySpan<byte> Buffer)
        {
            return QUIC_STATUS_NOT_SUPPORTED;
        }

        static ulong QuicConfigurationParamSet(QUIC_CONFIGURATION Configuration, uint Param, ReadOnlySpan<byte> Buffer)
        {
            return QUIC_STATUS_NOT_SUPPORTED;
        }
    }
}
