
# 现有代码重要问题:﻿

1: QUIC_LOOKUP(根据数据包查找连接): 现有的写法有问题, 当时主要是考虑用C#字典数据结构来简化实现。
但很明显这里存在一些多线程竞争性能瓶颈问题。

2: 犯了兵家大忌: 先后对2个MSQuic版本进行代码抄录，导致现在出现问题了，不知道问题在哪。

3：先前的测试不严谨, 现在可以肯定的说：核心丢包检测，还是有问题，发送压力一大，就会出现 错误。

4: 今天测试不小心 用了超过 int.MaxValue个字节的Buffer, 结果程序没报任何错。但也不执行任何其他方法了。能用long就用，少出点问题。

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
