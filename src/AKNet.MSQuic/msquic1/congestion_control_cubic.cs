/************************************Copyright*****************************************
*        ProjectName:AKNet
*        Web:https://github.com/825126369/AKNet
*        Description:C#游戏网络库
*        Author:许珂
*        StartTime:2024/11/01 00:00:00
*        ModifyTime:2026/2/1 20:26:55
*        Copyright:MIT软件许可证
************************************Copyright*****************************************/
using AKNet.Common;
using System;

namespace MSQuic1
{
    internal enum QUIC_CUBIC_HYSTART_STATE
    {
        HYSTART_NOT_STARTED = 0,
        HYSTART_ACTIVE = 1,
        HYSTART_DONE = 2
    }

    internal class QUIC_CONGESTION_CONTROL_CUBIC
    {
        public bool HasHadCongestionEvent;
        public bool IsInRecovery;
        public bool IsInPersistentCongestion;
        public bool TimeOfLastAckValid;
        public long InitialWindowPackets;
        public long SendIdleTimeoutMs;

        public long CongestionWindow; // bytes
        public long PrevCongestionWindow; // bytes
        public long SlowStartThreshold; // 慢启动阶段的阈值, bytes
        public long PrevSlowStartThreshold; // bytes
        public long AimdWindow; // bytes
        public long PrevAimdWindow; // bytes
        public long AimdAccumulator; // bytes

        public long BytesInFlight;
        public long BytesInFlightMax;
        public long LastSendAllowance; // bytes

        public byte Exemptions; //它是个“免责计数器”——只要不为 0，本轮丢包就不会把拥塞窗口 cwnd 砍掉一半，而是继续按 CUBic 曲线增长。

        public long TimeOfLastAck; // microseconds
        public long TimeOfCongAvoidStart; // microseconds
        public long KCubic; // millisec
        public long PrevKCubic; // millisec
        public long WindowPrior; // bytes (prior_cwnd from rfc8312bis)
        public long PrevWindowPrior; // bytes
        public long WindowMax; // bytes (W_max from rfc8312bis)
        public long PrevWindowMax; // bytes
        public long WindowLastMax; // bytes (W_last_max from rfc8312bis)
        public long PrevWindowLastMax; // bytes

        public QUIC_CUBIC_HYSTART_STATE HyStartState;
        public long HyStartAckCount;
        public long MinRttInLastRound; // microseconds
        public long MinRttInCurrentRound; // microseconds
        public long CssBaselineMinRtt; // microseconds
        public long HyStartRoundEnd; // Packet Number
        public long CWndSlowStartGrowthDivisor;
        public long ConservativeSlowStartRounds;
        public long RecoverySentPacketNumber;
    }

    internal static partial class MSQuicFunc
    {
        public const int TEN_TIMES_BETA_CUBIC = 7;
        public const int TEN_TIMES_C_CUBIC = 4;

        //“整数立方根”算法——用 逐位试填（bit-by-bit） 的方式，
        //在 31 位范围内求 ⌊∛radicand⌋，完全避开浮点和 pow，保证 O(1) 时间、O(1) 空间、可预测的分支，适合内核或实时场景。
        static long CubeRoot(long Radicand)
        {
            int i;
            long x = 0;
            long y = 0;

            for (i = 30; i >= 0; i -= 3)
            {
                x = x * 8 + ((Radicand >> i) & 7);
                if ((y * 2 + 1) * (y * 2 + 1) * (y * 2 + 1) <= x)
                {
                    y = y * 2 + 1;
                }
                else
                {
                    y = y * 2;
                }
            }
            return y;
        }

        static void CubicCongestionControlInitialize(out QUIC_CONGESTION_CONTROL Cc, QUIC_CONNECTION Connection)
        {
            QUIC_CONGESTION_CONTROL QuicCongestionControlCubic = new QUIC_CONGESTION_CONTROL
            {
                Name = "Cubic",
                QuicCongestionControlCanSend = CubicCongestionControlCanSend,
                QuicCongestionControlSetExemption = CubicCongestionControlSetExemption,
                QuicCongestionControlReset = CubicCongestionControlReset,
                QuicCongestionControlGetSendAllowance = CubicCongestionControlGetSendAllowance,
                QuicCongestionControlOnDataSent = CubicCongestionControlOnDataSent,
                QuicCongestionControlOnDataInvalidated = CubicCongestionControlOnDataInvalidated,
                QuicCongestionControlOnDataAcknowledged = CubicCongestionControlOnDataAcknowledged,
                QuicCongestionControlOnDataLost = CubicCongestionControlOnDataLost,
                QuicCongestionControlOnEcn = CubicCongestionControlOnEcn,
                QuicCongestionControlOnSpuriousCongestionEvent = CubicCongestionControlOnSpuriousCongestionEvent,
                QuicCongestionControlLogOutFlowStatus = CubicCongestionControlLogOutFlowStatus,
                QuicCongestionControlGetExemptions = CubicCongestionControlGetExemptions,
                QuicCongestionControlGetBytesInFlightMax = CubicCongestionControlGetBytesInFlightMax,
                QuicCongestionControlIsAppLimited = CubicCongestionControlIsAppLimited,
                QuicCongestionControlSetAppLimited = CubicCongestionControlSetAppLimited,
                QuicCongestionControlGetCongestionWindow = CubicCongestionControlGetCongestionWindow,
            };

            Cc = QuicCongestionControlCubic;
            QUIC_CONGESTION_CONTROL_CUBIC Cubic = Cc.Cubic;
            Cc.mConnection = Connection;

            ushort DatagramPayloadLength = QuicPathGetDatagramPayloadSize(Connection.Paths[0]);
            Cubic.SlowStartThreshold = uint.MaxValue;
            Cubic.SendIdleTimeoutMs = Connection.Settings.SendIdleTimeoutMs;
            Cubic.InitialWindowPackets = Connection.Settings.InitialWindowPackets;
            Cubic.CongestionWindow = DatagramPayloadLength * Cubic.InitialWindowPackets;
            Cubic.BytesInFlightMax = Cubic.CongestionWindow / 2;
            Cubic.MinRttInCurrentRound = long.MaxValue;
            Cubic.HyStartRoundEnd = Connection.Send.NextPacketNumber;
            Cubic.HyStartState = QUIC_CUBIC_HYSTART_STATE.HYSTART_NOT_STARTED;
            Cubic.CWndSlowStartGrowthDivisor = 1;

            CubicCongestionHyStartResetPerRttRound(Cubic);
        }

        static void CubicCongestionHyStartResetPerRttRound(QUIC_CONGESTION_CONTROL_CUBIC Cubic)
        {
            Cubic.HyStartAckCount = 0;
            Cubic.MinRttInLastRound = Cubic.MinRttInCurrentRound;
            Cubic.MinRttInCurrentRound = long.MaxValue;
        }

        static void CubicCongestionHyStartChangeState(QUIC_CONGESTION_CONTROL Cc, QUIC_CUBIC_HYSTART_STATE NewHyStartState)
        {
            QUIC_CONNECTION Connection = QuicCongestionControlGetConnection(Cc);
            if (!Connection.Settings.HyStartEnabled)
            {
                return;
            }

            QUIC_CONGESTION_CONTROL_CUBIC Cubic = Cc.Cubic;
            switch (NewHyStartState)
            {
                case QUIC_CUBIC_HYSTART_STATE.HYSTART_ACTIVE:
                    break;
                case QUIC_CUBIC_HYSTART_STATE.HYSTART_DONE:
                case QUIC_CUBIC_HYSTART_STATE.HYSTART_NOT_STARTED:
                    Cubic.CWndSlowStartGrowthDivisor = 1;
                    break;
                default:
                    NetLog.Assert(false);
                    break;
            }

            if (Cubic.HyStartState != NewHyStartState)
            {
                Cubic.HyStartState = NewHyStartState;
            }
        }

        static bool CubicCongestionControlCanSend(QUIC_CONGESTION_CONTROL Cc)
        {
            QUIC_CONGESTION_CONTROL_CUBIC Cubic = Cc.Cubic;
            return Cubic.BytesInFlight < Cubic.CongestionWindow || Cubic.Exemptions > 0;
        }

        //该函数的主要目的是临时豁免某些关键数据包，允许它们在拥塞窗口（Congestion Window）已满的情况下仍然被发送。
        //在正常的拥塞控制中，应用程序能发送的数据量受到“拥塞窗口”大小的限制。
        //当窗口“满”了（即已发送但未确认的数据量达到了窗口大小），新的数据包必须等待，直到收到 ACK 确认，窗口“前进”后才能发送。
        //但有些数据包非常关键，不能等待，例如：
        //重传包(Retransmissions) : 如果一个重要的控制包（如 ACK 帧本身、连接关闭帧）丢失了，如果不立即重传，整个连接可能会停滞或超时。
        //因此，这类重传包通常被“豁免”，可以立即发送。
        //探路包(Probing Packets) : 当连接长时间空闲后恢复发送，或者需要探测网络是否还有可用带宽时，会发送少量“探路包”。
        //这些包也需要豁免，以触发网络响应。
        static void CubicCongestionControlSetExemption(QUIC_CONGESTION_CONTROL Cc, byte NumPackets)
        {
            Cc.Cubic.Exemptions = NumPackets;
        }

        static void CubicCongestionControlReset(QUIC_CONGESTION_CONTROL Cc, bool FullReset)
        {
            QUIC_CONGESTION_CONTROL_CUBIC Cubic = Cc.Cubic;
            QUIC_CONNECTION Connection = QuicCongestionControlGetConnection(Cc);
            ushort DatagramPayloadLength = QuicPathGetDatagramPayloadSize(Connection.Paths[0]);
            Cubic.SlowStartThreshold = uint.MaxValue;
            Cubic.MinRttInCurrentRound = uint.MaxValue;
            Cubic.HyStartRoundEnd = Connection.Send.NextPacketNumber;
            CubicCongestionHyStartResetPerRttRound(Cubic);
            CubicCongestionHyStartChangeState(Cc, QUIC_CUBIC_HYSTART_STATE.HYSTART_NOT_STARTED);
            Cubic.IsInRecovery = false;
            Cubic.HasHadCongestionEvent = false;
            Cubic.CongestionWindow = DatagramPayloadLength * Cubic.InitialWindowPackets;
            Cubic.BytesInFlightMax = Cubic.CongestionWindow / 2;
            Cubic.LastSendAllowance = 0;
            if (FullReset)
            {
                Cubic.BytesInFlight = 0;
            }
        }

        static long CubicCongestionControlGetSendAllowance(QUIC_CONGESTION_CONTROL Cc, long TimeSinceLastSend, bool TimeSinceLastSendValid)
        {
            QUIC_CONGESTION_CONTROL_CUBIC Cubic = Cc.Cubic;
            long SendAllowance;
            QUIC_CONNECTION Connection = QuicCongestionControlGetConnection(Cc);
            if (Cubic.BytesInFlight >= Cubic.CongestionWindow)
            {
                SendAllowance = 0;
            }
            else if (!TimeSinceLastSendValid || !Connection.Settings.PacingEnabled || !Connection.Paths[0].GotFirstRttSample ||
                Connection.Paths[0].SmoothedRtt < QUIC_MIN_PACING_RTT)
            {
                SendAllowance = Cubic.CongestionWindow - Cubic.BytesInFlight;
            }
            else
            {
                long EstimatedWnd;
                if (Cubic.CongestionWindow < Cubic.SlowStartThreshold)
                {
                    EstimatedWnd = Cubic.CongestionWindow << 1;
                    if (EstimatedWnd > Cubic.SlowStartThreshold)
                    {
                        EstimatedWnd = Cubic.SlowStartThreshold;
                    }
                }
                else
                {
                    EstimatedWnd = Cubic.CongestionWindow + (Cubic.CongestionWindow >> 2); // CongestionWindow * 1.25
                }

                SendAllowance = Cubic.LastSendAllowance + (EstimatedWnd * TimeSinceLastSend / Connection.Paths[0].SmoothedRtt);
                if (SendAllowance < Cubic.LastSendAllowance ||
                    SendAllowance > (Cubic.CongestionWindow - Cubic.BytesInFlight))
                {
                    SendAllowance = Cubic.CongestionWindow - Cubic.BytesInFlight;
                }
                Cubic.LastSendAllowance = SendAllowance;
            }
            return SendAllowance;
        }

        static bool CubicCongestionControlUpdateBlockedState(QUIC_CONGESTION_CONTROL Cc, bool PreviousCanSendState)
        {
            QUIC_CONNECTION Connection = QuicCongestionControlGetConnection(Cc);
            if (PreviousCanSendState != CubicCongestionControlCanSend(Cc))
            {
                if (PreviousCanSendState)
                {
                    QuicConnAddOutFlowBlockedReason(Connection, QUIC_FLOW_BLOCKED_CONGESTION_CONTROL);
                }
                else
                {
                    QuicConnRemoveOutFlowBlockedReason(Connection, QUIC_FLOW_BLOCKED_CONGESTION_CONTROL);
                    Connection.Send.LastFlushTime = CxPlatTimeUs(); // Reset last flush time
                    return true;
                }
            }
            return false;
        }

        static void CubicCongestionControlOnCongestionEvent(QUIC_CONGESTION_CONTROL Cc, bool IsPersistentCongestion, bool Ecn)
        {
            QUIC_CONGESTION_CONTROL_CUBIC Cubic = Cc.Cubic;

            QUIC_CONNECTION Connection = QuicCongestionControlGetConnection(Cc);
            ushort DatagramPayloadLength = QuicPathGetDatagramPayloadSize(Connection.Paths[0]);
            Connection.Stats.Send.CongestionCount++;
            Cubic.IsInRecovery = true;
            Cubic.HasHadCongestionEvent = true;
            if (!Ecn)
            {
                Cubic.PrevWindowPrior = Cubic.WindowPrior;
                Cubic.PrevWindowMax = Cubic.WindowMax;
                Cubic.PrevWindowLastMax = Cubic.WindowLastMax;
                Cubic.PrevKCubic = Cubic.KCubic;
                Cubic.PrevSlowStartThreshold = Cubic.SlowStartThreshold;
                Cubic.PrevCongestionWindow = Cubic.CongestionWindow;
                Cubic.PrevAimdWindow = Cubic.AimdWindow;
            }

            if (IsPersistentCongestion && !Cubic.IsInPersistentCongestion)
            {
                NetLog.Assert(!Cubic.IsInPersistentCongestion);
                Connection.Stats.Send.PersistentCongestionCount++;
                Connection.Paths[0].Route.State = CXPLAT_ROUTE_STATE.RouteSuspected;

                Cubic.IsInPersistentCongestion = true;
                Cubic.WindowPrior = Cubic.WindowMax = Cubic.WindowLastMax = Cubic.SlowStartThreshold = Cubic.AimdWindow = Cubic.CongestionWindow * TEN_TIMES_BETA_CUBIC / 10;
                Cubic.CongestionWindow = DatagramPayloadLength * QUIC_PERSISTENT_CONGESTION_WINDOW_PACKETS;
                Cubic.KCubic = 0;
                CubicCongestionHyStartChangeState(Cc, QUIC_CUBIC_HYSTART_STATE.HYSTART_DONE);
            }
            else
            {

                Cubic.WindowPrior = Cubic.WindowMax = Cubic.CongestionWindow;
                if (Cubic.WindowLastMax > Cubic.WindowMax)
                {
                    Cubic.WindowLastMax = Cubic.WindowMax;
                    Cubic.WindowMax = Cubic.WindowMax * (10 + TEN_TIMES_BETA_CUBIC) / 20;
                }
                else
                {
                    Cubic.WindowLastMax = Cubic.WindowMax;
                }

                Cubic.KCubic = CubeRoot((Cubic.WindowMax / DatagramPayloadLength * (10 - TEN_TIMES_BETA_CUBIC) << 9) / TEN_TIMES_C_CUBIC);
                Cubic.KCubic = Cubic.KCubic * 1000;
                Cubic.KCubic >>= 3;

                CubicCongestionHyStartChangeState(Cc, QUIC_CUBIC_HYSTART_STATE.HYSTART_DONE);
                Cubic.SlowStartThreshold = Cubic.CongestionWindow = Cubic.AimdWindow =
                    Math.Max(DatagramPayloadLength * QUIC_PERSISTENT_CONGESTION_WINDOW_PACKETS, Cubic.CongestionWindow * TEN_TIMES_BETA_CUBIC / 10);
            }

        }

        static void CubicCongestionControlOnDataSent(QUIC_CONGESTION_CONTROL Cc, long NumRetransmittableBytes)
        {
            QUIC_CONGESTION_CONTROL_CUBIC Cubic = Cc.Cubic;
            bool PreviousCanSendState = QuicCongestionControlCanSend(Cc);

            Cubic.BytesInFlight += NumRetransmittableBytes;
            if (Cubic.BytesInFlightMax < Cubic.BytesInFlight)
            {
                Cubic.BytesInFlightMax = Cubic.BytesInFlight;
                QuicSendBufferConnectionAdjust(QuicCongestionControlGetConnection(Cc));
            }

            if (NumRetransmittableBytes > Cubic.LastSendAllowance)
            {
                Cubic.LastSendAllowance = 0;
            }
            else
            {
                Cubic.LastSendAllowance -= NumRetransmittableBytes;
            }

            if (Cubic.Exemptions > 0)
            {
                --Cubic.Exemptions;
            }

            CubicCongestionControlUpdateBlockedState(Cc, PreviousCanSendState);
        }

        static bool CubicCongestionControlOnDataInvalidated(QUIC_CONGESTION_CONTROL Cc, long NumRetransmittableBytes)
        {
            QUIC_CONGESTION_CONTROL_CUBIC Cubic = Cc.Cubic;
            bool PreviousCanSendState = CubicCongestionControlCanSend(Cc);

            NetLog.Assert(Cubic.BytesInFlight >= NumRetransmittableBytes);
            Cubic.BytesInFlight -= NumRetransmittableBytes;
            return CubicCongestionControlUpdateBlockedState(Cc, PreviousCanSendState);
        }

        static bool CubicCongestionControlOnDataAcknowledged(QUIC_CONGESTION_CONTROL Cc, QUIC_ACK_EVENT AckEvent)
        {
            QUIC_CONGESTION_CONTROL_CUBIC Cubic = Cc.Cubic;

            long TimeNowUs = AckEvent.TimeNow;
            QUIC_CONNECTION Connection = QuicCongestionControlGetConnection(Cc);
            bool PreviousCanSendState = CubicCongestionControlCanSend(Cc);
            long BytesAcked = AckEvent.NumRetransmittableBytes;

            NetLog.Assert(Cubic.BytesInFlight >= BytesAcked, $"{Cubic.BytesInFlight}, {BytesAcked}");
            Cubic.BytesInFlight -= BytesAcked;

            if (Cubic.IsInRecovery)
            {
                //已完成恢复。请注意，恢复的完成被定义为，这里与TCP有点不同：我们只需要一个ACK， 恢复开始后发送的数据包。
                if (AckEvent.LargestAck > Cubic.RecoverySentPacketNumber)
                {
                    Cubic.IsInRecovery = false;
                    Cubic.IsInPersistentCongestion = false;
                    Cubic.TimeOfCongAvoidStart = TimeNowUs;
                }
                goto Exit;
            }
            else if (BytesAcked == 0)
            {
                goto Exit;
            }

            if (Connection.Settings.HyStartEnabled && Cubic.HyStartState != QUIC_CUBIC_HYSTART_STATE.HYSTART_DONE)
            {
                if (AckEvent.MinRttValid)
                {
                    if (Cubic.HyStartAckCount < QUIC_HYSTART_DEFAULT_N_SAMPLING)
                    {
                        Cubic.MinRttInCurrentRound = Math.Min(Cubic.MinRttInCurrentRound, AckEvent.MinRtt);
                        Cubic.HyStartAckCount++;
                    }
                    else if (Cubic.HyStartState == QUIC_CUBIC_HYSTART_STATE.HYSTART_NOT_STARTED)
                    {
                        long Eta = Math.Min(QUIC_HYSTART_DEFAULT_MAX_ETA, Math.Max(QUIC_HYSTART_DEFAULT_MIN_ETA, Cubic.MinRttInLastRound / 8));
                        if (Cubic.MinRttInLastRound != long.MaxValue &&
                            Cubic.MinRttInCurrentRound != long.MaxValue &&
                            (Cubic.MinRttInCurrentRound >= Cubic.MinRttInLastRound + Eta))
                        {
                            CubicCongestionHyStartChangeState(Cc, QUIC_CUBIC_HYSTART_STATE.HYSTART_ACTIVE);
                            Cubic.CWndSlowStartGrowthDivisor = QUIC_CONSERVATIVE_SLOW_START_DEFAULT_GROWTH_DIVISOR;
                            Cubic.ConservativeSlowStartRounds = QUIC_CONSERVATIVE_SLOW_START_DEFAULT_ROUNDS;
                            Cubic.CssBaselineMinRtt = Cubic.MinRttInCurrentRound;
                        }
                    }
                    else
                    {
                        if (Cubic.MinRttInCurrentRound < Cubic.CssBaselineMinRtt)
                        {
                            CubicCongestionHyStartChangeState(Cc, QUIC_CUBIC_HYSTART_STATE.HYSTART_NOT_STARTED);
                        }
                    }
                }

                if (AckEvent.LargestAck >= Cubic.HyStartRoundEnd)
                {
                    Cubic.HyStartRoundEnd = Connection.Send.NextPacketNumber;
                    if (Cubic.HyStartState == QUIC_CUBIC_HYSTART_STATE.HYSTART_ACTIVE)
                    {
                        if (--Cubic.ConservativeSlowStartRounds == 0)
                        {
                            Cubic.SlowStartThreshold = Cubic.CongestionWindow;
                            Cubic.TimeOfCongAvoidStart = TimeNowUs;
                            Cubic.AimdWindow = Cubic.CongestionWindow;
                            CubicCongestionHyStartChangeState(Cc, QUIC_CUBIC_HYSTART_STATE.HYSTART_DONE);
                        }
                    }
                    CubicCongestionHyStartResetPerRttRound(Cubic);
                }
            }

            if (Cubic.CongestionWindow < Cubic.SlowStartThreshold)
            {
                Cubic.CongestionWindow += (BytesAcked / Cubic.CWndSlowStartGrowthDivisor);
                BytesAcked = 0;
                if (Cubic.CongestionWindow >= Cubic.SlowStartThreshold)
                {
                    Cubic.TimeOfCongAvoidStart = TimeNowUs;
                    BytesAcked = Cubic.CongestionWindow - Cubic.SlowStartThreshold;
                    Cubic.CongestionWindow = Cubic.SlowStartThreshold;
                }
            }

            if (BytesAcked > 0)
            {
                NetLog.Assert(Cubic.CongestionWindow >= Cubic.SlowStartThreshold);
                ushort DatagramPayloadLength = QuicPathGetDatagramPayloadSize(Connection.Paths[0]);
                if (Cubic.TimeOfLastAckValid)
                {
                    long TimeSinceLastAck = CxPlatTimeDiff(Cubic.TimeOfLastAck, TimeNowUs);
                    if (TimeSinceLastAck > MS_TO_US(Cubic.SendIdleTimeoutMs) &&
                        TimeSinceLastAck > (Connection.Paths[0].SmoothedRtt + 4 * Connection.Paths[0].RttVariance))
                    {
                        Cubic.TimeOfCongAvoidStart += TimeSinceLastAck;
                        if (CxPlatTimeAtOrBefore64(TimeNowUs, Cubic.TimeOfCongAvoidStart))
                        {
                            Cubic.TimeOfCongAvoidStart = TimeNowUs;
                        }
                    }
                }

                long TimeInCongAvoidUs = CxPlatTimeDiff(Cubic.TimeOfCongAvoidStart, TimeNowUs);
                long DeltaT = US_TO_MS(TimeInCongAvoidUs - MS_TO_US(Cubic.KCubic) + AckEvent.SmoothedRtt);
                if (DeltaT > 2500000)
                {
                    DeltaT = 2500000;
                }

                long CubicWindow = ((((DeltaT * DeltaT) >> 10) * DeltaT * (DatagramPayloadLength * TEN_TIMES_C_CUBIC / 10)) >> 20) + Cubic.WindowMax;
                if (CubicWindow < 0)
                {
                    CubicWindow = 2 * Cubic.BytesInFlightMax;
                }

                NetLog.Assert(TEN_TIMES_BETA_CUBIC == 7, "TEN_TIMES_BETA_CUBIC must be 7 for simplified calculation.");
                if (Cubic.AimdWindow < Cubic.WindowPrior)
                {
                    Cubic.AimdAccumulator += BytesAcked / 2;
                }
                else
                {
                    Cubic.AimdAccumulator += BytesAcked;
                }
                if (Cubic.AimdAccumulator > Cubic.AimdWindow)
                {
                    Cubic.AimdWindow += DatagramPayloadLength;
                    Cubic.AimdAccumulator -= Cubic.AimdWindow;
                }

                if (Cubic.AimdWindow > CubicWindow)
                {
                    Cubic.CongestionWindow = Cubic.AimdWindow;
                }
                else
                {
                    long TargetWindow = Math.Max(Cubic.CongestionWindow, Math.Min(CubicWindow, Cubic.CongestionWindow + (Cubic.CongestionWindow >> 1)));
                    Cubic.CongestionWindow += (TargetWindow - Cubic.CongestionWindow) * DatagramPayloadLength / Cubic.CongestionWindow;
                }
            }

            if (Cubic.CongestionWindow > 2 * Cubic.BytesInFlightMax)
            {
                Cubic.CongestionWindow = 2 * Cubic.BytesInFlightMax;
            }

        Exit:
            Cubic.TimeOfLastAck = TimeNowUs;
            Cubic.TimeOfLastAckValid = true;

            if (Connection.Settings.NetStatsEventEnabled)
            {
                QUIC_PATH Path = Connection.Paths[0];
                QUIC_CONNECTION_EVENT Event = new QUIC_CONNECTION_EVENT();
                Event.Type = QUIC_CONNECTION_EVENT_TYPE.QUIC_CONNECTION_EVENT_NETWORK_STATISTICS;
                Event.NETWORK_STATISTICS.BytesInFlight = Cubic.BytesInFlight;
                Event.NETWORK_STATISTICS.PostedBytes = Connection.SendBuffer.PostedBytes;
                Event.NETWORK_STATISTICS.IdealBytes = Connection.SendBuffer.IdealBytes;
                Event.NETWORK_STATISTICS.SmoothedRTT = Path.SmoothedRtt;
                Event.NETWORK_STATISTICS.CongestionWindow = Cubic.CongestionWindow;
                Event.NETWORK_STATISTICS.Bandwidth = Cubic.CongestionWindow / Path.SmoothedRtt;
                QuicConnIndicateEvent(Connection, ref Event);
            }

            return CubicCongestionControlUpdateBlockedState(Cc, PreviousCanSendState);
        }

        static void CubicCongestionControlOnDataLost(QUIC_CONGESTION_CONTROL Cc, QUIC_LOSS_EVENT LossEvent)
        {
            QUIC_CONGESTION_CONTROL_CUBIC Cubic = Cc.Cubic;

            bool PreviousCanSendState = CubicCongestionControlCanSend(Cc);
            if (!Cubic.HasHadCongestionEvent || LossEvent.LargestPacketNumberLost > Cubic.RecoverySentPacketNumber)
            {
                Cubic.RecoverySentPacketNumber = LossEvent.LargestSentPacketNumber;
                CubicCongestionControlOnCongestionEvent(Cc, LossEvent.PersistentCongestion, false);
                CubicCongestionHyStartChangeState(Cc, QUIC_CUBIC_HYSTART_STATE.HYSTART_DONE);
            }

            NetLog.Assert(Cubic.BytesInFlight >= LossEvent.NumRetransmittableBytes);
            Cubic.BytesInFlight -= LossEvent.NumRetransmittableBytes;

            CubicCongestionControlUpdateBlockedState(Cc, PreviousCanSendState);
        }

        static void CubicCongestionControlOnEcn(QUIC_CONGESTION_CONTROL Cc, QUIC_ECN_EVENT EcnEvent)
        {
            QUIC_CONGESTION_CONTROL_CUBIC Cubic = Cc.Cubic;

            bool PreviousCanSendState = CubicCongestionControlCanSend(Cc);

            if (!Cubic.HasHadCongestionEvent || EcnEvent.LargestPacketNumberAcked > Cubic.RecoverySentPacketNumber)
            {
                Cubic.RecoverySentPacketNumber = EcnEvent.LargestSentPacketNumber;
                QuicCongestionControlGetConnection(Cc).Stats.Send.EcnCongestionCount++;
                CubicCongestionControlOnCongestionEvent(Cc, false, true);
                CubicCongestionHyStartChangeState(Cc, QUIC_CUBIC_HYSTART_STATE.HYSTART_DONE);
            }

            CubicCongestionControlUpdateBlockedState(Cc, PreviousCanSendState);
        }

        static bool CubicCongestionControlOnSpuriousCongestionEvent(QUIC_CONGESTION_CONTROL Cc)
        {
            QUIC_CONGESTION_CONTROL_CUBIC Cubic = Cc.Cubic;

            if (!Cubic.IsInRecovery)
            {
                return false;
            }

            QUIC_CONNECTION Connection = QuicCongestionControlGetConnection(Cc);
            bool PreviousCanSendState = QuicCongestionControlCanSend(Cc);

            Cubic.WindowPrior = Cubic.PrevWindowPrior;
            Cubic.WindowMax = Cubic.PrevWindowMax;
            Cubic.WindowLastMax = Cubic.PrevWindowLastMax;
            Cubic.KCubic = Cubic.PrevKCubic;
            Cubic.SlowStartThreshold = Cubic.PrevSlowStartThreshold;
            Cubic.CongestionWindow = Cubic.PrevCongestionWindow;
            Cubic.AimdWindow = Cubic.PrevAimdWindow;

            Cubic.IsInRecovery = false;
            Cubic.HasHadCongestionEvent = false;

            bool Result = CubicCongestionControlUpdateBlockedState(Cc, PreviousCanSendState);
            return Result;
        }

        static void CubicCongestionControlLogOutFlowStatus(QUIC_CONGESTION_CONTROL Cc)
        {
            //这里是日志，就忽略了
        }

        static long CubicCongestionControlGetBytesInFlightMax(QUIC_CONGESTION_CONTROL Cc)
        {
            return Cc.Cubic.BytesInFlightMax;
        }

        static byte CubicCongestionControlGetExemptions(QUIC_CONGESTION_CONTROL Cc)
        {
            return Cc.Cubic.Exemptions;
        }

        static long CubicCongestionControlGetCongestionWindow(QUIC_CONGESTION_CONTROL Cc)
        {
            return Cc.Cubic.CongestionWindow;
        }

        static bool CubicCongestionControlIsAppLimited(QUIC_CONGESTION_CONTROL Cc)
        {
            return false;
        }

        static void CubicCongestionControlSetAppLimited(QUIC_CONGESTION_CONTROL Cc)
        {

        }
    }
}
