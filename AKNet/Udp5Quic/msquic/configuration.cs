using System;

namespace AKNet.Udp5Quic.Common
{
    internal class QUIC_CONFIGURATION : QUIC_HANDLE
    {
        public QUIC_REGISTRATION Registration;
        public CXPLAT_LIST_ENTRY Link;
        public long RefCount;
        public CXPLAT_SEC_CONFIG SecurityConfig;
        public uint CompartmentId;
        public CXPLAT_STORAGE AppSpecificStorage;
        public QUIC_SETTINGS_INTERNAL Settings;
        public QUIC_BUFFER AlpnList = new QUIC_BUFFER(0);
    }

    internal static partial class MSQuicFunc
    {
        static void QuicConfigurationAddRef(QUIC_CONFIGURATION Configuration)
        {
            CxPlatRefIncrement(ref Configuration.RefCount);
        }

        static ulong QuicConfigurationParamGet(QUIC_CONFIGURATION Configuration, uint Param, QUIC_BUFFER Buffer)
        {
            //if (Param == QUIC_PARAM_CONFIGURATION_SETTINGS)
            //{
            //    return QuicSettingsGetSettings(Configuration.Settings, Buffer.Length, (QUIC_SETTINGS)Buffer);
            //}

            //if (Param == QUIC_PARAM_CONFIGURATION_VERSION_SETTINGS)
            //{
            //    return QuicSettingsGetVersionSettings(Configuration.Settings, Buffer.Length, (QUIC_VERSION_SETTINGS)Buffer);
            //}

            //if (Param == QUIC_PARAM_CONFIGURATION_VERSION_NEG_ENABLED)
            //{
            //    if (Buffer.Length < sizeof(bool))
            //    {
            //        Buffer.Length = sizeof(bool);
            //        return QUIC_STATUS_BUFFER_TOO_SMALL;
            //    }

            //    if (Buffer == null)
            //    {
            //        return QUIC_STATUS_INVALID_PARAMETER;
            //    }

            //    Buffer.Length = sizeof(bool);
            //    Buffer.Buffer[0] = (byte)(Configuration.Settings.VersionNegotiationExtEnabled ? 1 : 0);
            //    return QUIC_STATUS_SUCCESS;
            //}

            return QUIC_STATUS_INVALID_PARAMETER;
        }

    }
}
