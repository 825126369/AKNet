// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using AKNet.Common;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Net.Security;
using System.Security.Cryptography.X509Certificates;

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

            if (MsQuicApi.UsesSChannelBackend)
            {
                flags |= QUIC_CREDENTIAL_FLAGS.QUIC_CREDENTIAL_FLAG_USE_SUPPLIED_CREDENTIALS;
            }

            X509Certificate? certificate = null;
            ReadOnlyCollection<X509Certificate2>? intermediates = null;
            if (authenticationOptions != null)
            {
                certificate = authenticationOptions.ClientCertificates;
                intermediates = authenticationOptions.ClientCertificateContext.IntermediateCertificates;
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

            return Create(options, flags, certificate, intermediates, authenticationOptions.ApplicationProtocols, authenticationOptions.CipherSuitesPolicy, authenticationOptions.EncryptionPolicy);
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
            ReadOnlyCollection<X509Certificate2>? intermediates = default;
            
            if (authenticationOptions.ServerCertificateSelectionCallback is not null)
            {
                certificate = authenticationOptions.ServerCertificateSelectionCallback.Invoke(authenticationOptions, targetHost);
            }

            //else if (authenticationOptions.ServerCertificateContext is not null)
            //{
            //    certificate = authenticationOptions.ServerCertificateContext.TargetCertificate;
            //    intermediates = authenticationOptions.ServerCertificateContext.IntermediateCertificates;
            //}
            //else if (authenticationOptions.ServerCertificate is not null)
            //{
            //    certificate = authenticationOptions.ServerCertificate;
            //}

            //if (certificate is null)
            //{
            //    throw new ArgumentException(SR.Format(SR.net_quic_not_null_ceritifcate, nameof(SslServerAuthenticationOptions.ServerCertificate), nameof(SslServerAuthenticationOptions.ServerCertificateContext), nameof(SslServerAuthenticationOptions.ServerCertificateSelectionCallback)), nameof(options));
            //}

            return CreateInternal(options, flags, certificate, intermediates, authenticationOptions.ApplicationProtocols, authenticationOptions.CipherSuitesPolicy, authenticationOptions.EncryptionPolicy);
        }

        private static QUIC_CONFIGURATION CreateInternal(QUIC_SETTINGS settings, QUIC_CREDENTIAL_FLAGS flags, X509Certificate? certificate, ReadOnlyCollection<X509Certificate2>? intermediates, List<SslApplicationProtocol> alpnProtocols, QUIC_ALLOWED_CIPHER_SUITE_FLAGS allowedCipherSuites)
        {
            QUIC_CONFIGURATION handle;
            MsQuicBuffers msquicBuffers = new MsQuicBuffers();
            msquicBuffers.Initialize(alpnProtocols, alpnProtocol => alpnProtocol.Protocol);
            if (MsQuicHelpers.QUIC_FAILED(MSQuicFunc.MsQuicConfigurationOpen(MsQuicApi.Api.Registration, msquicBuffers.Buffers, settings, null, out handle)))
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
                if (certificate is null)
                {
                    config.Type = QUIC_CREDENTIAL_TYPE.QUIC_CREDENTIAL_TYPE_NONE;
                    status = MSQuicFunc.MsQuicConfigurationLoadCredential(configurationHandle, config);
                }
                else if (MsQuicApi.UsesSChannelBackend)
                {
                    config.Type = QUIC_CREDENTIAL_TYPE.QUIC_CREDENTIAL_TYPE_CERTIFICATE_CONTEXT;
                    config.CertificateContext = certificate.Handle;
                    status = MSQuicFunc.MsQuicConfigurationLoadCredential(configurationHandle, config);
                }
                else
                {
                    config.Type = QUIC_CREDENTIAL_TYPE.QUIC_CREDENTIAL_TYPE_CERTIFICATE_PKCS12;
                    byte[] certificateData;
                    if (intermediates != null && intermediates.Count > 0)
                    {
                        X509Certificate2Collection collection = new X509Certificate2Collection();
                        collection.Add(certificate);
                        foreach (X509Certificate2 intermediate in intermediates)
                        {
                            collection.Add(intermediate);
                        }
                        certificateData = collection.Export(X509ContentType.Pkcs12)!;
                    }
                    else
                    {
                        certificateData = certificate.Export(X509ContentType.Pkcs12);
                    }


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
