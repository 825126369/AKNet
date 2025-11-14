/************************************Copyright*****************************************
*        ProjectName:AKNet
*        Web:https://github.com/825126369/AKNet
*        Description:C#游戏网络库
*        Author:许珂
*        ModifyTime:2025/11/14 8:44:38
*        Copyright:MIT软件许可证
************************************Copyright*****************************************/
using AKNet.Common;
using System.Collections.Generic;
using System.Security.Cryptography.X509Certificates;
using System.Text;

#if USE_MSQUIC_2 
using MSQuic2;
#else
using MSQuic1;
#endif

namespace AKNet.Udp2MSQuic.Common
{
    internal static partial class ServerConfig
    {
        public static readonly List<string> ApplicationProtocols = new List<string>() { "hello" };

        private static QUIC_SETTINGS GetSetting(QuicConnectionOptions options)
        {
            QUIC_SETTINGS settings = new QUIC_SETTINGS();
            MSQuicFunc.SetFlag(ref settings.IsSetFlags, MSQuicFunc.E_SETTING_FLAG_PeerUnidiStreamCount, true);
            settings.PeerUnidiStreamCount = 10;

            MSQuicFunc.SetFlag(ref settings.IsSetFlags, MSQuicFunc.E_SETTING_FLAG_PeerBidiStreamCount, true);
            settings.PeerBidiStreamCount = 10;
            
            MSQuicFunc.SetFlag(ref settings.IsSetFlags, MSQuicFunc.E_SETTING_FLAG_IdleTimeoutMs, true);
            settings.IdleTimeoutMs = 0;

            MSQuicFunc.SetFlag(ref settings.IsSetFlags, MSQuicFunc.E_SETTING_FLAG_KeepAliveIntervalMs, true);
            settings.KeepAliveIntervalMs = 0; // 0 disables the keepalive

            MSQuicFunc.SetFlag(ref settings.IsSetFlags, MSQuicFunc.E_SETTING_FLAG_HandshakeIdleTimeoutMs, true);
            settings.HandshakeIdleTimeoutMs = 0; // 0 disables the timeout
            
            return settings;
        }

        private static QUIC_CREDENTIAL_CONFIG GetCertConfig(X509Certificate? certificate)
        {
            QUIC_CREDENTIAL_CONFIG CredConfig = new QUIC_CREDENTIAL_CONFIG();
            CredConfig.Type = QUIC_CREDENTIAL_TYPE.QUIC_CREDENTIAL_TYPE_CERTIFICATE_HASH;
            CredConfig.Flags = 
                QUIC_CREDENTIAL_FLAGS.QUIC_CREDENTIAL_FLAG_NONE |
                QUIC_CREDENTIAL_FLAGS.QUIC_CREDENTIAL_FLAG_SET_ALLOWED_CIPHER_SUITES;

            CredConfig.AllowedCipherSuites = QUIC_ALLOWED_CIPHER_SUITE_FLAGS.QUIC_ALLOWED_CIPHER_SUITE_AES_128_GCM_SHA256;

            if (CredConfig.Type == QUIC_CREDENTIAL_TYPE.QUIC_CREDENTIAL_TYPE_CERTIFICATE_HASH)
            {
                CredConfig.CertificateHash = new QUIC_CERTIFICATE_HASH();
                CredConfig.CertificateHash.Hash = certificate.GetCertHash();
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

        public static QUIC_CONFIGURATION Create(QuicConnectionOptions options)
        {
            List<QUIC_BUFFER> mAlpnList = new List<QUIC_BUFFER>();
            foreach (var v in ApplicationProtocols)
            {
                mAlpnList.Add(new QUIC_BUFFER(Encoding.ASCII.GetBytes(v)));
            }

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
