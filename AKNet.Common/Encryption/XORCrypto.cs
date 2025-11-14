/************************************Copyright*****************************************
*        ProjectName:AKNet
*        Web:https://github.com/825126369/AKNet
*        Description:C#游戏网络库
*        Author:许珂
*        StartTime:2024/11/01 00:00:00
*        ModifyTime:2025/11/14 8:56:43
*        Copyright:MIT软件许可证
************************************Copyright*****************************************/
using System;
using System.Text;

namespace AKNet.Common
{
    internal class XORCrypto
    {
        readonly string default_password = "qwertyuiopasd";
        readonly byte[] key = new byte[64];
        public XORCrypto(string password = null)
        {
            if (!string.IsNullOrWhiteSpace(password))
            {
                key = Encoding.ASCII.GetBytes(password);
            }
            else
            {
                key = Encoding.ASCII.GetBytes(default_password);
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
