/************************************Copyright*****************************************
*        ProjectName:AKNet
*        Web:https://github.com/825126369/AKNet
*        Description:C#游戏网络库
*        Author:许珂
*        ModifyTime:2025/11/14 8:44:41
*        Copyright:MIT软件许可证
************************************Copyright*****************************************/
namespace AKNet.Udp4LinuxTcp.Common
{
    internal class request_sock : sock_common
    {
        public ushort mss;
        public byte num_retrans;
        public long ts_recent;
        public long timeout;
        public byte num_timeout;
    }

    internal class inet_request_sock : request_sock
    {
        public ushort snd_wscale;
        public ushort rcv_wscale;
        public ushort tstamp_ok;
        public ushort sack_ok;
        public ushort wscale_ok;
        public ushort ecn_ok;
        public ushort acked;
        public ushort no_srccheck;
        public ushort smc_ok;
    }

    internal class tcp_request_sock_ops
    {
        public ushort mss_clamp;
    }

    internal class tcp_request_sock : inet_request_sock
    {
        public tcp_request_sock_ops af_specific;
        public long snt_synack;
        public bool tfo_listener;
        public bool is_mptcp;
        public bool req_usec_ts;
        public bool drop_req;
        public uint txhash;
        public uint rcv_isn;
        public uint snt_isn;
        public uint ts_off;
        public long last_oow_ack_time;
        public uint rcv_nxt;
        public byte syn_tos;
        public byte ao_keyid;
        public byte ao_rcv_next;
        public bool used_tcp_ao;
    }

}
