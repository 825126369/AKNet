namespace AKNet.Udp5Quic.Common
{
    internal class QUIC_CONFIGURATION : QUIC_HANDLE
    {
        public QUIC_REGISTRATION Registration;
        public CXPLAT_LIST_ENTRY Link;
        public long RefCount;
        //public CXPLAT_SEC_CONFIG SecurityConfig;
        public uint CompartmentId;
        CXPLAT_STORAGE AppSpecificStorage;
        QUIC_SETTINGS_INTERNAL Settings;
        public ushort AlpnListLength;
        public byte[] AlpnList = new byte[0];
    }
}
