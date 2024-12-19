using System;
using System.Collections.Generic;

namespace AKNet.LinuxTcp
{
    internal class sock
    {
        public int sk_err;
        public int sk_err_soft;

        public LinkedList<sk_buff> sk_send_head;
        public LinkedList<sk_buff> sk_write_queue;
        public AkRBTree<sk_buff> tcp_rtx_queue;

        public net sk_net;
        public ulong sk_flags;
        public uint sk_txhash;
        public int sk_refcnt;

        public sk_family sk_family;

        //sk_sndbuf 是 Linux 内核中 struct sock（套接字结构体）的一个成员变量，用于定义套接字的发送缓冲区大小。
        //这个参数控制了应用程序可以一次性写入套接字的最大数据量，并且对 TCP 连接的性能和行为有重要影响。
        public int sk_sndbuf;
    }

    public class ip_options
    {
        public int faddr; //描述：最终目的地址（Final Address）。当使用源路由选项时，这个字段表示数据包的最终目标地址。
        public int nexthop; //描述：下一跳地址。当使用源路由选项时，这个字段表示下一个中间节点的地址。
        public byte optlen; //描述：选项部分的总长度（以字节为单位），包括所有选项和填充字节。
        public byte srr; //描述：源路由记录（Source Route Record）的长度。如果设置了源路由选项，则此字段表示源路由记录的长度。
        public byte rr; //描述：记录路由（Record Route）选项的长度。如果设置了记录路由选项，则此字段表示记录路由的长度。
        public byte ts; //描述：时间戳（Timestamp）选项的长度。如果设置了时间戳选项，则此字段表示时间戳的长度。
        public byte is_strictroute;//位域，表示是否使用严格源路由（Strict Source Route）。如果设置为 1，则必须按照指定路径中的每个路由器进行转发；如果为 0，则允许某些路由器绕过。
        public byte srr_is_hit; //位域，表示当前节点是否是源路由中的一个点。如果是，则设置为 1。
        public byte is_changed;//位域，表示 IP 选项是否被修改过。这对于确保选项的一致性和安全性非常重要
        public byte rr_needaddr; //位域，表示记录路由选项是否需要添加当前节点的地址。
        public byte ts_needtime;//描述：位域，表示时间戳选项是否需要添加当前时间。
        public byte ts_needaddr;//位域，表示时间戳选项是否需要添加当前节点的地址。
        public byte router_alert;//描述：路由器告警（Router Alert）选项的值。这个选项用于通知沿途的路由器对特定的数据包进行特殊处理。
        public byte cipso;//描述：CIPSO（Commercial IP Security Option）选项的长度。CIPSO 提供了一种方法来标记 IP 数据包的安全级别。
        public byte __pad2;//描述：填充字段，用于对齐或保留未来扩展。
        public byte[] __data;//描述：可变长度数组，用于存储实际的 IP 选项数据。不同的选项类型有不同的格式和长度。
    }

    internal static partial class LinuxTcpFunc
    {
        public static net sock_net(sock sk)
        {
            return sk.sk_net;
        }

        public static bool sock_flag(sock sk, sock_flags flag)
        {
	        return ((ulong)flag & sk.sk_flags) > 0;
        }

        public static void __sock_put(sock sk)
        {
            sk.sk_refcnt--;
        }

        public static void sk_reset_timer(sock sk, HRTimer timer, long expires)
        {
            timer.ModTimer(TimeSpan.FromMilliseconds(expires));
        }
    }
}
