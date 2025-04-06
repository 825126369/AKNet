using System;

namespace AKNet.Udp5Quic.Common
{
    internal static partial class MSQuicFunc
    {
        public const uint QUIC_VERSION_VER_NEG = 0x00000000U;     // Version for 'Version Negotiation'
        public const uint QUIC_VERSION_2 = 0xcf43336bU;    // Second official version
        public const uint QUIC_VERSION_1 = 0x01000000U;    // First official version
        public const uint QUIC_VERSION_MS_1 = 0x0000cdabU;    // First Microsoft version (currently same as latest draft)
        public const uint QUIC_VERSION_DRAFT_29 = 0x1d0000ffU;   // IETF draft 29
        public const uint QUIC_VERSION_VER_NEG_H = 0x00000000U;  // Version for 'Version Negotiation'
        public const uint QUIC_VERSION_2_H = 0x6b3343cfU;    // Second official version
        public const uint QUIC_VERSION_1_H = 0x00000001U;    // First official version
        public const uint QUIC_VERSION_1_MS_H = 0xabcd0000U;    // First Microsoft version (-1412628480 in decimal)
        
        public const uint QUIC_VERSION_DRAFT_29_H = 0xff00001dU;    // IETF draft 29
        public const uint QUIC_VERSION_RESERVED = 0x0a0a0a0aU;
        public const uint QUIC_VERSION_RESERVED_MASK = 0x0f0f0f0fU;
        public const uint QUIC_VERSION_LATEST = QUIC_VERSION_1;
        public const uint QUIC_VERSION_LATEST_H = QUIC_VERSION_1_H;

        static readonly uint[] DefaultSupportedVersionsList = new uint[4]
        {
            QUIC_VERSION_2,
            QUIC_VERSION_1,
            QUIC_VERSION_MS_1,
            QUIC_VERSION_DRAFT_29,
        };

        static bool QuicIsVersionSupported(uint Version) // Network Byte Order
        {
            switch (Version)
            {
                case QUIC_VERSION_1:
                case QUIC_VERSION_DRAFT_29:
                case QUIC_VERSION_MS_1:
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
