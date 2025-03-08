namespace AKNet.Udp5Quic.Common
{
    internal class QUIC_LISTENER
    {
        public bool WildCard;
        public bool AppClosed;
        public bool Stopped;
        public bool NeedsCleanup;
        CXPLAT_THREAD_ID StopCompleteThreadID;
        CXPLAT_LIST_ENTRY Link;
        QUIC_REGISTRATION* Registration;
        CXPLAT_LIST_ENTRY RegistrationLink;
        CXPLAT_REF_COUNT RefCount;
        CXPLAT_EVENT StopEvent;
        QUIC_ADDR LocalAddress;
        QUIC_BINDING* Binding;
        QUIC_LISTENER_CALLBACK_HANDLER ClientCallbackHandler;
        uint64_t TotalAcceptedConnections;
        uint64_t TotalRejectedConnections;
        uint16_t AlpnListLength;
        uint8_t* AlpnList;
        public byte[] CibirId = new byte[2 + MSQuicFunc.QUIC_MAX_CIBIR_LENGTH];
    }
}
