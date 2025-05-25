using System;

namespace AKNet.MSQuicWrapper;

public partial struct QUIC_API_TABLE
{
    [NativeTypeName("QUIC_SET_CONTEXT_FN")]
    public IntPtr SetContext;

    [NativeTypeName("QUIC_GET_CONTEXT_FN")]
    public IntPtr GetContext;

    [NativeTypeName("QUIC_SET_CALLBACK_HANDLER_FN")]
    public IntPtr SetCallbackHandler;

    [NativeTypeName("QUIC_SET_PARAM_FN")]
    public IntPtr SetParam;

    [NativeTypeName("QUIC_GET_PARAM_FN")]
    public IntPtr GetParam;

    [NativeTypeName("QUIC_REGISTRATION_OPEN_FN")]
    public IntPtr RegistrationOpen;

    [NativeTypeName("QUIC_REGISTRATION_CLOSE_FN")]
    public IntPtr RegistrationClose;

    [NativeTypeName("QUIC_REGISTRATION_SHUTDOWN_FN")]
    public IntPtr RegistrationShutdown;

    [NativeTypeName("QUIC_CONFIGURATION_OPEN_FN")]
    public IntPtr ConfigurationOpen;

    [NativeTypeName("QUIC_CONFIGURATION_CLOSE_FN")]
    public IntPtr ConfigurationClose;

    [NativeTypeName("QUIC_CONFIGURATION_LOAD_CREDENTIAL_FN")]
    public IntPtr ConfigurationLoadCredential;

    [NativeTypeName("QUIC_LISTENER_OPEN_FN")]
    public IntPtr ListenerOpen;

    [NativeTypeName("QUIC_LISTENER_CLOSE_FN")]
    public IntPtr ListenerClose;

    [NativeTypeName("QUIC_LISTENER_START_FN")]
    public IntPtr ListenerStart;

    [NativeTypeName("QUIC_LISTENER_STOP_FN")]
    public IntPtr ListenerStop;

    [NativeTypeName("QUIC_CONNECTION_OPEN_FN")]
    public IntPtr ConnectionOpen;

    [NativeTypeName("QUIC_CONNECTION_CLOSE_FN")]
    public IntPtr ConnectionClose;

    [NativeTypeName("QUIC_CONNECTION_SHUTDOWN_FN")]
    public IntPtr ConnectionShutdown;

    [NativeTypeName("QUIC_CONNECTION_START_FN")]
    public IntPtr ConnectionStart;

    [NativeTypeName("QUIC_CONNECTION_SET_CONFIGURATION_FN")]
    public IntPtr ConnectionSetConfiguration;

    [NativeTypeName("QUIC_CONNECTION_SEND_RESUMPTION_FN")]
    public IntPtr ConnectionSendResumptionTicket;

    [NativeTypeName("QUIC_STREAM_OPEN_FN")]
    public IntPtr StreamOpen;

    [NativeTypeName("QUIC_STREAM_CLOSE_FN")]
    public IntPtr StreamClose;

    [NativeTypeName("QUIC_STREAM_START_FN")]
    public IntPtr StreamStart;

    [NativeTypeName("QUIC_STREAM_SHUTDOWN_FN")]
    public IntPtr StreamShutdown;

    [NativeTypeName("QUIC_STREAM_SEND_FN")]
    public IntPtr StreamSend;

    [NativeTypeName("QUIC_STREAM_RECEIVE_COMPLETE_FN")]
    public IntPtr StreamReceiveComplete;

    [NativeTypeName("QUIC_STREAM_RECEIVE_SET_ENABLED_FN")]
    public IntPtr StreamReceiveSetEnabled;

    [NativeTypeName("QUIC_DATAGRAM_SEND_FN")]
    public IntPtr DatagramSend;

    [NativeTypeName("QUIC_CONNECTION_COMP_RESUMPTION_FN")]
    public IntPtr ConnectionResumptionTicketValidationComplete;

    [NativeTypeName("QUIC_CONNECTION_COMP_CERT_FN")]
    public IntPtr ConnectionCertificateValidationComplete;
}
