/************************************Copyright*****************************************
*        ProjectName:AKNet
*        Web:https://github.com/825126369/AKNet
*        Description:这是一个面向 .Net Standard 2.1 的游戏网络库
*        Author:阿珂
*        CreateTime:2024/12/28 16:38:23
*        Copyright:MIT软件许可证
************************************Copyright*****************************************/
using System;
using System.Collections.Generic;
using System.Text;

namespace AKNet.LinuxTcp
{
    internal class tcpvegas_info
    {
        public uint tcpv_enabled;
        public uint tcpv_rttcnt;
        public uint tcpv_rtt;
        public uint tcpv_minrtt;
    }

    internal class tcp_dctcp_info
    {
        public ushort dctcp_enabled;
        public ushort dctcp_ce_state;
        public uint dctcp_alpha;
        public uint dctcp_ab_ecn;
        public uint dctcp_ab_tot;
    }

    internal class tcp_bbr_info
    {
        /* u64 bw: max-filtered BW (app throughput) estimate in Byte per sec: */
        public uint bbr_bw_lo;        /* lower 32 bits of bw */
        public uint bbr_bw_hi;        /* upper 32 bits of bw */
        public uint bbr_min_rtt;      /* min-filtered RTT in uSec */
        public uint bbr_pacing_gain;  /* pacing gain shifted left 8 bits */
        public uint bbr_cwnd_gain;        /* cwnd gain shifted left 8 bits */
    }

    internal class tcp_cc_info
    {
        public tcpvegas_info    vegas;
        public tcp_dctcp_info   dctcp;
        public tcp_bbr_info bbr;
    }
}
