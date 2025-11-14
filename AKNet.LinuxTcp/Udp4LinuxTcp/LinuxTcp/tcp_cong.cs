/************************************Copyright*****************************************
*        ProjectName:AKNet
*        Web:https://github.com/825126369/AKNet
*        Description:C#游戏网络库
*        Author:许珂
*        ModifyTime:2025/11/14 8:44:41
*        Copyright:MIT软件许可证
************************************Copyright*****************************************/
using AKNet.Common;
using System;
using System.Collections.Generic;

namespace AKNet.Udp4LinuxTcp.Common
{
    internal static partial class LinuxTcpFunc
    {
        static LinkedList<tcp_congestion_ops> tcp_cong_list = new LinkedList<tcp_congestion_ops>();

        static int tcp_validate_congestion_control(tcp_congestion_ops ca)
        {
	        if (ca.ssthresh == null || ca.undo_cwnd == null || !(ca.cong_avoid != null || ca.cong_control != null)) 
            {
		        NetLog.LogError($"{ca.name} does not implement required ops\n");
		        return -ErrorCode.EINVAL;
	        }
	        return 0;
        }

        static tcp_congestion_ops tcp_ca_find_key(string key)
        {
            foreach(var v in tcp_cong_list)
            {
                if(v.name == key)
                {
                    return v;
                }
            }

	        return null;
        }

        static int tcp_register_congestion_control(tcp_congestion_ops ca)
        {
            int ret = tcp_validate_congestion_control(ca);
            if (ret != 0)
            {
                return ret;
            }

            if (tcp_ca_find_key(ca.name) != null) 
            {
                ret = -ErrorCode.EEXIST;
            } 
            else 
            {
                tcp_cong_list.AddLast(ca);
            }

            return ret;
        }

        public static void tcp_set_ca_state(tcp_sock tp, tcp_ca_state ca_state)
        {
            if (tp.icsk_ca_ops.set_state != null)
            {
                tp.icsk_ca_ops.set_state(tp, ca_state);
            }

            tp.icsk_ca_state = (byte)ca_state;
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
