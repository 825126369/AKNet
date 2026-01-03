using AKNet.Common;
using System.Collections.Generic;

namespace AKNet.Udp4Tcp.Common
{
    internal enum E_TIMER_TYPE
    {
        //用于控制数据包的发送速率，避免突发发送导致网络拥塞。Pacing 定时器确保数据包以更平滑、更均匀的间隔发送，有助于提高网络效率和公平性。
        TIMER_PACING,
        //控制接收方发送 ACK（确认）的延迟时间。QUIC 允许接收方延迟发送 ACK 以减少小数据包的数量，提高网络效率。该定时器用于在延迟超时后强制发送 ACK。
        TIMER_ACK_DELAY,
        //用于检测数据包是否丢失。QUIC 使用基于时间的机制来判断哪些数据包可能已经丢失，从而触发重传。这个定时器是 QUIC 拥塞控制和可靠性机制的核心部分。
        TIMER_LOSS_DETECTION,
        //在长时间空闲但连接仍需保持时，定期发送保持连接的探测包（keep-alive packets），以防止中间 NAT 或防火墙关闭连接。
        TIMER_KEEP_ALIVE,
        //用于管理连接的空闲超时。如果在指定时间内没有任何活动（包括数据和 keep-alive），连接将被关闭。该定时器用于检测和处理长时间无活动的连接。
        TIMER_IDLE,
        //在连接关闭过程中使用，确保关闭过程（如发送关闭帧、等待对端确认等）在合理时间内完成，避免资源长时间占用。
        TIMER_SHUTDOWN,
        TIMER_COUNT
    }

    internal class ConnectionOP : IPoolItemInterface
    {
        internal enum E_OP_TYPE
        {
            SendConnect,
            SendDisConnect,
        }

        private readonly LinkedListNode<ConnectionOP> mEntry;
        public E_OP_TYPE nOPType;

        public ConnectionOP()
        {
            mEntry = new LinkedListNode<ConnectionOP>(this);
        }

        public LinkedListNode<ConnectionOP> GetEntry()
        {
            return mEntry;
        }

        public void Reset()
        {
            
        }
    };
}
