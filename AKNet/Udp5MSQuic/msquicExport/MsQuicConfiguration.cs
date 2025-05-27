// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using AKNet.Common;
using System;
using System.Collections.Generic;
using System.Net.Security;
using System.Security.Cryptography.X509Certificates;
using System.Threading;

namespace AKNet.Udp5MSQuic.Common
{
    internal static partial class MsQuicConfiguration
    {
        private static bool HasPrivateKey(this X509Certificate certificate)
            => certificate is X509Certificate2 certificate2 && certificate2.Handle != null && certificate2.HasPrivateKey;

        public static QUIC_CONFIGURATION Create(QuicClientConnectionOptions options)
        {
            SslClientAuthenticationOptions authenticationOptions = options.ClientAuthenticationOptions;

            QUIC_CREDENTIAL_FLAGS flags = QUIC_CREDENTIAL_FLAGS.QUIC_CREDENTIAL_FLAG_NONE;
            flags |= QUIC_CREDENTIAL_FLAGS.QUIC_CREDENTIAL_FLAG_CLIENT;
            flags |= QUIC_CREDENTIAL_FLAGS.QUIC_CREDENTIAL_FLAG_INDICATE_CERTIFICATE_RECEIVED;
            flags |= QUIC_CREDENTIAL_FLAGS.QUIC_CREDENTIAL_FLAG_NO_CERTIFICATE_VALIDATION;

            X509Certificate? certificate = null;
            if (authenticationOptions != null)
            {
                certificate = authenticationOptions.ClientCertificates[0];
            }
            else if (authenticationOptions.LocalCertificateSelectionCallback != null)
            {
                X509Certificate selectedCertificate = authenticationOptions.LocalCertificateSelectionCallback(
                    options,
                    authenticationOptions.TargetHost ?? string.Empty,
                    authenticationOptions.ClientCertificates ?? new X509CertificateCollection(),
                    null,
                    Array.Empty<string>());
                if (selectedCertificate.HasPrivateKey())
                {
                    certificate = selectedCertificate;
                }
            }
            else if (authenticationOptions.ClientCertificates != null)
            {
                foreach (X509Certificate clientCertificate in authenticationOptions.ClientCertificates)
                {
                    if (clientCertificate.HasPrivateKey())
                    {
                        certificate = clientCertificate;
                        break;
                    }
                }
            }
            return Create(options, flags, certificate, authenticationOptions.ApplicationProtocols);
        }

        public static QUIC_CONFIGURATION Create(QuicServerConnectionOptions options, string? targetHost)
        {
            SslServerAuthenticationOptions authenticationOptions = options.ServerAuthenticationOptions;

            QUIC_CREDENTIAL_FLAGS flags = QUIC_CREDENTIAL_FLAGS.QUIC_CREDENTIAL_FLAG_NONE;
            if (authenticationOptions.ClientCertificateRequired)
            {
                flags |= QUIC_CREDENTIAL_FLAGS.QUIC_CREDENTIAL_FLAG_REQUIRE_CLIENT_AUTHENTICATION;
                flags |= QUIC_CREDENTIAL_FLAGS.QUIC_CREDENTIAL_FLAG_INDICATE_CERTIFICATE_RECEIVED;
                flags |= QUIC_CREDENTIAL_FLAGS.QUIC_CREDENTIAL_FLAG_NO_CERTIFICATE_VALIDATION;
            }

            X509Certificate? certificate = null;
            if (authenticationOptions.ServerCertificateSelectionCallback != null)
            {
                certificate = authenticationOptions.ServerCertificateSelectionCallback.Invoke(authenticationOptions, targetHost);
            }
            else if (authenticationOptions.ServerCertificate != null)
            {
                certificate = authenticationOptions.ServerCertificate;
            }

            if (certificate == null)
            {

            }

            return Create(options, flags, certificate, authenticationOptions.ApplicationProtocols);
        }

        private static QUIC_CONFIGURATION Create(QuicConnectionOptions options, QUIC_CREDENTIAL_FLAGS flags, X509Certificate? certificate, 
            List<SslApplicationProtocol>? alpnProtocols)
        {
            if (alpnProtocols is null || alpnProtocols.Count <= 0)
            {
                NetLog.LogError("alpnProtocols == null");
                return null;
            }

            QUIC_SETTINGS settings = new QUIC_SETTINGS();
            MSQuicFunc.SetFlag(settings.IsSetFlags, MSQuicFunc.E_SETTING_FLAG_PeerUnidiStreamCount, true);
            settings.PeerUnidiStreamCount = (ushort)options.MaxInboundUnidirectionalStreams;

            MSQuicFunc.SetFlag(settings.IsSetFlags, MSQuicFunc.E_SETTING_FLAG_PeerBidiStreamCount, true);
            settings.PeerBidiStreamCount = (ushort)options.MaxInboundBidirectionalStreams;

            if (options.IdleTimeout != TimeSpan.Zero)
            {
                MSQuicFunc.SetFlag(settings.IsSetFlags, MSQuicFunc.E_SETTING_FLAG_IdleTimeoutMs, true);
                settings.IdleTimeoutMs = options.IdleTimeout != Timeout.InfiniteTimeSpan ? (long)options.IdleTimeout.TotalMilliseconds : 0;
            }

            if (options.KeepAliveInterval != TimeSpan.Zero)
            {
                MSQuicFunc.SetFlag(settings.IsSetFlags, MSQuicFunc.E_SETTING_FLAG_KeepAliveIntervalMs, true);
                settings.KeepAliveIntervalMs = 
                    options.KeepAliveInterval != Timeout.InfiniteTimeSpan? (uint)options.KeepAliveInterval.TotalMilliseconds : 0; // 0 disables the keepalive
            }

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
                settings.HandshakeIdleTimeoutMs = options.HandshakeTimeout != Timeout.InfiniteTimeSpan
                        ? (long)options.HandshakeTimeout.TotalMilliseconds
                        : 0; // 0 disables the timeout
            }
            
            flags |= QUIC_CREDENTIAL_FLAGS.QUIC_CREDENTIAL_FLAG_SET_ALLOWED_CIPHER_SUITES;
            QUIC_ALLOWED_CIPHER_SUITE_FLAGS allowedCipherSuites = QUIC_ALLOWED_CIPHER_SUITE_FLAGS.QUIC_ALLOWED_CIPHER_SUITE_AES_128_GCM_SHA256;
            return CreateInternal(settings, flags, certificate, alpnProtocols, allowedCipherSuites);
        }

        private static QUIC_CONFIGURATION CreateInternal(QUIC_SETTINGS settings, QUIC_CREDENTIAL_FLAGS flags, X509Certificate? certificate, List<SslApplicationProtocol> alpnProtocols, QUIC_ALLOWED_CIPHER_SUITE_FLAGS allowedCipherSuites)
        {
            QUIC_CONFIGURATION handle;
            MsQuicBuffers msquicBuffers = new MsQuicBuffers();
            msquicBuffers.Initialize(alpnProtocols, alpnProtocol => alpnProtocol.Protocol);
            if (MsQuicHelpers.QUIC_FAILED(MSQuicFunc.MsQuicConfigurationOpen(MsQuicApi.Api.Registration, msquicBuffers.Buffers, msquicBuffers.Count, settings, null, out handle)))
            {
                NetLog.LogError("ConfigurationOpen failed");
            }

            QUIC_CONFIGURATION configurationHandle = handle;
            try
            {
                QUIC_CREDENTIAL_CONFIG config = new QUIC_CREDENTIAL_CONFIG()
                {
                    Flags = flags,
                    AllowedCipherSuites = allowedCipherSuites
                };

                ulong status;
                if (certificate == null)
                {
                    config.Type = QUIC_CREDENTIAL_TYPE.QUIC_CREDENTIAL_TYPE_NONE;
                    status = MSQuicFunc.MsQuicConfigurationLoadCredential(configurationHandle, config);
                }
                else
                {
                    config.Type = QUIC_CREDENTIAL_TYPE.QUIC_CREDENTIAL_TYPE_CERTIFICATE_PKCS12;
                    byte[] certificateData = certificate.Export(X509ContentType.Pkcs12);
                    QUIC_CERTIFICATE_PKCS12 pkcs12Certificate = new QUIC_CERTIFICATE_PKCS12
                    {
                        Asn1Blob = certificateData,
                        Asn1BlobLength = certificateData.Length,
                        PrivateKeyPassword = null,
                    };

                    config.CertificatePkcs12 = pkcs12Certificate;
                    status = MSQuicFunc.MsQuicConfigurationLoadCredential(configurationHandle, config);
                    if (MsQuicHelpers.QUIC_FAILED(status))
                    {
                        NetLog.LogError("ConfigurationLoadCredential failed");
                    }
                }
            }
            catch (Exception e)
            {
                NetLog.LogError(e.ToString());
            }
            return configurationHandle;
        }
    }

}
