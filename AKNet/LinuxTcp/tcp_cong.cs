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
        public static void tcp_set_ca_state(tcp_sock tp, tcp_ca_state ca_state)
        {
            if (tp.icsk_ca_ops.set_state != null)
            {
                tp.icsk_ca_ops.set_state(tp, ca_state);
                tp.icsk_ca_state = (byte)ca_state;
            }
        }
    }
}
