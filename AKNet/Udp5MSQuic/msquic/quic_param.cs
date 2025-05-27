using System;
namespace AKNet.Udp5MSQuic.Common
{
    internal static partial class MSQuicFunc
    {
        static ulong QuicConnParamSet(QUIC_CONNECTION Connection, uint Param, QUIC_SSBuffer Buffer)
        {
            ulong Status;
            QUIC_SETTINGS InternalSettings = new QUIC_SETTINGS();

            switch (Param) 
            {

            case QUIC_PARAM_CONN_LOCAL_ADDRESS: {

                if (BufferLength != sizeof(QUIC_ADDR)) {
                    Status = QUIC_STATUS_INVALID_PARAMETER;
                    break;
                }

                if (Connection->State.ClosedLocally || QuicConnIsServer(Connection)) {
                    Status = QUIC_STATUS_INVALID_STATE;
                    break;
                }

        if (Connection->State.Started &&
            !Connection->State.HandshakeConfirmed)
        {
            Status = QUIC_STATUS_INVALID_STATE;
            break;
        }

        const QUIC_ADDR* LocalAddress = (const QUIC_ADDR*)Buffer;

        if (!QuicAddrIsValid(LocalAddress))
        {
            Status = QUIC_STATUS_INVALID_PARAMETER;
            break;
        }

        Connection->State.LocalAddressSet = TRUE;
        CxPlatCopyMemory(&Connection->Paths[0].Route.LocalAddress, Buffer, sizeof(QUIC_ADDR));
        QuicTraceEvent(
            ConnLocalAddrAdded,
            "[conn][%p] New Local IP: %!ADDR!",
            Connection,
            CASTED_CLOG_BYTEARRAY(sizeof(Connection->Paths[0].Route.LocalAddress), &Connection->Paths[0].Route.LocalAddress));

        if (Connection->State.Started)
        {

            CXPLAT_DBG_ASSERT(Connection->Paths[0].Binding);
            CXPLAT_DBG_ASSERT(Connection->State.RemoteAddressSet);
            CXPLAT_DBG_ASSERT(Connection->Configuration != NULL);

            QUIC_BINDING* OldBinding = Connection->Paths[0].Binding;

            CXPLAT_UDP_CONFIG UdpConfig = { 0 };
            UdpConfig.LocalAddress = LocalAddress;
            UdpConfig.RemoteAddress = &Connection->Paths[0].Route.RemoteAddress;
            UdpConfig.Flags = Connection->State.ShareBinding ? CXPLAT_SOCKET_FLAG_SHARE : 0;
            UdpConfig.InterfaceIndex = 0;
        # ifdef QUIC_COMPARTMENT_ID
            UdpConfig.CompartmentId = Connection->Configuration->CompartmentId;
        #endif
        # ifdef QUIC_OWNING_PROCESS
            UdpConfig.OwningProcess = Connection->Configuration->OwningProcess;
        #endif
            Status =
                QuicLibraryGetBinding(
                    &UdpConfig,
                    &Connection->Paths[0].Binding);
            if (QUIC_FAILED(Status))
            {
                Connection->Paths[0].Binding = OldBinding;
                break;
            }
            Connection->Paths[0].Route.Queue = NULL;

            //
            // TODO - Need to free any queued recv packets from old binding.
            //

            QuicBindingMoveSourceConnectionIDs(
                OldBinding, Connection->Paths[0].Binding, Connection);
            QuicLibraryReleaseBinding(OldBinding);

            QuicTraceEvent(
                ConnLocalAddrRemoved,
                "[conn][%p] Removed Local IP: %!ADDR!",
                Connection,
                CASTED_CLOG_BYTEARRAY(sizeof(Connection->Paths[0].Route.LocalAddress), &Connection->Paths[0].Route.LocalAddress));

            QuicBindingGetLocalAddress(
                Connection->Paths[0].Binding,
                &Connection->Paths[0].Route.LocalAddress);

            QuicTraceEvent(
                ConnLocalAddrAdded,
                "[conn][%p] New Local IP: %!ADDR!",
                Connection,
                CASTED_CLOG_BYTEARRAY(sizeof(Connection->Paths[0].Route.LocalAddress), &Connection->Paths[0].Route.LocalAddress));

            QuicSendSetSendFlag(&Connection->Send, QUIC_CONN_SEND_FLAG_PING);
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

            if (BufferLength != sizeof(QUIC_ADDR) ||
                QuicAddrIsWildCard((QUIC_ADDR*)Buffer) ||
                QuicConnIsServer(Connection))
            {
                Status = QUIC_STATUS_INVALID_PARAMETER;
                break;
            }

            Connection->State.RemoteAddressSet = TRUE;
            CxPlatCopyMemory(&Connection->Paths[0].Route.RemoteAddress, Buffer, sizeof(QUIC_ADDR));
            //
            // Don't log new Remote address added here because it is logged when
            // the connection is started.
            //

            Status = QUIC_STATUS_SUCCESS;
            break;

        case QUIC_PARAM_CONN_SETTINGS:

            if (Buffer == NULL)
            {
                Status = QUIC_STATUS_INVALID_PARAMETER;
                break;
            }

            Status =
                QuicSettingsSettingsToInternal(
                    BufferLength,
                    (QUIC_SETTINGS*)Buffer,
                    &InternalSettings);
            if (QUIC_FAILED(Status))
            {
                break;
            }

            if (!QuicConnApplyNewSettings(
                    Connection,
                    TRUE,
                    &InternalSettings))
            {
                Status = QUIC_STATUS_INVALID_PARAMETER;
                break;
            }

            break;

        case QUIC_PARAM_CONN_VERSION_SETTINGS:

            if (Buffer == NULL)
            {
                Status = QUIC_STATUS_INVALID_PARAMETER;
                break;
            }

            Status =
                QuicSettingsVersionSettingsToInternal(
                    BufferLength,
                    (QUIC_VERSION_SETTINGS*)Buffer,
                    &InternalSettings);
            if (QUIC_FAILED(Status))
            {
                break;
            }

            if (!QuicConnApplyNewSettings(
                    Connection,
                    TRUE,
                    &InternalSettings))
            {
                QuicSettingsCleanup(&InternalSettings);
                Status = QUIC_STATUS_INVALID_PARAMETER;
                break;
            }
            QuicSettingsCleanup(&InternalSettings);

            break;

        case QUIC_PARAM_CONN_SHARE_UDP_BINDING:

            if (BufferLength != sizeof(uint8_t))
            {
                Status = QUIC_STATUS_INVALID_PARAMETER;
                break;
            }

            if (QUIC_CONN_BAD_START_STATE(Connection) ||
                QuicConnIsServer(Connection))
            {
                Status = QUIC_STATUS_INVALID_STATE;
                break;
            }

            Connection->State.ShareBinding = *(uint8_t*)Buffer;

            QuicTraceLogConnInfo(
                UpdateShareBinding,
                Connection,
                "Updated ShareBinding = %hhu",
                Connection->State.ShareBinding);

            Status = QUIC_STATUS_SUCCESS;
            break;

        case QUIC_PARAM_CONN_CLOSE_REASON_PHRASE:

            if (BufferLength > QUIC_MAX_CONN_CLOSE_REASON_LENGTH)
            {
                Status = QUIC_STATUS_INVALID_PARAMETER;
                break;
            }

            //
            // Require the reason to be null terminated.
            //
            if (Buffer && ((char*)Buffer)[BufferLength - 1] != 0)
            {
                Status = QUIC_STATUS_INVALID_PARAMETER;
                break;
            }

            //
            // Free any old data.
            //
            if (Connection->CloseReasonPhrase != NULL)
            {
                CXPLAT_FREE(Connection->CloseReasonPhrase, QUIC_POOL_CLOSE_REASON);
            }

            //
            // Allocate new space.
            //
            Connection->CloseReasonPhrase =
                CXPLAT_ALLOC_NONPAGED(BufferLength, QUIC_POOL_CLOSE_REASON);

            if (Buffer && Connection->CloseReasonPhrase != NULL)
            {
                CxPlatCopyMemory(
                    Connection->CloseReasonPhrase,
                    Buffer,
                    BufferLength);
                Status = QUIC_STATUS_SUCCESS;
            }
            else
            {
                Status = QUIC_STATUS_OUT_OF_MEMORY;
            }

            break;

        case QUIC_PARAM_CONN_STREAM_SCHEDULING_SCHEME:
            {

                if (BufferLength != sizeof(QUIC_STREAM_SCHEDULING_SCHEME))
                {
                    Status = QUIC_STATUS_INVALID_PARAMETER;
                    break;
                }

                QUIC_STREAM_SCHEDULING_SCHEME Scheme =
                    *(QUIC_STREAM_SCHEDULING_SCHEME*)Buffer;

                if (Scheme >= QUIC_STREAM_SCHEDULING_SCHEME_COUNT)
                {
                    Status = QUIC_STATUS_INVALID_PARAMETER;
                    break;
                }

                Connection->State.UseRoundRobinStreamScheduling =
                    Scheme == QUIC_STREAM_SCHEDULING_SCHEME_ROUND_ROBIN;

                QuicTraceLogConnInfo(
                    UpdateStreamSchedulingScheme,
                    Connection,
                    "Updated Stream Scheduling Scheme = %u",
                    (uint32_t)Scheme);

                Status = QUIC_STATUS_SUCCESS;
                break;
            }

        case QUIC_PARAM_CONN_DATAGRAM_RECEIVE_ENABLED:

            if (BufferLength != sizeof(BOOLEAN))
            {
                Status = QUIC_STATUS_INVALID_PARAMETER;
                break;
            }

            if (QUIC_CONN_BAD_START_STATE(Connection))
            {
                Status = QUIC_STATUS_INVALID_STATE;
                break;
            }

            Connection->Settings.DatagramReceiveEnabled = *(BOOLEAN*)Buffer;
            Connection->Settings.IsSet.DatagramReceiveEnabled = TRUE;
            Status = QUIC_STATUS_SUCCESS;

            QuicTraceLogConnVerbose(
                DatagramReceiveEnableUpdated,
                Connection,
                "Updated datagram receive enabled to %hhu",
                Connection->Settings.DatagramReceiveEnabled);

            break;

        case QUIC_PARAM_CONN_DISABLE_1RTT_ENCRYPTION:

            if (BufferLength != sizeof(BOOLEAN))
            {
                Status = QUIC_STATUS_INVALID_PARAMETER;
                break;
            }

            if (QUIC_CONN_BAD_START_STATE(Connection))
            {
                Status = QUIC_STATUS_INVALID_STATE;
                break;
            }

            if (Connection->State.PeerTransportParameterValid &&
                (!(Connection->PeerTransportParams.Flags & QUIC_TP_FLAG_DISABLE_1RTT_ENCRYPTION)))
            {
                //
                // The peer did't negotiate the feature.
                //
                Status = QUIC_STATUS_INVALID_STATE;
                break;
            }

            Connection->State.Disable1RttEncrytion = *(BOOLEAN*)Buffer;
            Status = QUIC_STATUS_SUCCESS;

            QuicTraceLogConnVerbose(
                Disable1RttEncrytionUpdated,
                Connection,
                "Updated disable 1-RTT encrytption to %hhu",
                Connection->State.Disable1RttEncrytion);

            break;

        case QUIC_PARAM_CONN_RESUMPTION_TICKET:
            {
                if (BufferLength == 0 || Buffer == NULL)
                {
                    Status = QUIC_STATUS_INVALID_PARAMETER;
                    break;
                }
                //
                // Must be set before the client connection is started.
                //

                if (QuicConnIsServer(Connection) ||
                    QUIC_CONN_BAD_START_STATE(Connection))
                {
                    Status = QUIC_STATUS_INVALID_STATE;
                    break;
                }

                Status =
                    QuicCryptoDecodeClientTicket(
                        Connection,
                        (uint16_t)BufferLength,
                        Buffer,
                        &Connection->PeerTransportParams,
                        &Connection->Crypto.ResumptionTicket,
                        &Connection->Crypto.ResumptionTicketLength,
                        &Connection->Stats.QuicVersion);
                if (QUIC_FAILED(Status))
                {
                    break;
                }

                QuicConnOnQuicVersionSet(Connection);
                Status = QuicConnProcessPeerTransportParameters(Connection, TRUE);
                CXPLAT_DBG_ASSERT(QUIC_SUCCEEDED(Status));

                break;
            }

        case QUIC_PARAM_CONN_PEER_CERTIFICATE_VALID:
            if (BufferLength != sizeof(BOOLEAN) || Buffer == NULL)
            {
                Status = QUIC_STATUS_INVALID_PARAMETER;
                break;
            }

            QuicCryptoCustomCertValidationComplete(
                &Connection->Crypto,
                *(BOOLEAN*)Buffer,
                QUIC_TLS_ALERT_CODE_BAD_CERTIFICATE);
            Status = QUIC_STATUS_SUCCESS;
            break;

        case QUIC_PARAM_CONN_LOCAL_INTERFACE:

            if (BufferLength != sizeof(uint32_t))
            {
                Status = QUIC_STATUS_INVALID_PARAMETER;
                break;
            }

            if (QuicConnIsServer(Connection) ||
                QUIC_CONN_BAD_START_STATE(Connection))
            {
                Status = QUIC_STATUS_INVALID_STATE;
                break;
            }

            Connection->State.LocalInterfaceSet = TRUE;
            Connection->Paths[0].Route.LocalAddress.Ipv6.sin6_scope_id = *(uint32_t*)Buffer;

            QuicTraceLogConnInfo(
                LocalInterfaceSet,
                Connection,
                "Local interface set to %u",
                Connection->Paths[0].Route.LocalAddress.Ipv6.sin6_scope_id);

            Status = QUIC_STATUS_SUCCESS;
            break;

        case QUIC_PARAM_CONN_TLS_SECRETS:

            if (BufferLength != sizeof(QUIC_TLS_SECRETS) || Buffer == NULL)
            {
                Status = QUIC_STATUS_INVALID_PARAMETER;
                break;
            }

            if (QUIC_CONN_BAD_START_STATE(Connection))
            {
                Status = QUIC_STATUS_INVALID_STATE;
                break;
            }

            Connection->TlsSecrets = (QUIC_TLS_SECRETS*)Buffer;
            CxPlatZeroMemory(Connection->TlsSecrets, sizeof(*Connection->TlsSecrets));
            Status = QUIC_STATUS_SUCCESS;
            break;

        case QUIC_PARAM_CONN_CIBIR_ID:
            {

                if (QuicConnIsServer(Connection) ||
                    QUIC_CONN_BAD_START_STATE(Connection))
                {
                    return QUIC_STATUS_INVALID_STATE;
                }
                if (!Connection->State.ShareBinding)
                {
                    //
                    // We aren't sharing the binding, and therefore we don't use source
                    // connection IDs, so CIBIR is not supported.
                    //
                    return QUIC_STATUS_INVALID_STATE;
                }

                if (BufferLength > QUIC_MAX_CIBIR_LENGTH + 1)
                {
                    return QUIC_STATUS_INVALID_PARAMETER;
                }
                if (BufferLength == 0)
                {
                    CxPlatZeroMemory(Connection->CibirId, sizeof(Connection->CibirId));
                    return QUIC_STATUS_SUCCESS;
                }
                if (BufferLength < 2)
                { // Must have at least the offset and 1 byte of payload.
                    return QUIC_STATUS_INVALID_PARAMETER;
                }

                if (((uint8_t*)Buffer)[0] != 0)
                {
                    return QUIC_STATUS_NOT_SUPPORTED; // Not yet supproted.
                }

                Connection->CibirId[0] = (uint8_t)BufferLength - 1;
                memcpy(Connection->CibirId + 1, Buffer, BufferLength);

                QuicTraceLogConnInfo(
                    CibirIdSet,
                    Connection,
                    "CIBIR ID set (len %hhu, offset %hhu)",
                    Connection->CibirId[0],
                    Connection->CibirId[1]);

                return QUIC_STATUS_SUCCESS;
            }

        //
        // Private
        //

        case QUIC_PARAM_CONN_FORCE_KEY_UPDATE:

            if (!Connection->State.Connected ||
                Connection->Packets[QUIC_ENCRYPT_LEVEL_1_RTT] == NULL ||
                Connection->Packets[QUIC_ENCRYPT_LEVEL_1_RTT]->AwaitingKeyPhaseConfirmation ||
                !Connection->State.HandshakeConfirmed)
            {
                Status = QUIC_STATUS_INVALID_STATE;
                break;
            }

            QuicTraceLogConnVerbose(
                ForceKeyUpdate,
                Connection,
                "Forcing key update");

            Status = QuicCryptoGenerateNewKeys(Connection);
            if (QUIC_FAILED(Status))
            {
                QuicTraceEvent(
                    ConnErrorStatus,
                    "[conn][%p] ERROR, %u, %s.",
                    Connection,
                    Status,
                    "Forced key update");
                break;
            }

            QuicCryptoUpdateKeyPhase(Connection, TRUE);
            Status = QUIC_STATUS_SUCCESS;
            break;

        case QUIC_PARAM_CONN_FORCE_CID_UPDATE:

            if (!Connection->State.Connected ||
                !Connection->State.HandshakeConfirmed)
            {
                Status = QUIC_STATUS_INVALID_STATE;
                break;
            }

            QuicTraceLogConnVerbose(
                ForceCidUpdate,
                Connection,
                "Forcing destination CID update");

            if (!QuicConnRetireCurrentDestCid(Connection, &Connection->Paths[0]))
            {
                Status = QUIC_STATUS_INVALID_STATE;
                break;
            }

            Connection->Paths[0].InitiatedCidUpdate = TRUE;
            Status = QUIC_STATUS_SUCCESS;
            break;

        case QUIC_PARAM_CONN_TEST_TRANSPORT_PARAMETER:

            if (BufferLength != sizeof(QUIC_PRIVATE_TRANSPORT_PARAMETER))
            {
                Status = QUIC_STATUS_INVALID_PARAMETER;
                break;
            }

            if (QUIC_CONN_BAD_START_STATE(Connection))
            {
                Status = QUIC_STATUS_INVALID_STATE;
                break;
            }

            CxPlatCopyMemory(
                &Connection->TestTransportParameter, Buffer, BufferLength);
            Connection->State.TestTransportParameterSet = TRUE;

            QuicTraceLogConnVerbose(
                TestTPSet,
                Connection,
                "Setting Test Transport Parameter (type %x, %hu bytes)",
                Connection->TestTransportParameter.Type,
                Connection->TestTransportParameter.Length);

            Status = QUIC_STATUS_SUCCESS;
            break;

        case QUIC_PARAM_CONN_KEEP_ALIVE_PADDING:

            if (BufferLength != sizeof(Connection->KeepAlivePadding)) {
                Status = QUIC_STATUS_INVALID_PARAMETER;
                break;
            }

            Connection->KeepAlivePadding = *(uint16_t*)Buffer;
            Status = QUIC_STATUS_SUCCESS;
            break;

        #if QUIC_TEST_DISABLE_VNE_TP_GENERATION
            case QUIC_PARAM_CONN_DISABLE_VNE_TP_GENERATION:

                if (BufferLength != sizeof(BOOLEAN) || Buffer == NULL) {
                    Status = QUIC_STATUS_INVALID_PARAMETER;
                    break;
                }

                Connection->State.DisableVneTp = *(BOOLEAN*)Buffer;
                Status = QUIC_STATUS_SUCCESS;
                break;
        #endif

        default:
            Status = QUIC_STATUS_INVALID_PARAMETER;
            break;
        }

        return Status;
        }
    }
}
