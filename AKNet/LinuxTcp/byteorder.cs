/************************************Copyright*****************************************
*        ProjectName:AKNet
*        Web:https://github.com/825126369/AKNet
*        Description:这是一个面向 .Net Standard 2.1 的游戏网络库
*        Author:阿珂
*        CreateTime:2024/12/28 16:38:23
*        Copyright:MIT软件许可证
************************************Copyright*****************************************/
using System;

namespace AKNet.LinuxTcp
{
    internal static partial class LinuxTcpFunc
    {
        // 将 uint 从主机字节序转换为网络字节序 (大端)
        public static ulong hton(ulong host)
        {
            if (BitConverter.IsLittleEndian)
            {
                byte[] bytes = BitConverter.GetBytes(host);
                Array.Reverse(bytes);
                return BitConverter.ToUInt32(bytes, 0);
            }
            else
            {
                return host;
            }
        }

        // 将 uint 从网络字节序 (大端) 转换为主机字节序
        public static ulong ntoh(ulong network)
        {
            if (BitConverter.IsLittleEndian)
            {
                byte[] bytes = BitConverter.GetBytes(network);
                Array.Reverse(bytes);
                return BitConverter.ToUInt32(bytes, 0);
            }
            else
            {
                return network;
            }
        }
    }
}
