/************************************Copyright*****************************************
*        ProjectName:AKNet
*        Web:https://github.com/825126369/AKNet
*        Description:这是一个面向 .Net Standard 2.1 的游戏网络库
*        Author:阿珂
*        CreateTime:2024/12/28 16:38:23
*        Copyright:MIT软件许可证
************************************Copyright*****************************************/
using System;
using System.Text;

namespace AKNet.Common
{
    internal class XORCrypto
    {
        readonly byte[] key = Encoding.ASCII.GetBytes("2024/11/23");
        public XORCrypto(string password)
        {
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
