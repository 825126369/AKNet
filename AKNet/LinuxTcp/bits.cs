﻿/************************************Copyright*****************************************
*        ProjectName:AKNet
*        Web:https://github.com/825126369/AKNet
*        Description:这是一个面向 .Net Standard 2.1 的游戏网络库
*        Author:阿珂
*        CreateTime:2024/12/28 16:38:23
*        Copyright:MIT软件许可证
************************************Copyright*****************************************/
namespace AKNet.LinuxTcp
{
    internal static partial class LinuxTcpFunc
    {
        public static ulong BIT(int nr)
        {
            return (ulong)(1 << nr);
        }

        public static void set_bit(byte m, ulong mm)
        {
            mm =  (mm | (ulong)1 << m);
        }

        public static bool BoolOk(long nr)
        {
            return nr > 0;
        }

        public static bool BoolOk(ulong nr)
        {
            return nr > 0;
        }
    }
}
