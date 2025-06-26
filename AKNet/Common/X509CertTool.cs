using System;
using System.IO;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;
namespace AKNet.Common
{
    //.cer / .crt 文件内容：.cer 或.crt 文件通常只包含证书本身（即公钥和证书信息），不包含私钥。
    //.pfx 文件（也称为 .p12 文件）是一个容器，可以包含以下内容：证书链：包括服务器证书、中间证书和根证书。私钥：与证书对应的私钥，用于身份验证。
    //.pfx（Personal Information Exchange）格式是一种二进制格式，用于存储证书及其私钥
    //PEM（Privacy Enhanced Mail）格式是一种基于 Base64 编码的文本格式，用于存储和传输加密材料（如证书、私钥等）。

    /*New-SelfSignedCertificate
     * -DnsName $env:computername,
     * localhost 
     * -FriendlyName MsQuic-Test 
     * -KeyUsageProperty Sign 
     * -KeyUsage DigitalSignature 
     * -CertStoreLocation cert:\CurrentUser\My 
     * -HashAlgorithm SHA256 
     * -Provider "Microsoft Software Key Storage Provider" 
     * -KeyExportPolicy Exportable
    */

    //发现问题: 第一次创建有 私钥， 接下来从 X509Store 中取出时，私钥就没有了
    internal static class X509CertTool
    {
        private static string storeName = "xuke_quic_test_cert";
        //public static StoreName storeName = StoreName.My;
        private const string pem_fileName = "xuke_quic_test_cert.pem";
        private const string pfx_fileName = "xuke_quic_test_cert.pfx";
        private const string cert_fileName = "xuke_quic_test_cert.cert";

        public static X509Certificate2 GetCert()
        {
            X509Certificate2 ori_X509Certificate2 = GetCertFromX509Store();
            if (ori_X509Certificate2 == null)
            {
                ori_X509Certificate2 = CreateCert();
            }

            NetLog.Assert(GetCertByHash(ori_X509Certificate2.GetCertHash()) != null);
            NetLog.Log("X509Certificate2 哈希值：" + ori_X509Certificate2.GetCertHashString());
            return ori_X509Certificate2;
        }

        //Pfx 文件，必须包含私钥
        public static X509Certificate2 GetPfxCert()
        {
            X509Store mX509Store = new X509Store(storeName, StoreLocation.CurrentUser);
            mX509Store.Open(OpenFlags.MaxAllowed | OpenFlags.ReadWrite);
            X509Certificate2 target_cert = null;
            for (int i = mX509Store.Certificates.Count - 1; i >= 0; i--)
            {
                if (!orCertValid(mX509Store.Certificates[i]) || !mX509Store.Certificates[i].HasPrivateKey)
                {
                    mX509Store.Remove(mX509Store.Certificates[i]);
                }
            }
            
            for (int i = mX509Store.Certificates.Count - 1; i >= 1; i--)
            {
                mX509Store.Remove(mX509Store.Certificates[i]);
            }
            
            if (mX509Store.Certificates.Count == 1)
            {
                target_cert = mX509Store.Certificates[0];
            }
            mX509Store.Close();
            if (target_cert == null)
            {
                target_cert = CreateAndSavePfxCert();
            }
            return target_cert;
        }

        public static X509Certificate2 GetPfxCertByHash(string hash, string Password = null)
        {
            X509Certificate2 target_cert = GetCertByHash(hash);
            if (target_cert == null)
            {
                target_cert = CreateAndSavePfxCert(Password);
            }
            return target_cert;
        }

        public static X509Certificate2 GetPfxCertByHash(byte[] hash, string Password = null)
        {
            X509Certificate2 target_cert = GetCertByHash(hash);
            if (target_cert == null)
            {
                target_cert = CreateAndSavePfxCert(Password);
            }
            return target_cert;
        }

        private static X509Certificate2 CreateAndSavePfxCert(string Password = null)
        {
            if (string.IsNullOrWhiteSpace(Password))
            {
                Span<byte> PasswordBuffer = new byte[50];
                RandomTool.Random(PasswordBuffer);
                Password = Convert.ToBase64String(PasswordBuffer);
            }

            //这种方式创建的证书，包含私钥，其他方式不包含
            var ori_cert = CreateSelfSignedCertificate();
            byte[] Data = ori_cert.Export(X509ContentType.Pfx, Password);
            X509Certificate2 new_cert = new X509Certificate2(Data, Password, X509KeyStorageFlags.Exportable | X509KeyStorageFlags.PersistKeySet);
            X509Store mX509Store = new X509Store(storeName, StoreLocation.CurrentUser);
            mX509Store.Open(OpenFlags.ReadWrite);
            mX509Store.Add(new_cert);
            mX509Store.Close();

            NetLog.Log("创建新证书:" + new_cert.GetCertHashString());
            return new_cert;
        }

        public static X509Certificate2 GetCertByHash(string hash)
        {
            X509Certificate2 target_cert = null;
            X509Store store = new X509Store(storeName, StoreLocation.CurrentUser);
            store.Open(OpenFlags.MaxAllowed | OpenFlags.ReadOnly);
            foreach (X509Certificate2 cert in store.Certificates)
            {
                if (cert.GetCertHashString().ToUpper() == hash.ToUpper())
                {
                    target_cert = cert;
                    break;
                }
            }
            store.Close();
            return target_cert;
        }

        public static X509Certificate2 GetCertByHash(ReadOnlySpan<byte> hash)
        {
            X509Certificate2 target_cert = null;
            X509Store store = new X509Store(storeName, StoreLocation.CurrentUser);
            store.Open(OpenFlags.MaxAllowed | OpenFlags.ReadOnly);
            foreach (X509Certificate2 cert in store.Certificates)
            {
                if (BufferTool.orBufferEqual(cert.GetCertHash(), hash))
                {
                    target_cert = cert;
                    break;
                }
            }
            store.Close();
            return target_cert;
        }

        public static string ToHexStringUpper(ReadOnlySpan<byte> bytes)
        {
            StringBuilder sb = new StringBuilder();
            foreach (byte b in bytes)
            {
                sb.AppendFormat("{0:X2}", b);
            }
            return sb.ToString();
        }

        static X509Certificate2 GetCertFromX509Store()
        {
            X509Certificate2 ori_X509Certificate2 = null;
            X509Store mX509Store = new X509Store(storeName, StoreLocation.CurrentUser);
            mX509Store.Open(OpenFlags.MaxAllowed | OpenFlags.ReadWrite);
            
            for (int i = mX509Store.Certificates.Count - 1; i >= 0; i--)
            {
                if (!orCertValid(mX509Store.Certificates[i]))
                {
                    mX509Store.Remove(mX509Store.Certificates[i]);
                }
            }
            
            if (mX509Store.Certificates.Count > 1)
            {
                for(int i = mX509Store.Certificates.Count - 1; i >= 1; i--)
                {
                    mX509Store.Remove(mX509Store.Certificates[i]);
                }
            }

            if (mX509Store.Certificates.Count == 1)
            {
                ori_X509Certificate2 = mX509Store.Certificates[0];
            }
            mX509Store.Close();
            return ori_X509Certificate2;
        }

        static bool orCertValid(X509Certificate2 certificate)
        {
            if (DateTime.Now < certificate.NotBefore || DateTime.Now > certificate.NotAfter)
            {
                NetLog.LogError("证书已过期或尚未生效！");
                return false;
            }

            // 验证证书链
            X509Chain chain = new X509Chain();
            chain.ChainPolicy.RevocationMode = X509RevocationMode.NoCheck; // 可以根据需要启用吊销检查
            chain.ChainPolicy.VerificationFlags = X509VerificationFlags.AllowUnknownCertificateAuthority;
            bool isValid = chain.Build(certificate);
            if(!isValid)
            {
                foreach (var status in chain.ChainStatus)
                {
                    NetLog.LogError("错误信息: " + status.StatusInformation);
                }
            }
            return isValid;
        }

        static X509Certificate2 CreateCert()
        {
            X509Certificate2 certificate = CreateSelfSignedCertificate();
            certificate = CreateCert_Cert_ForQuic(certificate);

            if (orCertValid(certificate))
            {
                X509Store mX509Store = new X509Store(storeName, StoreLocation.CurrentUser);
                mX509Store.Open(OpenFlags.ReadWrite);
                mX509Store.Add(certificate);
                mX509Store.Close();

                return certificate;
            }
            else
            {
                NetLog.LogError("CreateCert Error: " + certificate);
            }
            return null;
        }

        static X509Certificate2 CreateSelfSignedCertificate()
        {
            string subjectName = "CN=localhost"; // 替换为你的主机名或域名
            string friendlyName = "quic_test_cert";

            using (RSA rsa = RSA.Create(2048)) // 使用 2048 位 RSA 密钥
            {
                var certificateRequest = new CertificateRequest(
                    subjectName,
                    rsa,
                    HashAlgorithmName.SHA256,
                    RSASignaturePadding.Pkcs1
                );

                certificateRequest.CertificateExtensions.Add(new X509BasicConstraintsExtension(false, false, 0, false));
                certificateRequest.CertificateExtensions.Add(new X509EnhancedKeyUsageExtension(new OidCollection
                {
                    new Oid("1.3.6.1.5.5.7.3.1") // serverAuth

                }, false));
                    
                certificateRequest.CertificateExtensions.Add(
                    new X509KeyUsageExtension(X509KeyUsageFlags.DigitalSignature, false)
                );

                SubjectAlternativeNameBuilder sanBuilder = new SubjectAlternativeNameBuilder();
                sanBuilder.AddDnsName("localhost");
                certificateRequest.CertificateExtensions.Add(sanBuilder.Build());

                // 创建证书
                X509Certificate2 certificate = certificateRequest.CreateSelfSigned(DateTime.UtcNow.AddDays(-1),DateTime.UtcNow.AddYears(1));
                certificate.FriendlyName = friendlyName;
                return certificate;
            }
        }

        //这个适合C#原生 SSLStream
        static X509Certificate2 CreateCert_Cert(X509Certificate2 ori_cert)
        {
            byte[] Data = ori_cert.Export(X509ContentType.Pfx);
            string path = Path.Combine(AppContext.BaseDirectory, cert_fileName);
            File.WriteAllBytes(path, Data);

            X509Certificate2 new_cert = new X509Certificate2(Data);
            // X509Certificate2 new_cert = X509CertificateLoader.LoadCertificate(Data);

            NetLog.Log("证书已导出到：" + path);
            NetLog.Log("ori_cert 哈希值：" + ori_cert.GetCertHashString());
            NetLog.Log("new_cert 哈希值：" + new_cert.GetCertHashString());
            return ori_cert;
        }
        
        //这个适合 Quic
        static X509Certificate2 CreateCert_Cert_ForQuic(X509Certificate2 ori_cert)
        {
            byte[] Data = ori_cert.Export(X509ContentType.Pfx);
            string path = Path.Combine(AppContext.BaseDirectory, cert_fileName);
            File.WriteAllBytes(path, Data);

            X509Certificate2 new_cert = new X509Certificate2(Data);

            NetLog.Log("证书已导出到：" + path);
            NetLog.Log("ori_cert 哈希值：" + ori_cert.GetCertHashString());
            NetLog.Log("new_cert 哈希值：" + new_cert.GetCertHashString());
            return ori_cert;
        }

        static string GetCertificatePEM(X509Certificate2 certificate)
        {
            // 获取证书的公钥部分（DER 编码的字节数组）
            byte[] derBytes = certificate.Export(X509ContentType.Cert);
            // 将字节数组转换为 Base64 编码的字符串
            string base64String = Convert.ToBase64String(derBytes);
            // 构造 PEM 格式的内容
            string pemContent = $"-----BEGIN CERTIFICATE-----\n{base64String}\n-----END CERTIFICATE-----";
            return pemContent;
        }

    }
}
