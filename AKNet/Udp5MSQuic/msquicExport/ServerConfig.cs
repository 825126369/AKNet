// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using AKNet.Common;
using System;
using System.Collections.Generic;
using System.Security.Cryptography.X509Certificates;
using System.Text;

namespace AKNet.Udp5MSQuic.Common
{
    internal static partial class ServerConfig
    {
        public static readonly List<string> ApplicationProtocols = new List<string>() { "hello, IsMe" };

        private static QUIC_SETTINGS GetSetting(QuicConnectionOptions options)
        {
            QUIC_SETTINGS settings = new QUIC_SETTINGS();
            MSQuicFunc.SetFlag(settings.IsSetFlags, MSQuicFunc.E_SETTING_FLAG_PeerUnidiStreamCount, true);
            settings.PeerUnidiStreamCount = (ushort)options.MaxInboundUnidirectionalStreams;

            MSQuicFunc.SetFlag(settings.IsSetFlags, MSQuicFunc.E_SETTING_FLAG_PeerBidiStreamCount, true);
            settings.PeerBidiStreamCount = (ushort)options.MaxInboundBidirectionalStreams;


            MSQuicFunc.SetFlag(settings.IsSetFlags, MSQuicFunc.E_SETTING_FLAG_IdleTimeoutMs, true);
            settings.IdleTimeoutMs = 0;

            MSQuicFunc.SetFlag(settings.IsSetFlags, MSQuicFunc.E_SETTING_FLAG_KeepAliveIntervalMs, true);
            settings.KeepAliveIntervalMs = 0; // 0 disables the keepalive


            MSQuicFunc.SetFlag(settings.IsSetFlags, MSQuicFunc.E_SETTING_FLAG_ConnFlowControlWindow, true);
            settings.ConnFlowControlWindow = (uint)(options._initialReceiveWindowSizes?.Connection ?? QuicDefaults.DefaultConnectionMaxData);

            MSQuicFunc.SetFlag(settings.IsSetFlags, MSQuicFunc.E_SETTING_FLAG_StreamRecvWindowBidiLocalDefault, true);
            settings.StreamRecvWindowBidiLocalDefault = (int)(options._initialReceiveWindowSizes?.LocallyInitiatedBidirectionalStream ?? QuicDefaults.DefaultStreamMaxData);

            MSQuicFunc.SetFlag(settings.IsSetFlags, MSQuicFunc.E_SETTING_FLAG_StreamRecvWindowBidiRemoteDefault, true);
            settings.StreamRecvWindowBidiRemoteDefault = (int)(options._initialReceiveWindowSizes?.RemotelyInitiatedBidirectionalStream ?? QuicDefaults.DefaultStreamMaxData);

            MSQuicFunc.SetFlag(settings.IsSetFlags, MSQuicFunc.E_SETTING_FLAG_StreamRecvWindowUnidiDefault, true);
            settings.StreamRecvWindowUnidiDefault = (int)(options._initialReceiveWindowSizes?.UnidirectionalStream ?? QuicDefaults.DefaultStreamMaxData);

            if (options.HandshakeTimeout != TimeSpan.Zero)
            {
                MSQuicFunc.SetFlag(settings.IsSetFlags, MSQuicFunc.E_SETTING_FLAG_HandshakeIdleTimeoutMs, true);
                settings.HandshakeIdleTimeoutMs = 0; // 0 disables the timeout
            }

            return settings;
        }

        private static QUIC_CREDENTIAL_CONFIG GetCertConfig(X509Certificate? certificate)
        {
            QUIC_CREDENTIAL_CONFIG CredConfig = new QUIC_CREDENTIAL_CONFIG();
            CredConfig.Type = QUIC_CREDENTIAL_TYPE.QUIC_CREDENTIAL_TYPE_CERTIFICATE_HASH;
            CredConfig.Flags = QUIC_CREDENTIAL_FLAGS.QUIC_CREDENTIAL_FLAG_NONE;
            if (CredConfig.Type == QUIC_CREDENTIAL_TYPE.QUIC_CREDENTIAL_TYPE_CERTIFICATE_HASH)
            {
                byte[] CertHash = certificate.GetCertHash();
                CredConfig.CertificateHash = new QUIC_CERTIFICATE_HASH(CertHash);
            }
            else if (CredConfig.Type == QUIC_CREDENTIAL_TYPE.QUIC_CREDENTIAL_TYPE_CERTIFICATE_FILE_PROTECTED)
            {
                //string Password = "123456";
                //QUIC_CERTIFICATE_FILE_PROTECTED CertFileProtected = new QUIC_CERTIFICATE_FILE_PROTECTED();
                //CertFileProtected.CertificateFile = certificate.Export(X509ContentType.Pkcs12);
                //CertFileProtected.PrivateKeyFile = (char*)KeyFile;
                //CertFileProtected.PrivateKeyPassword = (char*)Password;
                //CredConfig.Type = QUIC_CREDENTIAL_TYPE_CERTIFICATE_FILE_PROTECTED;
                //CredConfig.CertificateFileProtected = &Config.CertFileProtected;

            }
            else if (CredConfig.Type == QUIC_CREDENTIAL_TYPE.QUIC_CREDENTIAL_TYPE_CERTIFICATE_FILE)
            {
                //Config.CertFile.CertificateFile = (char*)Cert;
                //Config.CertFile.PrivateKeyFile = (char*)KeyFile;
                //Config.CredConfig.Type = QUIC_CREDENTIAL_TYPE_CERTIFICATE_FILE;
                //Config.CredConfig.CertificateFile = &Config.CertFile;
            }
            else
            {
                NetLog.Assert(false);
            }
            return CredConfig;
        }

        public static QUIC_CONFIGURATION Create(QuicServerConnectionOptions options)
        {
            List<QUIC_BUFFER> mAlpnList = new List<QUIC_BUFFER>();
            mAlpnList.Add(new QUIC_BUFFER(Encoding.ASCII.GetBytes("hello, IsMe")));

            QUIC_CREDENTIAL_CONFIG CredConfig = GetCertConfig(options.ServerAuthenticationOptions.ServerCertificate);
            QUIC_SETTINGS settings = GetSetting(options);

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
