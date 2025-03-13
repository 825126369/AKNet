namespace AKNet.Udp5Quic.Common
{
    internal class QUIC_CONFIGURATION : QUIC_HANDLE
    {
        public QUIC_REGISTRATION Registration;
        public CXPLAT_LIST_ENTRY Link;
        public long RefCount;
        public CXPLAT_SEC_CONFIG SecurityConfig;

# ifdef QUIC_COMPARTMENT_ID
        //
        // The network compartment ID.
        //
        QUIC_COMPARTMENT_ID CompartmentId;
#endif

# ifdef QUIC_SILO
        //
        // The silo.
        //
        QUIC_SILO Silo;

        //
        // Handle to persistent storage (registry).
        //
        CXPLAT_STORAGE* Storage; // Only necessary if it could be in a different silo.
#endif

# ifdef QUIC_OWNING_PROCESS
        //
        // The process token of the owning process
        //
        QUIC_PROCESS OwningProcess;
#endif
        CXPLAT_STORAGE* AppSpecificStorage;

        //
        // Configurable (app & registry) settings.
        //
        QUIC_SETTINGS_INTERNAL Settings;

        uint16_t AlpnListLength;
        uint8_t AlpnList[0];

    }
}
