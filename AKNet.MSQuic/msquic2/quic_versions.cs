namespace MSQuic2
{
    internal static partial class MSQuicFunc
    {
        public const uint QUIC_VERSION_VER_NEG = 0x00000000U;  //这个表明版本需要协商
        public const uint QUIC_VERSION_2 = 0xcf43336bU;    // Second official version
        public const uint QUIC_VERSION_1 = 0x01000000U;    // First official version
        
        public const uint QUIC_VERSION_RESERVED = 0x0a0a0a0aU;
        public const uint QUIC_VERSION_RESERVED_MASK = 0x0f0f0f0fU;
        public const uint QUIC_VERSION_LATEST = QUIC_VERSION_2;

        static bool QuicIsVersionSupported(uint Version) // Network Byte Order
        {
            switch (Version)
            {
                case QUIC_VERSION_1:
                case QUIC_VERSION_2:
                    return true;
                default:
                    return false;
            }
        }

        static bool QuicIsVersionReserved(uint Version)
        {
            return (Version & QUIC_VERSION_RESERVED_MASK) == QUIC_VERSION_RESERVED;
        }

        static bool QuicVersionNegotiationExtIsVersionServerSupported(uint Version)
        {
            if (MsQuicLib.Settings.VersionSettings != null)
            {
                if (QuicIsVersionReserved(Version))
                {
                    return false;
                }

                for (int i = 0; i < MsQuicLib.Settings.VersionSettings.AcceptableVersions.Length; ++i)
                {
                    if (MsQuicLib.Settings.VersionSettings.AcceptableVersions[i] == Version)
                    {
                        return true;
                    }
                }
            }
            else
            {
                return QuicIsVersionSupported(Version);
            }
            return false;
        }
    }
}
