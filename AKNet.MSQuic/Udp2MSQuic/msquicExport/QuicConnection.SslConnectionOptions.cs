/************************************Copyright*****************************************
*        ProjectName:AKNet
*        Web:https://github.com/825126369/AKNet
*        Description:C#游戏网络库
*        Author:许珂
*        ModifyTime:2025/11/14 8:44:38
*        Copyright:MIT软件许可证
************************************Copyright*****************************************/
using AKNet.Common;
using System;
using System.Buffers;
using System.Diagnostics;
using System.Net.Security;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;

#if USE_MSQUIC_2 
using MSQuic2;
#else
using MSQuic1;
#endif

namespace AKNet.Udp2MSQuic.Common
{
    internal partial class QuicConnection
    {
        internal readonly struct SslConnectionOptions
        {
            private static readonly Oid s_serverAuthOid = new Oid("1.3.6.1.5.5.7.3.1", null);
            private static readonly Oid s_clientAuthOid = new Oid("1.3.6.1.5.5.7.3.2", null);

            /// <summary>
            /// The connection to which these options belong.
            /// </summary>
            private readonly QuicConnection _connection;
            /// <summary>
            /// Determines if the connection is outbound/client or inbound/server.
            /// </summary>
            private readonly bool _isClient;
            /// <summary>
            /// Host name send in SNI, set only for outbound/client connections. Configured via <see cref="SslClientAuthenticationOptions.TargetHost"/>.
            /// </summary>
            private readonly string _targetHost;
            /// <summary>
            /// Always <c>true</c> for outbound/client connections. Configured for inbound/server ones via <see cref="SslServerAuthenticationOptions.ClientCertificateRequired"/>.
            /// </summary>
            private readonly bool _certificateRequired;
            /// <summary>
            /// Configured via <see cref="SslServerAuthenticationOptions.CertificateRevocationCheckMode"/> or <see cref="SslClientAuthenticationOptions.CertificateRevocationCheckMode"/>.
            /// </summary>
            private readonly X509RevocationMode _revocationMode;
            /// <summary>
            /// Configured via <see cref="SslServerAuthenticationOptions.RemoteCertificateValidationCallback"/> or <see cref="SslClientAuthenticationOptions.RemoteCertificateValidationCallback"/>.
            /// </summary>
            private readonly RemoteCertificateValidationCallback _validationCallback;

            /// <summary>
            /// Configured via <see cref="SslServerAuthenticationOptions.CertificateChainPolicy"/> or <see cref="SslClientAuthenticationOptions.CertificateChainPolicy"/>.
            /// </summary>
            private readonly X509ChainPolicy? _certificateChainPolicy;

            internal string TargetHost => _targetHost;

            public SslConnectionOptions(QuicConnection connection, bool isClient, string targetHost, bool certificateRequired, X509RevocationMode revocationMode, 
                RemoteCertificateValidationCallback validationCallback, X509ChainPolicy certificateChainPolicy)
            {
                _connection = connection;
                _isClient = isClient;
                _targetHost = targetHost;
                _certificateRequired = certificateRequired;
                _revocationMode = revocationMode;
                _validationCallback = validationCallback;
                _certificateChainPolicy = certificateChainPolicy;
            }

            public async Task<bool>  StartAsyncCertificateValidation(object certificatePtr, object chainPtr)
            {
                X509Certificate2? certificate = null;

                byte[]? certDataRented = null;
                Memory<byte> certData = default;
                byte[]? chainDataRented = null;
                Memory<byte> chainData = default;

                if (certificatePtr != null)
                {
                    QUIC_BUFFER certificateBuffer = (QUIC_BUFFER)certificatePtr;
                    QUIC_BUFFER chainBuffer = (QUIC_BUFFER)chainPtr;
                    if (certificateBuffer.Length > 0)
                    {
                        certDataRented = ArrayPool<byte>.Shared.Rent((int)certificateBuffer.Length);
                        certData = certDataRented.AsMemory(0, (int)certificateBuffer.Length);
                        certificateBuffer.GetSpan().CopyTo(certData.Span);
                    }

                    if (chainBuffer.Length > 0)
                    {
                        chainDataRented = ArrayPool<byte>.Shared.Rent((int)chainBuffer.Length);
                        chainData = chainDataRented.AsMemory(0, (int)chainBuffer.Length);
                        chainBuffer.GetSpan().CopyTo(chainData.Span);
                    }
                }
                
                await Task.CompletedTask;

                QUIC_TLS_ALERT_CODES result;
                try
                {
                    if (certData.Length > 0)
                    {
                        Debug.Assert(certificate == null);
                        certificate = new X509Certificate2(chainDataRented);
                    }

                    result = _connection._sslConnectionOptions.ValidateCertificate(certificate, certData.Span, chainData.Span);
                }
                catch (Exception ex)
                {
                    certificate?.Dispose();
                    await _connection.CloseAsync(0);
                    result = QUIC_TLS_ALERT_CODES.QUIC_TLS_ALERT_CODE_USER_CANCELED;
                }
                finally
                {
                    if (certDataRented != null)
                    {
                        ArrayPool<byte>.Shared.Return(certDataRented);
                    }

                    if (chainDataRented != null)
                    {
                        ArrayPool<byte>.Shared.Return(chainDataRented);
                    }
                }

                int status = MSQuicFunc.MsQuicConnectionCertificateValidationComplete(
                    _connection._handle,
                    result == QUIC_TLS_ALERT_CODES.QUIC_TLS_ALERT_CODE_SUCCESS,
                    result);

                if (MSQuicFunc.QUIC_FAILED(status))
                {
                    NetLog.LogError($"ConnectionCertificateValidationComplete failed with {status}");
                }

                return result == QUIC_TLS_ALERT_CODES.QUIC_TLS_ALERT_CODE_SUCCESS;
            }

            private QUIC_TLS_ALERT_CODES ValidateCertificate(X509Certificate2? certificate, ReadOnlySpan<byte> certData, ReadOnlySpan<byte> chainData)
            {
                NetLog.Log("QuicConnection ValidateCertificate: ");
                SslPolicyErrors sslPolicyErrors = SslPolicyErrors.None;
                bool wrapException = false;

                X509Chain chain = null;
                try
                {
                    if (certificate != null)
                    {
                        chain = new X509Chain();
                        if (_certificateChainPolicy != null)
                        {
                            chain.ChainPolicy = _certificateChainPolicy;
                        }
                        else
                        {
                            chain.ChainPolicy.RevocationMode = _revocationMode;
                            chain.ChainPolicy.RevocationFlag = X509RevocationFlag.ExcludeRoot;
                        }

                        if (chain.ChainPolicy.ApplicationPolicy.Count == 0)
                        {
                            chain.ChainPolicy.ApplicationPolicy.Add(_isClient ? s_serverAuthOid : s_clientAuthOid);
                        }

                        if (chainData.Length > 0)
                        {
                            Debug.Assert(X509Certificate2.GetCertContentType(chainData.ToArray()) is X509ContentType.Pkcs7);
                            X509Certificate2Collection additionalCertificates = new X509Certificate2Collection();
                            additionalCertificates.Import(chainData.ToArray());
                            chain.ChainPolicy.ExtraStore.AddRange(additionalCertificates);
                        }

                        bool isValid = chain.Build(certificate);
                        bool checkCertName = !chain!.ChainPolicy!.VerificationFlags.HasFlag(X509VerificationFlags.IgnoreInvalidName);
                        sslPolicyErrors |= SslPolicyErrors.RemoteCertificateChainErrors;
                    }
                    else if (_certificateRequired)
                    {
                        sslPolicyErrors |= SslPolicyErrors.RemoteCertificateNotAvailable;
                    }

                    QUIC_TLS_ALERT_CODES result = QUIC_TLS_ALERT_CODES.QUIC_TLS_ALERT_CODE_SUCCESS;
                    if (_validationCallback != null)
                    {
                        wrapException = true;
                        if (!_validationCallback(_connection, certificate, chain, sslPolicyErrors))
                        {
                            wrapException = false;
                            result = QUIC_TLS_ALERT_CODES.QUIC_TLS_ALERT_CODE_BAD_CERTIFICATE;
                        }
                    }
                    else if (sslPolicyErrors != SslPolicyErrors.None)
                    {
                        result = QUIC_TLS_ALERT_CODES.QUIC_TLS_ALERT_CODE_BAD_CERTIFICATE;
                    }

                    return result;
                }
                catch (Exception ex)
                {
                    if (wrapException)
                    {
                        
                    }

                    throw;
                }

                finally
                {
                    if (chain != null)
                    {
                        X509ChainElementCollection elements = chain.ChainElements;
                        for (int i = 0; i < elements.Count; i++)
                        {
                            elements[i].Certificate.Dispose();
                        }

                        chain.Dispose();
                    }
                }
            }
        }
    }
}
