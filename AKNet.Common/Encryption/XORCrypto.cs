/************************************Copyright*****************************************
*        ProjectName:AKNet
*        Web:https://github.com/825126369/AKNet
*        Description:这是一个面向 .Net Standard 2.1 的游戏网络库
*        Author:阿珂
*        ModifyTime:2025/2/27 22:28:11
*        Copyright:MIT软件许可证
************************************Copyright*****************************************/
using System;
using System.Security.Cryptography;
using System.Text;

namespace AKNet.Common
{
    internal class XORCrypto
    {
        readonly byte[] key = new byte[64];
        public XORCrypto(string password)
        {
            RandomNumberGenerator.Fill(key);
            if (!string.IsNullOrWhiteSpace(password))
            {
                key = Encoding.ASCII.GetBytes(password);
            }
        }

        public byte Encode(int i, byte input, byte token)
        {
            if (i % 2 == 0)
            {
                int nIndex = Math.Abs(i) % key.Length;
                return (byte)(input ^ key[nIndex] ^ token);
            }
            else
            {
                int nIndex = Math.Abs(key.Length - i) % key.Length;
                return (byte)(input ^ key[nIndex] ^ token);
            }
        }
    }
}
