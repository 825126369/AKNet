using System.IO;
using System;
using AKNet.Common;

namespace AKNet.Udp5Quic.Common
{
    internal class QUIC_LOSS_DETECTION
    {
        public QUIC_CONNECTION mConnection;
        public int PacketsInFlight;
        public ulong LargestAck;
        public QUIC_ENCRYPT_LEVEL LargestAckEncryptLevel;
        public long TimeOfLastPacketSent;
        public long TimeOfLastPacketAcked;
        public long TimeOfLastAckedPacketSent;
        public long AdjustedLastAckedTime;
        public ulong TotalBytesSent;
        public ulong TotalBytesAcked;
        public ulong TotalBytesSentAtLastAck;
        public ulong LargestSentPacketNumber;
        public QUIC_SENT_PACKET_METADATA SentPackets;
        public QUIC_SENT_PACKET_METADATA SentPacketsTail;
        public QUIC_SENT_PACKET_METADATA LostPackets;
        public QUIC_SENT_PACKET_METADATA LostPacketsTail;
        public ushort ProbeCount;
    }

    internal static partial class MSQuicFunc
    {
        static void QuicLossDetectionInitialize(QUIC_LOSS_DETECTION LossDetection)
        {
            LossDetection.SentPackets = null;
            LossDetection.SentPacketsTail = LossDetection.SentPackets;
            LossDetection.LostPackets = null;
            LossDetection.LostPacketsTail = LossDetection.LostPackets;
            QuicLossDetectionInitializeInternalState(LossDetection);
        }

        static void QuicLossDetectionInitializeInternalState(QUIC_LOSS_DETECTION LossDetection)
        {
            LossDetection.PacketsInFlight = 0;
            LossDetection.TimeOfLastPacketSent = 0;
            LossDetection.TotalBytesSent = 0;
            LossDetection.TotalBytesAcked = 0;
            LossDetection.TotalBytesSentAtLastAck = 0;
            LossDetection.TimeOfLastPacketAcked = 0;
            LossDetection.TimeOfLastAckedPacketSent = 0;
            LossDetection.AdjustedLastAckedTime = 0;
            LossDetection.ProbeCount = 0;
        }

        static long QuicLossDetectionComputeProbeTimeout(QUIC_LOSS_DETECTION LossDetection, QUIC_PATH Path, int Count)
        {
            QUIC_CONNECTION Connection = QuicLossDetectionGetConnection(LossDetection);
            NetLog.Assert(Path.SmoothedRtt != 0);

            long Pto = Path.SmoothedRtt + 4 * Path.RttVariance + Connection.PeerTransportParams.MaxAckDelay;
            Pto *= Count;
            return Pto;
        }

    }
}
