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
        public static void tcp_set_ca_state(tcp_sock tp, tcp_ca_state ca_state)
        {
            if (tp.icsk_ca_ops.set_state != null)
            {
                tp.icsk_ca_ops.set_state(tp, ca_state);
                tp.icsk_ca_state = (byte)ca_state;
            }
        }

        static void tcp_init_congestion_control(tcp_sock tp)
        {
            tp.prior_ssthresh = 0;
            if (tp.icsk_ca_ops.init != null)
            {
                tp.icsk_ca_ops.init(tp);
            }

            if (tcp_ca_needs_ecn(tp))
            {
                INET_ECN_xmit(tp);
            }
            else
            {
                INET_ECN_dontxmit(tp);
            }

            tp.icsk_ca_initialized = true;
        }

        static void tcp_assign_congestion_control(tcp_sock tp)
        {
            net net = sock_net(tp);
            tcp_congestion_ops ca = net.ipv4.tcp_congestion_control;
            if (ca == null)
            {
                ca = tcp_reno;
            }

            tp.icsk_ca_ops = ca;
            Array.Clear(tp.icsk_ca_priv, 0, tp.icsk_ca_priv.Length);

            if (BoolOk(ca.flags & TCP_CONG_NEEDS_ECN))
            {
                INET_ECN_xmit(tp);
            }
            else
            {
                INET_ECN_dontxmit(tp);
            }
        }

    }
}
