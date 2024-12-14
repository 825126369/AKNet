namespace AKNet.LinuxTcp
{
    internal static partial class LinuxTcpFunc
    {
        public static bool tcp_write_queue_empty(tcp_sock tp)
        {
	        return tp.write_seq == tp.snd_nxt;
        }

        public static bool tcp_rtx_queue_empty(tcp_sock tp)
        {
            return tp.tcp_rtx_queue.rb_node == null;
        }

        public static bool tcp_rtx_and_write_queues_empty(tcp_sock tp)
        {
	        return tcp_rtx_queue_empty(tp) && tcp_write_queue_empty(tp);
        }

        public static void tcp_write_queue_purge(tcp_sock tp)
        {
	        sk_buff skb;

	        tcp_chrono_stop(tp, tcp_chrono.TCP_CHRONO_BUSY);
	        while ((skb = __skb_dequeue(tp.sk_write_queue)) != null) 
            {
		        tcp_skb_tsorted_anchor_cleanup(skb);
                tcp_wmem_free_skb(sk, skb);
            }

            tcp_rtx_queue_purge(sk);
            INIT_LIST_HEAD(&tcp_sk(sk)->tsorted_sent_queue);
            tcp_clear_all_retrans_hints(tcp_sk(sk));
            tcp_sk(sk)->packets_out = 0;
	        inet_csk(sk)->icsk_backoff = 0;
        }
    }
}
