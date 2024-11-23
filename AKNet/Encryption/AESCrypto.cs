using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;

namespace AKNet.Common
{
    internal class AESCrypto : NetPackageCryptoInterface
    {
        readonly byte[] ASample = Encoding.ASCII.GetBytes("123456789");
        readonly byte[] BSample = Encoding.ASCII.GetBytes("123456789");
        readonly byte[] mKeyValue = null;
        readonly byte[] mIVValue = null;

        public AESCrypto(string password1, string password2)
        {
            if (!string.IsNullOrWhiteSpace(password1))
            {
                ASample = Encoding.ASCII.GetBytes(password1);
            }

            if (!string.IsNullOrWhiteSpace(password2))
            {
                BSample = Encoding.ASCII.GetBytes(password2);
            }

            using (Aes myAes = Aes.Create())
            {
                int nIVSize = myAes.BlockSize / 8;
                int nKeySize = nIVSize;//和IV长度一样吧，其他都会报错

                byte[] KeyValue = new byte[nKeySize];
                for (int i = 0; i < KeyValue.Length; i++)
                {
                    KeyValue[i] = ASample[i % ASample.Length];
                }

                byte[] IVValue = new byte[nIVSize];
                for (int i = 0; i < IVValue.Length; i++)
                {
                    IVValue[i] = BSample[i % BSample.Length];
                }

                myAes.Key = KeyValue;
                myAes.IV = IVValue;

                mKeyValue = KeyValue;
                mIVValue = IVValue;
            }
        }

        public ReadOnlySpan<byte> Encode(ReadOnlySpan<byte> input)
        {
            return EncryptStringToBytes_Aes(input, mKeyValue, mIVValue);
        }

        public ReadOnlySpan<byte> Decode(ReadOnlySpan<byte> input)
        {
            return DecryptStringFromBytes_Aes(input, mKeyValue, mIVValue);
        }

        ReadOnlySpan<byte> EncryptStringToBytes_Aes(ReadOnlySpan<byte> plainText, byte[] Key, byte[] IV)
        {
            byte[] encrypted;
            using (Aes aesAlg = Aes.Create())
            {
                aesAlg.Key = Key;
                aesAlg.IV = IV;
                ICryptoTransform encryptor = aesAlg.CreateEncryptor(aesAlg.Key, aesAlg.IV);
                using (MemoryStream msEncrypt = new MemoryStream())
                {
                    using (CryptoStream csEncrypt = new CryptoStream(msEncrypt, encryptor, CryptoStreamMode.Write))
                    {
                        csEncrypt.Write(plainText);
                        encrypted = msEncrypt.ToArray();
                    }
                }
            }
            return encrypted;
        }

        byte[] msBuffer = new byte[16];
        ReadOnlySpan<byte> DecryptStringFromBytes_Aes(ReadOnlySpan<byte> cipherText, byte[] Key, byte[] IV)
        {
            using (Aes aesAlg = Aes.Create())
            {
                aesAlg.Key = Key;
                aesAlg.IV = IV;

                ICryptoTransform decryptor = aesAlg.CreateDecryptor(aesAlg.Key, aesAlg.IV);
                using (MemoryStream msDecrypt = new MemoryStream(msBuffer))
                {
                    msDecrypt.Write(cipherText);
                    using (CryptoStream csDecrypt = new CryptoStream(msDecrypt, decryptor, CryptoStreamMode.Read))
                    {
                        // 读取字节
                        byte[] buffer = new byte[msDecrypt.Length];
                        int bytesRead = csDecrypt.Read(buffer, 0, buffer.Length);
                        return new ReadOnlySpan<byte>(buffer, 0, bytesRead);
                    }
                }
            }
        }
    }
}