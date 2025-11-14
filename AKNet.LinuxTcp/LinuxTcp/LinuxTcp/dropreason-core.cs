/************************************Copyright*****************************************
*        ProjectName:AKNet
*        Web:https://github.com/825126369/AKNet
*        Description:C#游戏网络库
*        Author:许珂
*        ModifyTime:2025/11/14 8:44:40
*        Copyright:MIT软件许可证
************************************Copyright*****************************************/
namespace AKNet.LinuxTcp.Common
{
    internal static class skb_drop_reason
    {
        public const int SKB_NOT_DROPPED_YET = 0;
        public const int SKB_CONSUMED = 1;
        public const int SKB_DROP_REASON_NOT_SPECIFIED = 2;
        public const int SKB_DROP_REASON_NO_SOCKET = 3;
        public const int SKB_DROP_REASON_PKT_TOO_SMALL = 4;
        public const int SKB_DROP_REASON_TCP_CSUM = 5;
        public const int SKB_DROP_REASON_SOCKET_FILTER = 6;
        public const int SKB_DROP_REASON_UDP_CSUM = 7;
        public const int SKB_DROP_REASON_NETFILTER_DROP = 8;
        public const int SKB_DROP_REASON_OTHERHOST = 9;
        public const int SKB_DROP_REASON_IP_CSUM = 10;
        public const int SKB_DROP_REASON_IP_INHDR = 11;
        public const int SKB_DROP_REASON_IP_RPFILTER = 12;
        public const int SKB_DROP_REASON_UNICAST_IN_L2_MULTICAST = 13;
        public const int SKB_DROP_REASON_XFRM_POLICY = 14;
        public const int SKB_DROP_REASON_IP_NOPROTO = 15;
        public const int SKB_DROP_REASON_SOCKET_RCVBUFF = 16;
        public const int SKB_DROP_REASON_PROTO_MEM = 17;
        public const int SKB_DROP_REASON_TCP_AUTH_HDR = 18;
        public const int SKB_DROP_REASON_TCP_MD5NOTFOUND = 19;
        public const int SKB_DROP_REASON_TCP_MD5UNEXPECTED = 20;
        public const int SKB_DROP_REASON_TCP_MD5FAILURE = 21;
        public const int SKB_DROP_REASON_TCP_AONOTFOUND = 22;
        public const int SKB_DROP_REASON_TCP_AOUNEXPECTED = 23;
        public const int SKB_DROP_REASON_TCP_AOKEYNOTFOUND = 24;
        public const int SKB_DROP_REASON_TCP_AOFAILURE = 25;
        public const int SKB_DROP_REASON_SOCKET_BACKLOG = 26;
        public const int SKB_DROP_REASON_TCP_FLAGS = 27;
        public const int SKB_DROP_REASON_TCP_ABORT_ON_DATA = 28;
        public const int SKB_DROP_REASON_TCP_ZEROWINDOW = 29;
        public const int SKB_DROP_REASON_TCP_OLD_DATA = 30;
        public const int SKB_DROP_REASON_TCP_OVERWINDOW = 31;
        public const int SKB_DROP_REASON_TCP_OFOMERGE = 32;
        public const int SKB_DROP_REASON_TCP_RFC7323_PAWS = 33;
        public const int SKB_DROP_REASON_TCP_OLD_SEQUENCE = 34;
        public const int SKB_DROP_REASON_TCP_INVALID_SEQUENCE = 35;
        public const int SKB_DROP_REASON_TCP_INVALID_ACK_SEQUENCE = 36;
        public const int SKB_DROP_REASON_TCP_RESET = 37;
        public const int SKB_DROP_REASON_TCP_INVALID_SYN = 38;
        public const int SKB_DROP_REASON_TCP_CLOSE = 39;
        public const int SKB_DROP_REASON_TCP_FASTOPEN = 40;
        public const int SKB_DROP_REASON_TCP_OLD_ACK = 41;
        public const int SKB_DROP_REASON_TCP_TOO_OLD_ACK = 42;
        public const int SKB_DROP_REASON_TCP_ACK_UNSENT_DATA = 43;
        public const int SKB_DROP_REASON_TCP_OFO_QUEUE_PRUNE = 44;
        public const int SKB_DROP_REASON_TCP_OFO_DROP = 45;
        public const int SKB_DROP_REASON_IP_OUTNOROUTES = 46;
        public const int SKB_DROP_REASON_BPF_CGROUP_EGRESS = 47;
        public const int SKB_DROP_REASON_IPV6DISABLED = 48;
        public const int SKB_DROP_REASON_NEIGH_CREATEFAIL = 49;
        public const int SKB_DROP_REASON_NEIGH_FAILED = 50;
        public const int SKB_DROP_REASON_NEIGH_QUEUEFULL = 51;
        public const int SKB_DROP_REASON_NEIGH_DEAD = 52;
        public const int SKB_DROP_REASON_TC_EGRESS = 53;
        public const int SKB_DROP_REASON_SECURITY_HOOK = 54;
        public const int SKB_DROP_REASON_QDISC_DROP = 55;
        public const int SKB_DROP_REASON_CPU_BACKLOG = 56;
        public const int SKB_DROP_REASON_XDP = 57;
        public const int SKB_DROP_REASON_TC_INGRESS = 58;
        public const int SKB_DROP_REASON_UNHANDLED_PROTO = 59;
        public const int SKB_DROP_REASON_SKB_CSUM = 60;
        public const int SKB_DROP_REASON_SKB_GSO_SEG = 61;
        public const int SKB_DROP_REASON_SKB_UCOPY_FAULT = 62;
        public const int SKB_DROP_REASON_DEV_HDR = 63;
        public const int SKB_DROP_REASON_DEV_READY = 64;
        public const int SKB_DROP_REASON_FULL_RING = 65;
        public const int SKB_DROP_REASON_NOMEM = 66;
        public const int SKB_DROP_REASON_HDR_TRUNC = 67;
        public const int SKB_DROP_REASON_TAP_FILTER = 68;
        public const int SKB_DROP_REASON_TAP_TXFILTER = 69;
        public const int SKB_DROP_REASON_ICMP_CSUM = 70;
        public const int SKB_DROP_REASON_INVALID_PROTO = 71;
        public const int SKB_DROP_REASON_IP_INADDRERRORS = 72;
        public const int SKB_DROP_REASON_IP_INNOROUTES = 73;
        public const int SKB_DROP_REASON_IP_LOCAL_SOURCE = 74;
        public const int SKB_DROP_REASON_IP_INVALID_SOURCE = 75;
        public const int SKB_DROP_REASON_IP_LOCALNET = 76;
        public const int SKB_DROP_REASON_IP_INVALID_DEST = 77;
        public const int SKB_DROP_REASON_PKT_TOO_BIG = 78;
        public const int SKB_DROP_REASON_DUP_FRAG = 79;
        public const int SKB_DROP_REASON_FRAG_REASM_TIMEOUT = 80;
        public const int SKB_DROP_REASON_FRAG_TOO_FAR = 81;
        public const int SKB_DROP_REASON_TCP_MINTTL = 82;
        public const int SKB_DROP_REASON_IPV6_BAD_EXTHDR = 83;
        public const int SKB_DROP_REASON_IPV6_NDISC_FRAG = 84;
        public const int SKB_DROP_REASON_IPV6_NDISC_HOP_LIMIT = 85;
        public const int SKB_DROP_REASON_IPV6_NDISC_BAD_CODE = 86;
        public const int SKB_DROP_REASON_IPV6_NDISC_BAD_OPTIONS = 87;
        public const int SKB_DROP_REASON_IPV6_NDISC_NS_OTHERHOST = 88;
        public const int SKB_DROP_REASON_QUEUE_PURGE = 89;
        public const int SKB_DROP_REASON_TC_COOKIE_ERROR = 90;
        public const int SKB_DROP_REASON_PACKET_SOCK_ERROR = 91;
        public const int SKB_DROP_REASON_TC_CHAIN_NOTFOUND = 92;
        public const int SKB_DROP_REASON_TC_RECLASSIFY_LOOP = 93;
        public const int SKB_DROP_REASON_VXLAN_INVALID_HDR = 94;
        public const int SKB_DROP_REASON_VXLAN_VNI_NOT_FOUND = 95;
        public const int SKB_DROP_REASON_MAC_INVALID_SOURCE = 96;
        public const int SKB_DROP_REASON_VXLAN_ENTRY_EXISTS = 97;
        public const int SKB_DROP_REASON_VXLAN_NO_REMOTE = 98;
        public const int SKB_DROP_REASON_IP_TUNNEL_ECN = 99;
        public const int SKB_DROP_REASON_TUNNEL_TXINFO = 100;
        public const int SKB_DROP_REASON_LOCAL_MAC = 101;
        public const int SKB_DROP_REASON_ARP_PVLAN_DISABLE = 102;
        public const int SKB_DROP_REASON_MAX = 103;
    }
}
