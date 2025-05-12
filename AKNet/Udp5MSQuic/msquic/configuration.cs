using AKNet.Common;
using System;
using System.Collections.Generic;
using System.Text;

namespace AKNet.Udp5MSQuic.Common
{
    internal class QUIC_CONFIGURATION : QUIC_HANDLE
    {
        public QUIC_REGISTRATION Registration;
        public CXPLAT_LIST_ENTRY Link;
        public long RefCount;
        public CXPLAT_SEC_CONFIG SecurityConfig;
        public uint CompartmentId;
        public QUIC_SETTINGS Settings;
        public readonly QUIC_BUFFER AlpnList = null;

        public QUIC_CONFIGURATION(int AlpnListLength) 
        {
            AlpnList = new QUIC_BUFFER(AlpnListLength);
        }
    }

    internal static partial class MSQuicFunc
    {
        static ulong MsQuicConfigurationOpen(QUIC_REGISTRATION Registration, List<QUIC_BUFFER> AlpnBuffers, QUIC_SETTINGS Settings,
            object Context, out QUIC_CONFIGURATION NewConfiguration)
        {
            ulong Status = QUIC_STATUS_INVALID_PARAMETER;
            NewConfiguration = null;
            QUIC_CONFIGURATION Configuration = null;
            QUIC_SETTINGS InternalSettings;

            if (AlpnBuffers == null || AlpnBuffers.Count == 0)
            {
                goto Error;
            }

            int AlpnListLength = 0;
            for (int i = 0; i < AlpnBuffers.Count; ++i)
            {
                if (AlpnBuffers[i].Length == 0 ||
                    AlpnBuffers[i].Length > QUIC_MAX_ALPN_LENGTH) {
                    Status = QUIC_STATUS_INVALID_PARAMETER;
                    goto Error;
                }
                AlpnListLength += sizeof(byte) + AlpnBuffers[i].Length;
            }

            if (AlpnListLength > ushort.MaxValue)
            {
                Status = QUIC_STATUS_INVALID_PARAMETER;
                goto Error;
            }
            NetLog.Assert(AlpnListLength <= ushort.MaxValue);

            Configuration = new QUIC_CONFIGURATION(AlpnListLength);
            Configuration.Type = QUIC_HANDLE_TYPE.QUIC_HANDLE_TYPE_CONFIGURATION;
            Configuration.ClientContext = Context;
            Configuration.Registration = Registration;
            CxPlatRefInitialize(ref Configuration.RefCount);
            Span<byte> AlpnList = Configuration.AlpnList.GetSpan();

            for (int i = 0; i < AlpnBuffers.Count; ++i)
            {
                AlpnList[0] = (byte)AlpnBuffers[i].Length;
                AlpnList = AlpnList.Slice(1);

                AlpnBuffers[i].GetSpan().CopyTo(AlpnList);
                AlpnList = AlpnList.Slice(AlpnBuffers[i].Length);
            }

            if (string.IsNullOrWhiteSpace(Registration.AppName))
            {
                StringBuilder SpecificAppKey = new StringBuilder(QUIC_SETTING_APP_KEY);
                SpecificAppKey.Append(Registration.AppName);
                Status = QUIC_STATUS_SUCCESS;
            }

            if (Settings != null && Settings.IsSetFlags != 0)
            {
                Status =
                    QuicSettingsSettingsToInternal(
                        SettingsSize,
                        Settings,
                        InternalSettings);
                if (QUIC_FAILED(Status))
                {
                    goto Error;
                }

                if (!QuicSettingApply(Configuration.Settings, true, true, InternalSettings))
                {
                    Status = QUIC_STATUS_INVALID_PARAMETER;
                    goto Error;
                }
            }

            QuicConfigurationSettingsChanged(Configuration);

            bool Result = CxPlatRundownAcquire(Registration.Rundown);
            NetLog.Assert(Result);

            CxPlatLockAcquire(Registration.ConfigLock);
            CxPlatListInsertTail(Registration.Configurations, Configuration.Link);
            CxPlatLockRelease(Registration.ConfigLock);

            NewConfiguration = Configuration;

        Error:
            return Status;
        }

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

        static void QuicConfigurationSettingsChanged(QUIC_CONFIGURATION Configuration)
        {
            QuicSettingsCopy(Configuration.Settings, MsQuicLib.Settings);
        }

    }
}
