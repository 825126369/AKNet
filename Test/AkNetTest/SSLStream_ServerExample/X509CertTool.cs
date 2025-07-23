using System;
using System.IO;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;


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
internal static class X509CertTool
{
    private const string Password = "123456"; // 导出证书时使用的密码
    private const string storeName = "xuke_quic_test_cert";
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

        PrintCertInfo(ori_X509Certificate2);
        return ori_X509Certificate2;
    }

    static void PrintCertInfo(X509Certificate2 cert)
    {
        Console.WriteLine("cert 哈希值：" + cert.GetCertHashString());
        Console.WriteLine("cert HasPrivateKey：" + cert.HasPrivateKey);
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
            for (int i = mX509Store.Certificates.Count - 1; i >= 1; i--)
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
            Console.WriteLine("证书已过期或尚未生效！");
            return false;
        }

        // 验证证书链
        X509Chain chain = new X509Chain();
        chain.ChainPolicy.RevocationMode = X509RevocationMode.NoCheck; // 可以根据需要启用吊销检查
        chain.ChainPolicy.VerificationFlags = X509VerificationFlags.AllowUnknownCertificateAuthority;
        bool isValid = chain.Build(certificate);
        if (!isValid)
        {
            foreach (var status in chain.ChainStatus)
            {
                Console.WriteLine("错误信息: " + status.StatusInformation);
            }
        }
        return isValid;
    }

    static X509Certificate2 CreateCert()
    {
        X509Certificate2 certificate = CreateSelfSignedCertificate();
        certificate = CreateCert_Cert(certificate);

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
            Console.WriteLine("CreateCert Error: " + certificate);
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
            X509Certificate2 certificate = certificateRequest.CreateSelfSigned(DateTime.UtcNow.AddDays(-1), DateTime.UtcNow.AddYears(1));
            certificate.FriendlyName = friendlyName;
            return certificate;
        }
    }

    static X509Certificate2 CreateCertificateWithPrivateKey()
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
            X509Certificate2 certificate = certificateRequest.CreateSelfSigned(DateTime.UtcNow.AddDays(-1), DateTime.UtcNow.AddYears(1));
            certificate.FriendlyName = friendlyName;
            return certificate;
        }
    }

    static X509Certificate2 CreateCert_Cert(X509Certificate2 ori_cert)
    {
        byte[] Data = ori_cert.Export(X509ContentType.Pfx, Password);
        string path = Path.Combine(AppContext.BaseDirectory, cert_fileName);
        File.WriteAllBytes(path, Data);

        X509Certificate2 new_cert = new X509Certificate2(Data, Password);
        // X509Certificate2 new_cert = X509CertificateLoader.LoadCertificate(Data);
        Console.WriteLine("证书已导出到：" + path);
        return new_cert;
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
