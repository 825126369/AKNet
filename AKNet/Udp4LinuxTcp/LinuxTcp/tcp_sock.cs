/************************************Copyright*****************************************
*        ProjectName:AKNet
*        Web:https://github.com/825126369/AKNet
*        Description:这是一个面向 .Net Standard 2.1 的游戏网络库
*        Author:阿珂
*        CreateTime:2024/12/28 16:38:23
*        Copyright:MIT软件许可证
************************************Copyright*****************************************/
using AKNet.Common;
using System;

namespace AKNet.Udp4LinuxTcp.Common
{
    internal class tcp_rack
    {
        public long mstamp; //记录了数据段（skb）被（重新）发送的时间戳
        public long rtt_us;  //关联的往返时间（RTT），以微秒为单位
        public uint end_seq; //数据段的结束序列号
        public uint last_delivered; //上次调整重排序窗口时的 tp->delivered 值。tp->delivered 是一个统计量，表示已成功传递给上层应用的数据量。这有助于评估重排序窗口的有效性。
        public byte reo_wnd_steps;  //允许的重排序窗口大小。重排序窗口定义了在认为数据段丢失之前可以容忍的最大乱序程度。
        public byte reo_wnd_persist; //自上次调整以来进入恢复状态的次数。这是一个位域，占用5位，因此可以表示0到31之间的值。它用于追踪重排序窗口调整后的恢复频率。
        public byte dsack_seen; //标志位，表示自从上次调整重排序窗口后是否看到了 DSACK（选择性确认重复数据段）。DSACK 提供了关于哪些数据段已经被重复确认的信息，这对改进 RACK 的行为非常有用。
        public byte advanced;   //标志位，表示自上次标记丢失以来 mstamp 是否已经前进。如果 mstamp 已经更新，则表明有新的数据段被发送或确认，这对于决定何时进行进一步的丢失检测是重要的.
    }

    internal class mtu_probe
    {
        public uint probe_seq_start;
        public uint probe_seq_end;
    }

    internal class tcp_sack_block_wire
    {
        public uint start_seq;
        public uint end_seq;
    }

    internal class tcp_sack_block
    {
        public uint start_seq;
        public uint end_seq;
    }

    internal class tcp_sacktag_state
    {
        public long first_sackt; //表示最早未被重传但已被SACKed（选择性确认）的数据段的时间戳
        public long last_sackt; //表示最晚未被重传但已被SACKed的数据段的时间戳。
        public uint reord; //乱序阈值，用于判断数据包是否乱序。如果数据包的序列号与预期的序列号相差超过这个阈值，则认为数据包乱序
        public uint sack_delivered;//记录通过 SACK 确认的字节数。这个值用于更新拥塞控制状态。
        public int flag;//标志位，用于记录各种状态信息。例如，FLAG_SACK_RENO 表示是否使用 SACK 算法，FLAG_LOST_RETRANS 表示是否有重传的数据包丢失。
        public uint mss_now;    //当前的 MSS（最大报文段长度），用于计算数据包的大小。
        public rate_sample rate;//指向 rate_sample 结构体的指针，用于速率采样。这个结构体包含速率采样的相关数据，用于拥塞控制。
    }

    internal class tcphdr
    {
        public ushort source;
        public ushort dest;
        public uint seq;
        public uint ack_seq;

        public byte doff;//TCP 头部的实际长度（以字节为单位）。
        public byte res1;

        public byte cwr;
        public byte ece;
        public byte urg;
        public byte ack;
        public byte psh;
        public byte rst;
        public byte syn;
        public byte fin;

        public ushort window; //接收窗口
        public ushort check;
        public ushort urg_ptr;

        public byte commandId;
        public ushort tot_len; //Buffer总长度

        public byte tcp_flags
        {
            set
            {
                cwr = (byte)(LinuxTcpFunc.BoolOk(value & LinuxTcpFunc.TCPHDR_CWR) || LinuxTcpFunc.BoolOk(cwr) ? 1 : 0);
                ece = (byte)(LinuxTcpFunc.BoolOk(value & LinuxTcpFunc.TCPHDR_ECE) || LinuxTcpFunc.BoolOk(ece) ? 1 : 0);
                urg = (byte)(LinuxTcpFunc.BoolOk(value & LinuxTcpFunc.TCPHDR_URG) || LinuxTcpFunc.BoolOk(urg) ? 1 : 0);
                ack = (byte)(LinuxTcpFunc.BoolOk(value & LinuxTcpFunc.TCPHDR_ACK) || LinuxTcpFunc.BoolOk(ack) ? 1 : 0);
                psh = (byte)(LinuxTcpFunc.BoolOk(value & LinuxTcpFunc.TCPHDR_PSH) || LinuxTcpFunc.BoolOk(psh) ? 1 : 0);
                rst = (byte)(LinuxTcpFunc.BoolOk(value & LinuxTcpFunc.TCPHDR_RST) || LinuxTcpFunc.BoolOk(rst) ? 1 : 0);
                syn = (byte)(LinuxTcpFunc.BoolOk(value & LinuxTcpFunc.TCPHDR_SYN) || LinuxTcpFunc.BoolOk(syn) ? 1 : 0);
                fin = (byte)(LinuxTcpFunc.BoolOk(value & LinuxTcpFunc.TCPHDR_FIN) || LinuxTcpFunc.BoolOk(fin) ? 1 : 0);
            }

            get
            {
                return (byte)(((byte)cwr) << 7 |
                      ((byte)ece) << 6 |
                      ((byte)urg) << 5 |
                      ((byte)ack) << 4 |
                      ((byte)psh) << 3 |
                      ((byte)rst) << 2 |
                      ((byte)syn) << 1 |
                      ((byte)fin) << 0);
            }
        }
            
        public void WriteTo(sk_buff skb)
        {
            WriteTo(skb.mBuffer.AsSpan().Slice(skb.nBufferOffset));
        }

        public void WriteFrom(sk_buff skb)
        {
            WriteFrom(skb.mBuffer.AsSpan().Slice(skb.nBufferOffset));
        }

        public void WriteTo(Span<byte> mBuffer)
        {
            EndianBitConverter.SetBytes(mBuffer, 0, source);
            EndianBitConverter.SetBytes(mBuffer, 2, dest);
            EndianBitConverter.SetBytes(mBuffer, 4, seq);
            EndianBitConverter.SetBytes(mBuffer, 8, ack_seq);

            mBuffer[12] = doff;
            mBuffer[13] = (byte)(
                ((byte)cwr) << 7 |
                ((byte)ece) << 6 |
                ((byte)urg) << 5 |
                ((byte)ack) << 4 |
                ((byte)psh) << 3 |
                ((byte)rst) << 2 |
                ((byte)syn) << 1 |
                ((byte)fin) << 0
            );

            EndianBitConverter.SetBytes(mBuffer, 14, window);
            EndianBitConverter.SetBytes(mBuffer, 16, check);
            EndianBitConverter.SetBytes(mBuffer, 18, urg_ptr);

            mBuffer[20] = commandId;
            EndianBitConverter.SetBytes(mBuffer, 21, tot_len);

            //NetLogHelper.PrintByteArray("WriteTo: ", mBuffer.Slice(0, LinuxTcpFunc.sizeof_tcphdr));
        }

        public void WriteFrom(ReadOnlySpan<byte> mBuffer)
        {
            source = EndianBitConverter.ToUInt16(mBuffer, 0);
            dest = EndianBitConverter.ToUInt16(mBuffer, 2);
            seq = EndianBitConverter.ToUInt32(mBuffer, 4);
            ack_seq = EndianBitConverter.ToUInt32(mBuffer, 8);

            doff = mBuffer[12];

            cwr = (byte)((mBuffer[13] & LinuxTcpFunc.TCPHDR_CWR) > 0 ? 1 : 0);
            ece = (byte)((mBuffer[13] & LinuxTcpFunc.TCPHDR_ECE) > 0 ? 1 : 0);
            urg = (byte)((mBuffer[13] & LinuxTcpFunc.TCPHDR_URG) > 0 ? 1 : 0);
            ack = (byte)((mBuffer[13] & LinuxTcpFunc.TCPHDR_ACK) > 0 ? 1 : 0);
            psh = (byte)((mBuffer[13] & LinuxTcpFunc.TCPHDR_PSH) > 0 ? 1 : 0);
            rst = (byte)((mBuffer[13] & LinuxTcpFunc.TCPHDR_RST) > 0 ? 1 : 0);
            syn = (byte)((mBuffer[13] & LinuxTcpFunc.TCPHDR_SYN) > 0 ? 1 : 0);
            fin = (byte)((mBuffer[13] & LinuxTcpFunc.TCPHDR_FIN) > 0 ? 1 : 0);

            window = EndianBitConverter.ToUInt16(mBuffer, 14);
            check = EndianBitConverter.ToUInt16(mBuffer, 16);
            urg_ptr = EndianBitConverter.ToUInt16(mBuffer, 18);

            commandId = mBuffer[20];
            tot_len = EndianBitConverter.ToUInt16(mBuffer, 21);

            //NetLogHelper.PrintByteArray("WriteFrom: ", mBuffer.Slice(0, LinuxTcpFunc.sizeof_tcphdr));
        }

    }

    internal class tcp_sock : inet_connection_sock
    {
        public UdpClientPeerCommonBase mClientPeer;

        public uint max_window;//

        //这个字段用于跟踪已经通过套接字发送给应用层的数据序列号（sequence number）。具体来说，pushed_seq 表示最近一次调用 tcp_push() 或类似函数后，
        //TCP 层认为应该被“推送”到网络上的数据的最后一个字节的序列号加一。
        //记录了最后一个被标记为推送（PUSH）的数据包的序列号。这个变量用于跟踪已经发送但尚未被确认的数据的边界
        public uint pushed_seq;
        public uint write_seq;  //应用程序通过 send() 或 write() 系统调用写入到TCP套接字中的最后一个字节的序列号。
        public uint rtt_seq;
        public uint snd_nxt;    //Tcp层 下一个将要发送的数据段的第一个字节的序列号。
        public uint snd_una;//表示未被确认的数据段的第一个字节的序列号。
        public uint mss_cache;  //单个数据包的最大大小

        //tcp_update_wl 是 Linux 内核 TCP 协议栈中的一个内部函数，用于更新所谓的“窗口左右边界”（Window Left, wl），
        //这在TCP的拥塞控制和流量控制机制中起着重要作用。
        //该函数通常用于确保TCP接收窗口的正确管理和维护，以优化数据传输效率并避免网络拥塞。
        //窗口左右边界的含义
        //Window Left(wl)：指的是接收方已经接收到的数据序列号中最小的一个未确认的数据包的序列号。
        //简单来说，它是接收窗口的左边界，表示下一个期望从发送方接收到的数据包的序列号。
        //Window Right：则表示接收窗口的右边界，即接收方愿意接收的最大序列号。它等于 wl + 接收窗口大小。
        //tcp_update_wl 函数的主要任务是根据传入的参数 seq 来更新 tp（指向 tcp_sock 结构体的指针）中的窗口左边界。具体来说：
        public uint snd_wl1;	/* Sequence for window update		*/
        public uint snd_wnd;    //发送窗口的大小
        public uint snd_cwnd;   //拥塞窗口的大小, 表示当前允许发送方发送的最大数据段 数量

        //记录了应用程序已经从接收缓冲区读取的数据的最后一个字节的序列号（seq）加一，即下一个期待被用户空间读取的数据的起始序列号
        //用于跟踪已经复制到用户空间的数据的序列号
        public uint copied_seq;

        public uint snd_cwnd_used;
        public uint snd_cwnd_cnt;	/* Linear increase counter		*/
        public long snd_cwnd_stamp; //通常用于 TCP 拥塞控制算法中，作为时间戳来记录某个特定事件的发生时刻。具体来说，它可以用来标记拥塞窗口 (snd_cwnd) 最后一次改变的时间

        //用于记录当前在网络中飞行的数据包数量。这些数据包已经发送出去但还未收到确认（ACK）
        //记录已经发送但还没有收到 ACK 确认的数据包数量。这对于 TCP 拥塞控制算法（如 Reno、Cubic）以及重传逻辑至关重要。
        public uint packets_out;
        public uint sacked_out;//表示已经被选择性确认SACK的数据包数量。
        public uint lost_out; // 表示被认为已经丢失的数据包数量

        //用于记录已经成功传递给应用程序的数据包总数。这个字段包括了所有已传递的数据包，即使这些数据包可能因为重传而被多次传递。
        public uint delivered;

        public minmax rtt_min = new minmax();
        public long srtt_us; //表示平滑后的往返时间，单位为微秒。
        public long rttvar_us;//表示往返时间变化的估计值，也称为均方差（mean deviation），单位为微秒。用来衡量RTT测量值的变化程度，帮助调整RTO以适应网络条件的变化。
        public long mdev_us;//mdev_us 记录了 RTT 样本的瞬时平均偏差，用于计算 RTT 的变异度（rttvar）
        public long mdev_max_us;//跟踪最大均方差，即mdev_us的最大值。可能用于调试目的或者特定的算法需求，比如设置RTO的上限。
        public ushort total_rto;    // Total number of RTO timeouts, including
        public ushort total_rto_recoveries;// Linux 内核 TCP 协议栈中的一个统计计数器，用于跟踪由于重传超时（RTO, Retransmission Timeout）而触发的恢复操作次数
        public long total_rto_time;

        //TCP chrono（计时器）是 Linux 内核 TCP 协议栈中用于时间测量和管理的机制，主要用于跟踪各种与连接状态相关的时间间隔。
        //它帮助实现诸如重传超时（RTO, Retransmission Timeout）、持续定时器（Persist Timer）、保持活动定时器（Keepalive Timer）等功能，
        //确保 TCP 连接的可靠性和性能优化。
        public long chrono_start;
        public tcp_chrono chrono_type;
        public long[] chrono_stat = new long[3];
        public ushort timeout_rehash;	/* Timeout-triggered rehash attempts */
        public byte compressed_ack;

        public uint rcv_nxt;//用于表示接收方下一个期望接收到的字节序号

        public HRTimer pacing_timer;
        public HRTimer compressed_ack_timer;

        /*
         * high_seq 是 Linux 内核 TCP 协议栈中的一个重要变量，用于跟踪 TCP 连接中某些特定序列号的边界。
         * 具体来说，high_seq 通常用来表示在快速重传（Fast Retransmit）或拥塞控制算法中的一些关键序列号位置。
         * 然而，在不同的上下文中，high_seq 的确切含义和用途可能会有所不同。
         * high_seq 在 TCP 协议栈中的作用:
          快速重传：在快速重传算法中，high_seq 可以被用来记录最近一次发送的最大序列号。
            当收到三个重复的 ACK（即同一个序列号的 ACK 出现三次），TCP 协议会认为丢失了一个数据包，并触发快速重传机制。
          此时，high_seq 帮助确定哪些数据包需要被重传。
          拥塞控制：在拥塞控制算法中，如 Reno 或 CUBIC，high_seq 也可以用来标记连接中某个重要的序列号点，例如最后一次窗口完全打开时的最高序列号。
            这有助于算法根据网络状况调整发送窗口大小，避免过度拥塞。
          SACK（选择性确认）支持：对于支持 SACK 的 TCP 实现，high_seq 可能用于跟踪已经发送但未被确认的数据块的上界，以便更精确地管理哪些部分的数据需要重传。
          其他用途：在某些情况下，high_seq 也可能用于其他与 TCP 状态跟踪相关的功能，具体取决于内核版本和实现细节。
        */
        public uint high_seq;	/* snd_nxt at onset of congestion	*/
        public uint snd_ssthresh;

        //// 是 Linux 内核 TCP 协议栈中用于拥塞控制的一个重要变量。
        ///它记录了在检测到网络拥塞（例如通过丢包或重复 ACK）之前，慢启动阈值（slow start threshold, ssthresh）的值。
        ///这个变量主要用于实现快速恢复（Fast Recovery）算法和帮助 TCP 连接从拥塞事件中更快地恢复。
        public uint prior_ssthresh;
        public uint prior_cwnd; //它通常指的是在某些特定事件发生之前的拥塞窗口（Congestion Window, cwnd）大小

        //描述：表示在新的恢复阶段（recovery episode）开始时的 snd_una 值（发送方未确认的数据包序列号）。
        //当进入一个新的恢复阶段时，undo_marker 会被设置为当前的 snd_una，这有助于确定哪些数据包是在恢复阶段之前发送的。
        //用途：
        //回滚支持：如果后续发现某些重传是不必要的（例如，因为延迟的 ACK 最终到达），TCP 可以使用 undo_marker 来回滚到恢复阶段之前的状态，从而避免不必要地减小拥塞窗口（CWND）。
        //拥塞控制调整：通过比较当前的 snd_una 和 undo_marker，可以判断是否应该撤销之前的拥塞控制决策。
        public uint undo_marker;
        //描述：表示可撤销（undoable）的重传次数。这个计数器记录了在当前恢复阶段内发生的、可能被撤销的重传数量。
        //用途：
        //追踪重传：帮助 TCP 跟踪哪些重传是可以撤销的，以便在接收到延迟的 ACK 或其他证据表明这些重传是不必要的时，能够正确地调整状态。
        //优化性能：通过允许撤销不必要的重传，TCP可以更智能地管理其发送速率，减少因误判导致的性能下降。
        public int undo_retrans;

        //描述：表示当前在网络中尚未被确认的重传数据包的数量。
        //每当一个数据包被重传时，retrans_out 会增加；当接收到对这些重传数据包的确认（ACK）时，retrans_out 会减少。
        //用途：
        //拥塞控制：帮助 TCP 检测和响应网络状况的变化。
        //例如，如果 retrans_out 数量增加，可能表明网络中存在丢包或拥塞，TCP 可以据此调整其发送速率和拥塞窗口（CWND）。
        //快速恢复：在快速恢复算法中，retrans_out 用于确定是否有未确认的重传数据包，并根据 ACK 反馈调整状态。
        //性能监控：通过监控 retrans_out 的变化，可以评估 TCP 连接的健康状况和性能，及时发现潜在的问题。
        public uint retrans_out;

        //是 Linux 内核 TCP 协议栈中用于管理 Tail Loss Probe (TLP) 机制的字段
        //TLP 是一种旨在更快速地检测和恢复尾部丢失（即连接末端的数据包丢失）的技术，它有助于减少不必要的延迟并提高传输效率。

        //表示在触发 TLP 时的 snd_nxt 值，即发送方下一个预期发送的数据包序列号。当 TLP 被触发时，这个值会被记录下来，以便后续评估 TLP 的效果。
        //用途：
        //跟踪 TLP 发送点：通过记录 snd_nxt 在 TLP 触发时的值，可以确定哪些数据包是在 TLP 触发之后发送的，从而更好地评估网络反馈。
        //确认 TLP 效果：如果接收到的 ACK 确认了比 tlp_high_seq 更高的序列号，说明 TLP 成功触发了新的 ACK，并可能揭示了之前未被发现的丢包
        public uint tlp_high_seq;   /* snd_nxt at the time of TLP */
        //指示 TLP 是否是一次重传操作。如果是重传，则该标志位会被设置为真（1 或 true），否则为假（0 或 false）。
        //用途：
        //区分 TLP 类型：帮助区分 TLP 是否是基于新数据还是重传旧数据包。这对于拥塞控制和恢复算法非常重要，因为不同类型的 TLP 可能需要不同的处理逻辑。
        //优化性能：通过了解 TLP 是否涉及重传，TCP 可以更智能地调整其行为，例如避免不必要的拥塞窗口减小。
        public byte tlp_retrans;    /* TLP is a retransmission */

        //这个变量用于表示当前连接对乱序包（out-of-order packets）的容忍度，即最大允许的数据段重排序数。
        //作用
        //乱序容忍度：当接收到的数据包不是按照发送顺序到达时，TCP 协议栈不会立即认为这些包是丢失的，而是等待一段时间看看是否能收到后续的包来填补空缺。
        //tp->reordering 就定义了在这种情况下可以接受的最大乱序程度。
        //快速重传触发：如果乱序超过了 tp->reordering 的值，TCP 可能会认为有数据包丢失，并触发快速重传机制以尽快恢复丢失的数据。
        public uint reordering;
        public byte ecn_flags;	/* ECN status bits.			*/

        //Fast Recovery with Timeout (F-RTO) 
        //F-RTO 的主要目的是在重传超时（RTO）后，判断该超时是由于真正的丢包还是由于延迟引起的。
        //如果是由于延迟引起的（即虚假 RTO），则避免不必要的重传和性能下降
        public bool frto; /* F-RTO (RFC5682) activated in CA_Loss */
        public bool is_sack_reneg;    /* in recovery from loss with SACK reneg? */

        public readonly tcp_options_received rx_opt = new tcp_options_received();
        public readonly tcp_rack rack = new tcp_rack();

        //这两个指针主要用于优化 TCP 丢失检测和重传机制。
        //用途: 这个指针通常用来标记或指示最近被认为丢失的数据包的 sk_buff。
        //它有助于快速定位可能需要进行 SACK 或 RACK 算法处理的数据段。
        //应用场景: 当 TCP 协议栈检测到数据包丢失时，它会使用这个指针来加快对丢失数据包的处理过程，比如决定哪些数据包需要被重传。
        //通过记住最后一个已知丢失的数据包的位置，可以减少遍历整个发送队列以查找丢失数据包所需的时间。
        public sk_buff lost_skb_hint;

        //用途: 这个指针指向最近一次尝试重传的数据包的 sk_buff。它帮助内核跟踪哪些数据包已经被重传，并且在某些情况下，可以帮助决定是否需要进一步重传其他数据包。
        //应用场景: 在执行快速重传或其他类型的重传策略时，retransmit_skb_hint 可以用来提高效率。
        //例如，当接收到 SACK 信息时，TCP 协议栈可以根据 retransmit_skb_hint 快速找到并评估哪些数据包还需要再次重传，而不需要重新扫描整个发送队列
        public sk_buff retransmit_skb_hint;
        public uint lost;//Linux 内核 TCP 协议栈中用于统计 TCP 连接上丢失的数据包总数的成员变量。

        public byte thin_lto; /* Use linear timeouts for thin streams */


        //struct sk_buff *highest_sack;
        //是 Linux 内核 TCP 协议栈中的一个重要成员变量，通常位于 struct tcp_sock 中。
        //它指向当前重传队列中最高的 SACK（选择性确认）块所对应的数据包 (sk_buff)。
        //这个指针在处理 SACK 选项时非常重要，因为它帮助 TCP 协议栈准确跟踪哪些数据包已经被部分或完全确认，并确保正确的数据重传。
        public sk_buff highest_sack;   /* skb just after the highest */

        //lost_cnt_hint 是 Linux 内核 TCP 协议栈中的一个字段，通常位于 struct tcp_sock 中。
        //它用于估计和跟踪在当前窗口内丢失的数据包数量。
        //这个字段对于 TCP 的拥塞控制和快速恢复机制非常重要，因为它帮助协议栈更准确地判断网络状况，并作出相应的调整以优化传输性能。
        //lost_cnt_hint 通常用来估计或提示（hint）有多少个段可能已经丢失。
        //这是TCP拥塞控制和丢失恢复算法的一部分。
        //当接收端检测到乱序或者重复ACK时，
        //它可以推断出某些段可能已经丢失。
        //这个字段可以帮助发送端了解网络状况，并据此调整其行为，例如通过快速重传机制来重发丢失的数据段，而无需等待超时。
        //具体来说，lost_cnt_hint 的值可以影响TCP的丢失恢复逻辑，比如SACK（选择性确认）选项的使用。
        //如果启用了SACK，那么即使只有部分数据丢失，也能够更精确地识别并重传这些丢失的部分，而不是重新传输整个窗口的数据。
        public int lost_cnt_hint;

        //定义：此字段表示整个TCP连接期间发生的总重传次数。
        //用途：它可以用来衡量一个连接中遇到的传输问题的严重程度。
        //频繁的重传可能是网络状况差的一个标志，也可能是拥塞控制算法响应的结果。
        public int total_retrans;

        //定义：这个字段记录了整个TCP连接过程中被重传的数据字节数。
        //用途：它有助于诊断网络问题或评估TCP连接的效率。
        //大量重传可能表明网络条件不佳、路由不稳定或者存在其他导致丢包的问题。
        public long bytes_retrans;

        //tcp_wstamp_ns 是Linux内核TCP协议栈中的一个字段，通常用于记录与TCP段相关的高精度时间戳。这个字段存储的是纳秒级的时间戳，它在TCP连接的管理和性能优化中扮演着重要角色。
        //tcp_wstamp_ns 的用途
        //精确的时间测量：tcp_wstamp_ns 用于记录TCP段被发送或接收的确切时间，以纳秒为单位。这种高精度的时间戳对于准确测量往返时间（RTT）、延迟和其他网络性能指标非常重要。
        //拥塞控制和流量控制：通过使用 tcp_wstamp_ns，TCP协议栈可以更准确地计算RTT，并据此调整拥塞窗口大小和发送速率，从而优化网络性能并避免拥塞。
        //快速重传和恢复：当检测到数据包丢失时，精确的时间戳可以帮助确定何时应该触发快速重传机制，以及如何有效地执行丢失恢复过程。
        //SACK（选择性确认）处理：在支持SACK的连接中，tcp_wstamp_ns 可以帮助更精确地识别哪些数据已被成功接收，哪些需要重传，进而提高传输效率。
        //统计和调试：高精度的时间戳对收集详细的网络统计信息和进行故障排除非常有用，能够提供关于网络行为的深入见解。
        public long tcp_wstamp_ns;
        public long tcp_clock_cache;
        public long rcv_tstamp;
        public long tcp_mstamp; //微秒
        public long retrans_stamp; //重传时间戳，毫秒
        public long lsndtime;//上次发送的数据包的时间戳, 用于重启窗口
        public long rto_stamp;//时间戳记录：每当触发一次 RTO 事件时，rto_stamp 会被设置为当前的时间戳。这有助于后续计算从 RTO 触发到恢复完成所花费的时间。
        public long first_tx_mstamp;
        public long delivered_mstamp;

        //它表示接收方愿意接受但尚未确认的数据量。
        //这个值在TCP头部中以16位字段的形式出现，因此其最大值为65535字节。
        //然而，通过使用窗口缩放选项（Window Scale），实际的接收窗口大小可以远远超过这个限制。
        public uint rcv_wnd;

        public uint snd_up;     //发送方的紧急指针,它表示的是上一次接收到的紧急指针值
        public uint rcv_up;
        //rcv_wup 字段通常出现在 struct tcp_sock 结构体中，表示接收窗口中紧急数据的结束位置。具体来说：
        //标识紧急数据的位置：rcv_wup 指向接收缓冲区中紧急数据之后的第一个字节。
        //这意味着从 rcv_wup 开始的数据是普通数据，而在此之前的数据被视为紧急数据。
        //支持紧急模式：当接收方接收到带有紧急指针的TCP段时，会更新 rcv_wup 以反映紧急数据的位置，
        //并可能进入紧急模式（如前所述的 tcp_urg_mode），确保紧急数据能够得到优先处理。
        public uint rcv_wup;    /* rcv_nxt on last window update sent	*/

        //存储了之前接收到的 TCP 报文的标志位。
        //比较这个值，判断是否直接进入快速路径
        //用于快速判断接收到的 TCP 数据包是否符合预期，从而决定是否可以进入快速处理路径（Fast Path）
        public uint pred_flags;

        public byte scaling_ratio;  /* see tcp_win_from_space() */

        //window_clamp 是Linux内核TCP协议栈中的一个重要参数，用于限制TCP接收窗口的最大值。
        //它确保接收窗口不会超过系统配置的最大值，从而避免过多的内存消耗，并且帮助维持网络连接的稳定性和性能。
        public uint window_clamp;   /* Maximal window to advertise		*/

        //rcv_ssthresh 在接收方的主要作用包括：
        //控制接收窗口增长：当接收到的数据量接近或超过当前的 rcv_ssthresh 时，接收方会更加保守地增加接收窗口大小，以避免过快消耗资源。
        //响应网络状况：通过动态调整 rcv_ssthresh，接收方可以更好地适应网络带宽和延迟的变化，确保高效的流量控制。
        //优化性能：合理设置 rcv_ssthresh 可以提高网络传输效率，减少丢包率和重传次数
        public uint rcv_ssthresh;

        //MSS不包括TCP报文头的长度，只指数据部分的最大长度。
        //advmss（Advertised Maximum Segment Size，通告的最大分段大小）是TCP协议中的一个重要参数，
        //用于协商连接两端的MTU（Maximum Transmission Unit，最大传输单元），以确保数据包不会被分片。
        //它在TCP三次握手过程中由发送方通过SYN或SYN-ACK报文中的MSS选项通告给接收方。
        public ushort advmss;
        public uint data_segs_out;
        public uint segs_out;
        public long bytes_sent;

        public uint tcp_tx_delay;   /* delay (in usec) added to TX packets */

        //时间排序的未确认报文链表：tsorted_sent_queue 是一个按时间排序的链表，用于存储已发送但尚未被确认的数据包。
        //这个链表用于加速 RACK（Retransmission Ambiguity Congestion Avoidance）算法的处理。
        public readonly list_head tsorted_sent_queue = new list_head(null);


        public uint delivered_ce;
        public uint app_limited;

        //prr_delivered 是TCP拥塞控制机制中的一个重要变量，
        //它用于记录进入恢复（Recovery）状态后接收端接收到的新数据包数量。
        //这个变量在Proportional Rate Reduction (PRR)算法中扮演了关键角色，PRR是RFC 6937定义的一种改进型快速恢复算法1。
        public long prr_delivered = 0;
        public uint prr_out = 0; //统计在同一时间段内发送方实际发出的新数据包数量。
        public long tsoffset;

        //为了应对乱序问题并优化TCP的行为，Linux内核引入了 reord_seen 计数器。每当TCP栈检测到一次乱序事件时，就会递增该计数器，并根据其值来调整算法的行为：
        //如果乱序已经被观察到（即 reord_seen 大于零），那么TCP可以在一定程度上容忍乱序，而不是立即进入拥塞恢复状态或降低拥塞窗口大小。这有助于避免因误判而导致的性能下降。
        //在一些情况下，如果乱序没有被观察到，TCP可能会更加激进地响应重复ACK或者达到重复ACK阈值，以此快速进入拥塞恢复阶段7。
        public uint reord_seen;	/* number of data packet reordering events */

        public long keepalive_time;      /* time before keep alive takes place */
        public long keepalive_intvl;  /* time interval between keep alive probes */

        //用于设置 TCP 连接的探测次数。
        //当 TCP 连接处于空闲状态时，内核会定期发送探测包以检测连接是否仍然可用。
        public byte keepalive_probes; /* num of allowed keep alive probes	*/


        //1. Nagle 算法简介
        //Nagle 算法是一种 TCP 优化机制，旨在减少网络上的小数据包数量。
        //它通过将小数据包聚合在一起，减少每个数据包的开销，从而提高网络效率。
        //然而，这种机制可能会增加数据传输的延迟，因为它会等待更多的数据积累后再发送。
        //2. 禁用 Nagle 算法
        //在某些应用场景中，如实时通信（在线游戏、语音通话、视频流等），低延迟比数据包聚合更重要。
        //因此，可以通过设置 TCP_NODELAY 选项来禁用 Nagle 算法，使数据立即发送。
        public byte nonagle; // Disable Nagle algorithm?
        public mtu_probe mtu_probe;

        //描述的是一个变量或数据成员，它保存了最近传输的小数据包的最后一个字节
        public uint snd_sml;    /* Last byte of the most recently transmitted small packet */

        //变量作用
        //序列号跟踪：cwnd_usage_seq 通常是一个无符号32位整数（u32），用来记录某个特定事件或状态的序列号。
        //它可以用于追踪哪些数据包已经在当前的拥塞窗口内发送出去，或者用于确定哪些ACK确认了哪些序列号范围内的数据。
        //拥塞窗口利用率：具体来说，这个变量可能用于衡量当前拥塞窗口的利用率，帮助算法决定如何调整未来的拥塞窗口大小。
        //例如，在收到一个ACK后，可以根据 cwnd_usage_seq 来判断该ACK对应的字节是否已经被计入到拥塞窗口的使用中。
        //使用场景
        //拥塞控制算法：不同的拥塞控制算法可能会以不同的方式使用 cwnd_usage_seq。例如，在某些实现中，它可以帮助区分新旧数据包，确保即使在网络状况变化时也能正确更新拥塞窗口。
        //快速恢复机制：在处理丢包或重复ACK的情况下，cwnd_usage_seq 可以帮助确定哪些数据包需要重新传输，并且可以辅助计算新的拥塞窗口大小。
        //性能优化：通过精确地跟踪拥塞窗口内的数据流动，可以更好地优化TCP连接的性能，减少不必要的重传并提高带宽利用率。
        public uint cwnd_usage_seq;
        public bool is_cwnd_limited;

        public uint max_packets_out;
        public uint snd_cwnd_clamp;

        public byte recvmsg_inq;//表明了你希望在调用 recvmsg 系统调用时获取队列中待接收的字节数

        //rcv_rtt_est 是 TCP 协议栈中的一个结构体，用于在接收端估计往返时间（RTT）。它通常包含以下几个字段：
        //rtt：用于存储估计的往返时间值。
        //seq：用于记录开始测量 RTT 的序列号。
        //time：用于记录开始测量 RTT 的时间戳
        //RTT 测量：在 TCP 连接中，接收端可以通过测量从发送确认到接收到下一个数据包的时间来估计 RTT。这个估计值用于调整重传超时（RTO）等参数
        //动态调整：通过不断更新 RTT 估计值，TCP 协议可以更好地适应网络条件的变化，从而提高传输效率和可靠性
        public readonly rcv_rtt_est rcv_rtt_est = new rcv_rtt_est();
        public readonly rcvq_space rcvq_space = new rcvq_space();
        public int linger2;

        public long bytes_received;
        public ulong bytes_acked;	//用于跟踪在当前拥塞窗口（congestion window）中已经被确认（ACKed）的字节数

        //out_of_order_queue 是 TCP 协议栈中用于处理乱序数据包的一个队列。
        //当 TCP 连接接收到的数据包不是按顺序到达时，这些乱序的数据包会被放入 out_of_order_queue 中，等待后续处理.
        //存储乱序数据包：该队列用于存储那些序列号不在当前接收窗口内的数据包。这些数据包可能因为网络延迟或丢包等原因而乱序到达.
        //数据包重组：当后续的数据包到达并填补了乱序数据包之间的空缺时，out_of_order_queue 中的数据包会被重新排序并移入接收队列中，
        //以便应用程序按顺序读取
        public readonly rb_root out_of_order_queue = new rb_root();

        //rcv_ooopack 是 TCP 协议栈中的一个字段，用于记录接收的乱序数据包的数量
        //当 TCP 接收到的数据包不是按顺序到达时，这些数据包会被标记为乱序，并且 rcv_ooopack 的值会增加。
        //这个字段可以帮助 TCP 协议栈更好地管理和处理乱序数据包，从而优化数据传输的效率和可靠性
        public uint rcv_ooopack;
        public sk_buff ooo_last_skb;

        //last_oow_ack_time 是与TCP协议栈中的ACK（确认）处理相关的内部变量，它记录了最近一次接收到的“超出窗口”（out-of-window, OoW）ACK的时间。
        //在TCP连接中，每个端点都有一个接收窗口，用来指示它可以接收但尚未确认的数据量。
        //当接收到的ACK不在当前接收窗口内时，就被认为是超出窗口的ACK。
        //超出窗口的ACK可能出现在以下几种情况：
        //重复ACK：由于网络重传或其它原因，接收到已经确认过的ACK。
        //未来的ACK：接收到的ACK确认了一个尚未发送的数据序列号，这可能是由于ACK风暴或者中间设备导致的ACK乱序。
        //错误的ACK：可能是由于数据包损坏或其他异常情况引起的。
        //记录 last_oow_ack_time 的主要目的是为了帮助检测和应对这些异常情况。例如，如果系统频繁地接收到超出窗口的ACK，可能意味着存在网络问题或潜在的安全威胁
        public long last_oow_ack_time;

        public uint dsack_dups;
        public uint rate_delivered;    /* saved rate sample: packets delivered */
        public uint rate_interval_us;  /* saved rate sample: time elapsed */
        public bool rate_app_limited;

        public long icsk_delack_max;

        //compressed_ack_rcv_nxt 是 TCP 协议栈中的一个字段，用于记录接收方期望接收的下一个序列号，特别是在压缩 ACK（Compressed ACK）机制中。
        //该字段在处理乱序数据包和压缩 ACK 时起关键作用。
        //记录期望序列号：compressed_ack_rcv_nxt 记录了接收方期望接收的下一个序列号。当接收到新的数据包时，该字段用于判断数据包是否按顺序到达。
        //处理乱序数据包：在处理乱序数据包时，compressed_ack_rcv_nxt 用于确定是否需要重新排序或合并数据包。
        //压缩 ACK 机制：在压缩 ACK 机制中，compressed_ack_rcv_nxt 用于判断是否需要发送压缩的 ACK 报文。
        //如果接收到的数据包的序列号与 compressed_ack_rcv_nxt 不匹配，可能会触发压缩 ACK 的发送。
        public uint compressed_ack_rcv_nxt;

        public byte dup_ack_counter;
        //用于记录接收方上次计算往返时间（RTT，Round-Trip Time）时的时间戳回显值（tsecr）。
        //这个字段在 TCP 时间戳选项中使用，用于更精确地测量 RTT。
        public long rcv_rtt_last_tsecr;

        public readonly tcp_sack_block[] duplicate_sack = new tcp_sack_block[1]
        {
            new tcp_sack_block()
        };
        public readonly tcp_sack_block[] selective_acks = new tcp_sack_block[4]
        {
            new tcp_sack_block(), new tcp_sack_block(),  new tcp_sack_block(),new tcp_sack_block()
        };
        public readonly tcp_sack_block[] recv_sack_cache = new tcp_sack_block[4]
        {
           new tcp_sack_block(), new tcp_sack_block(),  new tcp_sack_block(),new tcp_sack_block()
        };
    }
}
