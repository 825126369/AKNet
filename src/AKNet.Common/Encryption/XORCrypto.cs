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
using System.Text;

namespace AKNet.Common
{
    internal static class XORCrypto
    {
        static readonly byte[] key = null;
        static XORCrypto()
        {
            //时间戳也不行。因为不同时区，相同的 DateTime, 返回的时间戳不一样。
            //直接 m_BuildTime.ToString() 也不行。在不同的应用上，虽然DateTime 一样，但转为ToString() 后，字符串不一样。
            if (VersionPublishConfig.m_BuildTime.Day % 2 == 1)
            {
                //具体化格式化字符串
                string t = VersionPublishConfig.m_BuildTime.ToString("yyyy/MM/dd HH:mm:ss");
                key = new byte[t.Length];
                EndianBitConverter.SetBytes(key, 0, t);
            }
            else
            {
                //不使用时间戳
                key = new byte[8];
                var mTimeSpan = VersionPublishConfig.m_BuildTime - DateTime.MinValue;
                EndianBitConverter.SetBytes(key, 0, (long)mTimeSpan.TotalMilliseconds);
            }
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
