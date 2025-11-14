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
    internal static partial class LinuxTcpFunc
    {
        //static int skb_gso_transport_seglen(sk_buff skb)
        //{
        //    skb_shared_info shinfo = skb_shinfo(skb);
        //    int thlen = 0;

        //    if (BoolOk(shinfo.gso_type & (SKB_GSO_TCPV4 | SKB_GSO_TCPV6)))
        //    {
        //        thlen = tcp_hdrlen(skb);
        //    }

        //    return thlen + shinfo.gso_size;
        //}

        //static int skb_gso_network_seglen(sk_buff skb)
        //{
        //    int hdr_len = skb.transport_header - skb.network_header;
        //    return hdr_len + skb_gso_transport_seglen(skb);
        //}

        //static bool skb_gso_size_check(sk_buff skb, int seg_len, int max_len)
        //{
        //    skb_shared_info shinfo = skb_shinfo(skb);
        //    if (shinfo.gso_size != GSO_BY_FRAGS)
        //    {
        //        return seg_len <= max_len;
        //    }

        //    seg_len -= GSO_BY_FRAGS;
        //    for (sk_buff iter = skb_shinfo(skb).frag_list; iter != null; iter = iter.next)
        //    {
        //        if (seg_len + skb_headlen(iter) > max_len)
        //        {
        //            return false;
        //        }
        //    }
        //    return true;

        //}

        //static bool skb_gso_validate_network_len(sk_buff skb, int mtu)
        //{
        //    return skb_gso_size_check(skb, skb_gso_network_seglen(skb), mtu);
        //}
    }
}
