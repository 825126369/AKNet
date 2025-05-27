using AKNet.Common;
using System;
using System.Collections.Generic;
using System.Text;

namespace AKNet.Udp5MSQuic.Common
{
    internal class QUIC_CERTIFICATE_HASH
    {
        public QUIC_BUFFER ShaHash  = new QUIC_BUFFER(20);
    }

    internal class QUIC_CERTIFICATE_FILE
    {
        public byte[] PrivateKeyFile;
        public byte[] CertificateFile;
    }

    internal class QUIC_CERTIFICATE_FILE_PROTECTED
    {
        public byte[] PrivateKeyFile;
        public byte[] CertificateFile;
        public byte[] PrivateKeyPassword;
    }

    internal class QUIC_CERTIFICATE_PKCS12
    {
        public byte[] Asn1Blob;
        public int Asn1BlobLength;
        public byte[] PrivateKeyPassword;     // Optional: used if provided. Ignored if NULL
    }

    internal enum QUIC_ALLOWED_CIPHER_SUITE_FLAGS
    {
        QUIC_ALLOWED_CIPHER_SUITE_NONE = 0x0,
        QUIC_ALLOWED_CIPHER_SUITE_AES_128_GCM_SHA256 = 0x1,
        QUIC_ALLOWED_CIPHER_SUITE_AES_256_GCM_SHA384 = 0x2,
        QUIC_ALLOWED_CIPHER_SUITE_CHACHA20_POLY1305_SHA256 = 0x4,  // Not supported on Schannel
    }

    internal delegate void QUIC_CREDENTIAL_LOAD_COMPLETE(QUIC_CONFIGURATION Configuration,object Context, ulong Status);

    internal class QUIC_CREDENTIAL_CONFIG
    {
        public QUIC_CREDENTIAL_TYPE Type;
        public QUIC_CREDENTIAL_FLAGS Flags;

        public QUIC_CERTIFICATE_HASH CertificateHash;
        public QUIC_CERTIFICATE_FILE CertificateFile;
        public QUIC_CERTIFICATE_FILE_PROTECTED CertificateFileProtected;
        public QUIC_CERTIFICATE_PKCS12 CertificatePkcs12;
        public object CertificateContext;

        public byte[] Principal;
        public QUIC_CREDENTIAL_LOAD_COMPLETE AsyncHandler; // Optional
        public QUIC_ALLOWED_CIPHER_SUITE_FLAGS AllowedCipherSuites;// Optional
        public byte[] CaCertificateFile;                      // Optional
    }

    internal class QUIC_CONFIGURATION : QUIC_HANDLE
    {
        public QUIC_REGISTRATION Registration;
        public readonly CXPLAT_LIST_ENTRY Link;
        public long RefCount;
        public CXPLAT_SEC_CONFIG SecurityConfig;
        public uint CompartmentId;
        public QUIC_SETTINGS Settings = new QUIC_SETTINGS();
        public readonly QUIC_BUFFER AlpnList = null;

        public QUIC_CONFIGURATION(int AlpnListLength) 
        {
            AlpnList = new QUIC_BUFFER(AlpnListLength);
            Link = new CXPLAT_LIST_ENTRY<QUIC_CONFIGURATION>(this);
        }
    }

    internal static partial class MSQuicFunc
    {
        public static ulong MsQuicConfigurationOpen(QUIC_REGISTRATION Registration, QUIC_BUFFER[] AlpnBuffers, int AlpnBuffersCount, QUIC_SETTINGS Settings,
            object Context, out QUIC_CONFIGURATION NewConfiguration)
        {

            ulong Status = QUIC_STATUS_INVALID_PARAMETER;
            NewConfiguration = null;
            QUIC_CONFIGURATION Configuration = null;

            if (AlpnBuffers == null || AlpnBuffersCount == 0)
            {
                goto Error;
            }

            int AlpnListLength = 0;
            for (int i = 0; i < AlpnBuffersCount; ++i)
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

            for (int i = 0; i < AlpnBuffersCount; ++i)
            {
                AlpnList[0] = (byte)AlpnBuffers[i].Length;
                AlpnList = AlpnList.Slice(1);

                AlpnBuffers[i].GetSpan().CopyTo(AlpnList);
                AlpnList = AlpnList.Slice(AlpnBuffers[i].Length);
            }

            if (!string.IsNullOrWhiteSpace(Registration.AppName))
            {
                StringBuilder SpecificAppKey = new StringBuilder(QUIC_SETTING_APP_KEY);
                SpecificAppKey.Append(Registration.AppName);
                Status = QUIC_STATUS_SUCCESS;
            }

            if (Settings != null && Settings.IsSetFlags != 0)
            {
                Configuration.Settings = Settings;
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

        public static ulong MsQuicConfigurationLoadCredential(QUIC_CONFIGURATION Handle, QUIC_CREDENTIAL_CONFIG CredConfig)
        {
            ulong Status = QUIC_STATUS_INVALID_PARAMETER;

            if (Handle != null && CredConfig != null && Handle.Type == QUIC_HANDLE_TYPE.QUIC_HANDLE_TYPE_CONFIGURATION)
            {
                QUIC_CONFIGURATION Configuration = Handle;
                CXPLAT_TLS_CREDENTIAL_FLAGS TlsCredFlags = CXPLAT_TLS_CREDENTIAL_FLAGS.CXPLAT_TLS_CREDENTIAL_FLAG_NONE;
                if (!(CredConfig.Flags.HasFlag(QUIC_CREDENTIAL_FLAGS.QUIC_CREDENTIAL_FLAG_CLIENT)) &&
                    Configuration.Settings.ServerResumptionLevel == QUIC_SERVER_RESUMPTION_LEVEL.QUIC_SERVER_NO_RESUME)
                {
                    TlsCredFlags |= CXPLAT_TLS_CREDENTIAL_FLAGS.CXPLAT_TLS_CREDENTIAL_FLAG_DISABLE_RESUMPTION;
                }

                QuicConfigurationAddRef(Configuration);

                Status =
                    CxPlatTlsSecConfigCreate(
                        CredConfig,
                        TlsCredFlags,
                        QuicTlsCallbacks,
                        Configuration,
                        MsQuicConfigurationLoadCredentialComplete);
            }

            return Status;
        }

        static void MsQuicConfigurationLoadCredentialComplete(QUIC_CREDENTIAL_CONFIG CredConfig, object Context, ulong Status, CXPLAT_SEC_CONFIG SecurityConfig)
        {
            QUIC_CONFIGURATION Configuration = (QUIC_CONFIGURATION)Context;

            NetLog.Assert(Configuration != null);
            NetLog.Assert(CredConfig != null);

            if (QUIC_SUCCEEDED(Status))
            {
                NetLog.Assert(SecurityConfig != null);
                Configuration.SecurityConfig = SecurityConfig;
            }
            else
            {
                NetLog.Assert(SecurityConfig == null);
            }

            if (CredConfig.Flags.HasFlag(QUIC_CREDENTIAL_FLAGS.QUIC_CREDENTIAL_FLAG_LOAD_ASYNCHRONOUS))
            {
                NetLog.Assert(CredConfig.AsyncHandler != null);
                CredConfig.AsyncHandler(Configuration, Configuration.ClientContext, Status);
            }
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
