using AKNet.Common;
using System.Collections.Generic;
using System.Text;

namespace AKNet.Udp5MSQuic.Common
{
    internal static partial class ClientConfig
    {
        private static QUIC_SETTINGS GetSetting()
        {
            QUIC_SETTINGS settings = new QUIC_SETTINGS();
            MSQuicFunc.SetFlag(settings.IsSetFlags, MSQuicFunc.E_SETTING_FLAG_PeerUnidiStreamCount, true);
            settings.PeerUnidiStreamCount = 10;
            MSQuicFunc.SetFlag(settings.IsSetFlags, MSQuicFunc.E_SETTING_FLAG_PeerBidiStreamCount, true);
            settings.PeerBidiStreamCount = 10;

            MSQuicFunc.SetFlag(settings.IsSetFlags, MSQuicFunc.E_SETTING_FLAG_IdleTimeoutMs, true);
            settings.IdleTimeoutMs = 0;
            MSQuicFunc.SetFlag(settings.IsSetFlags, MSQuicFunc.E_SETTING_FLAG_KeepAliveIntervalMs, true);
            settings.KeepAliveIntervalMs = 0; // 0 disables the keepalive
            
            MSQuicFunc.SetFlag(settings.IsSetFlags, MSQuicFunc.E_SETTING_FLAG_HandshakeIdleTimeoutMs, true);
            settings.HandshakeIdleTimeoutMs = 0;
            
            return settings;
        }

        public static QUIC_CONFIGURATION Create(bool Unsecure)
        {
            List<QUIC_BUFFER> mAlpnList = new List<QUIC_BUFFER>();
            mAlpnList.Add(new QUIC_BUFFER(Encoding.UTF8.GetBytes("hello, IsMe")));

            QUIC_SETTINGS settings = GetSetting();

            QUIC_CREDENTIAL_CONFIG CredConfig = new QUIC_CREDENTIAL_CONFIG();
            CredConfig.Type =  QUIC_CREDENTIAL_TYPE.QUIC_CREDENTIAL_TYPE_NONE;
            CredConfig.Flags = QUIC_CREDENTIAL_FLAGS.QUIC_CREDENTIAL_FLAG_CLIENT;
            if (Unsecure)
            {
                CredConfig.Flags |=  QUIC_CREDENTIAL_FLAGS.QUIC_CREDENTIAL_FLAG_NO_CERTIFICATE_VALIDATION;
            }
            
            QUIC_CONFIGURATION configurationHandle;
            if (MSQuicFunc.QUIC_FAILED(MSQuicFunc.MsQuicConfigurationOpen(MsQuicApi.Api.Registration, mAlpnList.ToArray(), mAlpnList.Count, settings, null, out configurationHandle)))
            {
                NetLog.LogError("ConfigurationOpen failed");
            }
            
            if (MSQuicFunc.QUIC_FAILED(MSQuicFunc.MsQuicConfigurationLoadCredential(configurationHandle, CredConfig)))
            {
                NetLog.LogError("ConfigurationLoadCredential failed");
            }

            return configurationHandle;
        }
    }

}
