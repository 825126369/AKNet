using System;

#if USE_MSQUIC_2 
using MSQuic2;
#else
using MSQuic1;
#endif

namespace AKNet.Udp1MSQuic.Common
{
    internal sealed class MsQuicTlsSecret : IDisposable
    {
        private QUIC_TLS_SECRETS _tlsSecrets;
        public static MsQuicTlsSecret Create(QUIC_HANDLE handle)
        {
            QUIC_TLS_SECRETS tlsSecrets = null;
            try
            {
                //tlsSecrets = new QUIC_TLS_SECRETS();
                //MsQuicHelpers.SetMsQuicParameter(handle, QUIC_TLS_SECRETS.QUIC_PARAM_CONN_TLS_SECRETS, (uint)sizeof(QUIC_TLS_SECRETS), (byte*)tlsSecrets);
                //MsQuicTlsSecret instance = new MsQuicTlsSecret(tlsSecrets);
                //handle.Disposable = instance;
                //return instance;
                return null;
            }
            catch (Exception ex)
            {
                return null;
            }
        }

        private MsQuicTlsSecret(QUIC_TLS_SECRETS tlsSecrets)
        {
            _tlsSecrets = tlsSecrets;
        }

        public void WriteSecret()
        {
            //ReadOnlySpan<byte> clientRandom = _tlsSecrets->IsSet.ClientRandom != 0
            //    ? new ReadOnlySpan<byte>(_tlsSecrets.ClientRandom, 32)
            //    : ReadOnlySpan<byte>.Empty;

            //Span<byte> clientHandshakeTrafficSecret = _tlsSecrets->IsSet.ClientHandshakeTrafficSecret != 0
            //    ? new Span<byte>(_tlsSecrets.ClientHandshakeTrafficSecret, _tlsSecrets.SecretLength)
            //    : Span<byte>.Empty;

            //Span<byte> serverHandshakeTrafficSecret = _tlsSecrets->IsSet.ServerHandshakeTrafficSecret != 0
            //    ? new Span<byte>(_tlsSecrets.ServerHandshakeTrafficSecret, _tlsSecrets.SecretLength)
            //    : Span<byte>.Empty;

            //Span<byte> clientTrafficSecret0 = _tlsSecrets->IsSet.ClientTrafficSecret0 != 0
            //    ? new Span<byte>(_tlsSecrets.ClientTrafficSecret0, _tlsSecrets.SecretLength)
            //    : Span<byte>.Empty;

            //Span<byte> serverTrafficSecret0 = _tlsSecrets->IsSet.ServerTrafficSecret0 != 0
            //    ? new Span<byte>(_tlsSecrets.ServerTrafficSecret0, _tlsSecrets.SecretLength)
            //    : Span<byte>.Empty;

            //Span<byte> clientEarlyTrafficSecret = _tlsSecrets->IsSet.ClientEarlyTrafficSecret != 0
            //    ? new Span<byte>(_tlsSecrets.ClientEarlyTrafficSecret, _tlsSecrets.SecretLength)
            //    : Span<byte>.Empty;

            //SslKeyLogger.WriteSecrets(
            //    clientRandom,
            //    clientHandshakeTrafficSecret,
            //    serverHandshakeTrafficSecret,
            //    clientTrafficSecret0,
            //    serverTrafficSecret0,
            //    clientEarlyTrafficSecret);
            
            //if (!clientHandshakeTrafficSecret.IsEmpty)
            //{
            //    clientHandshakeTrafficSecret.Clear();
            //    _tlsSecrets->IsSet.ClientHandshakeTrafficSecret = 0;
            //}

            //if (!serverHandshakeTrafficSecret.IsEmpty)
            //{
            //    serverHandshakeTrafficSecret.Clear();
            //    _tlsSecrets->IsSet.ServerHandshakeTrafficSecret = 0;
            //}

            //if (!clientTrafficSecret0.IsEmpty)
            //{
            //    clientTrafficSecret0.Clear();
            //    _tlsSecrets->IsSet.ClientTrafficSecret0 = 0;
            //}

            //if (!serverTrafficSecret0.IsEmpty)
            //{
            //    serverTrafficSecret0.Clear();
            //    _tlsSecrets->IsSet.ServerTrafficSecret0 = 0;
            //}

            //if (!clientEarlyTrafficSecret.IsEmpty)
            //{
            //    clientEarlyTrafficSecret.Clear();
            //    _tlsSecrets->IsSet.ClientEarlyTrafficSecret = 0;
            //}
        }

        public void Dispose()
        {
            if (_tlsSecrets is null)
            {
                return;
            }

            lock (this)
            {
                if (_tlsSecrets is null)
                {
                    return;
                }

                QUIC_TLS_SECRETS tlsSecrets = _tlsSecrets;
                _tlsSecrets = null;
            }
        }
    }
}
