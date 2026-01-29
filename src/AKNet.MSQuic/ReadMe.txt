
# 现有代码重要问题:﻿

1：GC还是太严重了,得持续的查

# MSQuic 2.5 重要特征：

1：Packet Number(包号): 从0开始，永不重复,无论是重传包还是不同流的包，包号严格递增，从而精确计算 RTT。

2: QUIC_STREAM_EVENT_SEND_COMPLETE,当发送数据 加到 缓冲区里的时候返回

# MSQuic 注意事项：

1: MsQuic 线程是“黄金线程”——它负责整个 QUIC 协议栈的高效运转。任何阻塞或耗时操作都会影响所有连接的性能。
因此，必须尽快“逃离”MsQuic 线程，把用户逻辑交给 .NET 的线程池或异步调度器来处理。

# MSQuic 丢包重传设计:

(1): Tracker.PacketNumbersToAck(Range): 收集接收到的包号, 发送ACK帧的时候，把这个Range编码进去。
(2): Tracker.PacketNumbersReceived(Range): 判断是否有重复包发送, 有,则丢弃该包。
(3): Connection.DecodedAckRanges(Range): 解析对端发送的ACK帧的结果
(4): LossDetection.SentPackets: 需要进行ACK确认的包，重传包
(5): LossDetection.LostPackets: 超过约2个RTO时间未接收到ACK, 那么 SentPackets队列中的包移除增加到 LostPackets队列中。
(6): Stream.SparseAckRanges(Range): 发送端收到ACK后,连接里的流中的确认的发送buffer中的 绝对偏移量。
(7): RecvBuffer.WrittenRanges(Range):接收端 收到流数据帧后,确认的发送buffer中的 绝对偏移量


# MSQuic/Quic 性能统计:
 
 2026-01-28 测试结果: [VS2022/.Net 9.0]
	.Net QUIC:
	接受包数量: 1000000 总共花费时间: 20.7302744,平均1秒接收：48238.62171992568
	接受包数量: 1000000 总共花费时间: 36.5642465,平均1秒接收：27349.11990354381

	MSQuic:(条件 忽略加密)
	接受包数量: 1000000 总共花费时间: 25.6202718,平均1秒接收：39031.58865892071
	接受包数量: 1000000 总共花费时间: 26.0276865,平均1秒接收：38420.61991024759
	
	综上: C# 写的网络库 竟然超过了 C语言的网络库。一定会让很多C语言大神羞愧不已。

