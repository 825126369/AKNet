/************************************Copyright*****************************************
*        ProjectName:AKNet
*        Web:https://github.com/825126369/AKNet
*        Description:C#游戏网络库
*        Author:许珂
*        StartTime:2024/11/01 00:00:00
*        ModifyTime:2026/2/1 20:26:44
*        Copyright:MIT软件许可证
************************************Copyright*****************************************/
using System;

namespace AKNet.Common
{
    internal static class XORCrypto
    {
        static readonly byte[] key = new byte[8];
        static XORCrypto()
        {
            EndianBitConverter.SetBytes(key, 0, TimeTool.GetTimeStamp(VersionPublishConfig.m_BuildTime));
            //VersionPublishConfig.GetBuildTimeStr() 在不同的应用上，虽然DateTime 一样，但转为ToString() 后，字符串不一样。
            //NetLogHelper.PrintByteArray("XORCrypto Key: ", key);
        }

        public static byte Encode(int i, byte input, byte token)
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
