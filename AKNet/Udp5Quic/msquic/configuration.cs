namespace AKNet.Udp5Quic.Common
{
    internal class QUIC_CONFIGURATION : QUIC_HANDLE
    {
        public QUIC_REGISTRATION Registration;
        public quic_platform_cxplat_list_entry Link;
        public long RefCount;
        //public CXPLAT_SEC_CONFIG SecurityConfig;
        public uint CompartmentId;
        CXPLAT_STORAGE AppSpecificStorage;
        QUIC_SETTINGS_INTERNAL Settings;
        public ushort AlpnListLength;
        public byte[] AlpnList = new byte[0];
    }
}
