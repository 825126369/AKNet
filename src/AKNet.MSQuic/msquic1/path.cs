/************************************Copyright*****************************************
*        ProjectName:AKNet
*        Web:https://github.com/825126369/AKNet
*        Description:C#游戏网络库
*        Author:许珂
*        StartTime:2024/11/01 00:00:00
*        ModifyTime:2025/11/30 19:43:18
*        Copyright:MIT软件许可证
************************************Copyright*****************************************/
using AKNet.Common;
using System;
using System.Diagnostics;
using System.IO;
using System.Security.Cryptography;

namespace MSQuic1
{
    internal enum ECN_VALIDATION_STATE
    {
        ECN_VALIDATION_TESTING, //正在测试 ECN 功能
        ECN_VALIDATION_UNKNOWN, //ECN 状态未知，尚未确定是否支持
        ECN_VALIDATION_CAPABLE, //已确认支持 ECN
        ECN_VALIDATION_FAILED,  //ECN 验证失败，或者应用程序未启用 ECN
    }

    internal enum QUIC_PATH_VALID_REASON
    {
        QUIC_PATH_VALID_INITIAL_TOKEN,
        QUIC_PATH_VALID_HANDSHAKE_PACKET,
        QUIC_PATH_VALID_PATH_RESPONSE
    }

    internal class QUIC_PATH
    {
        public readonly QUIC_MTU_DISCOVERY MtuDiscovery = new QUIC_MTU_DISCOVERY();
        public readonly CXPLAT_ROUTE Route = new CXPLAT_ROUTE();
        public QUIC_BINDING Binding;

        public byte ID;
        public bool InUse;
        public bool IsActive;
        public bool InitiatedCidUpdate;
        public bool GotFirstRttSample;
        public bool GotValidPacket;
        public bool IsPeerValidated; //将当前路径（Path）标记为“对端已验证”状态，表示该路径是可达且安全的，可以用于传输数据。
        public bool IsMinMtuValidated;
        public bool SpinBit;
        public bool SendChallenge;
        public bool SendResponse;
        public bool PartitionUpdated;
        public ECN_VALIDATION_STATE EcnValidationState;
        public bool EncryptionOffloading;
        public long EcnTestingEndingTime;
        public ushort Mtu;
        public ushort LocalMtu;
        public QUIC_CID DestCid;

        public long SmoothedRtt;
        public long LatestRttSample;
        public long MinRtt;
        public long MaxRtt;
        public long RttVariance;
        public long OneWayDelay;
        public long OneWayDelayLatest;
        
        public long Allowance;
        public readonly byte[] Response = new byte[8];
        public readonly byte[] Challenge = new byte[8];
        public long PathValidationStartTime;

        public QUIC_PATH()
        {
            MtuDiscovery.mQUIC_PATH = this;
        }
    }

    internal static partial class MSQuicFunc
    {
        static void QuicPathInitialize(QUIC_CONNECTION Connection, QUIC_PATH Path)
        {
            Path.ID = (byte)Connection.NextPathId++;
            Path.InUse = true;
            Path.MinRtt = long.MaxValue;
            Path.Mtu = Connection.Settings.MinimumMtu;
            Path.SmoothedRtt = MS_TO_US(Connection.Settings.InitialRttMs);
            Path.RttVariance = Path.SmoothedRtt / 2;
            Path.EcnValidationState = Connection.Settings.EcnEnabled ? ECN_VALIDATION_STATE.ECN_VALIDATION_TESTING : ECN_VALIDATION_STATE.ECN_VALIDATION_FAILED;
        }

        static QUIC_PATH QuicConnGetPathByID(QUIC_CONNECTION Connection, byte ID, out int Index)
        {
            Index = -1;
            for (int i = 0; i < Connection.PathsCount; ++i)
            {
                if (Connection.Paths[i].ID == ID)
                {
                    Index = i;
                    return Connection.Paths[i];
                }
            }
            return null;
        }

        static ushort QuicPathGetDatagramPayloadSize(QUIC_PATH Path)
        {
            return MaxUdpPayloadSizeForFamily(QuicAddrGetFamily(Path.Route.RemoteAddress), Path.Mtu);
        }

        static void QuicPathRemove(QUIC_CONNECTION Connection, int Index)
        {
            NetLog.Assert(Connection.PathsCount > 0);
            NetLog.Assert(Connection.PathsCount <= QUIC_MAX_PATH_COUNT);
            if (Index >= Connection.PathsCount)
            {
                NetLog.Assert(Index < Connection.PathsCount, "Invalid path removal!");
                return;
            }

            QUIC_PATH Path = Connection.Paths[Index];
            NetLog.Assert(Path.InUse);

#if DEBUG
            if (Path.DestCid != null)
            {
                QUIC_CID_CLEAR_PATH(Path.DestCid);
            }
#endif
            if (Index + 1 < Connection.PathsCount)
            {
                Connection.Paths.AsSpan().Slice(Index + 1, Connection.PathsCount - Index - 1).CopyTo(Connection.Paths.AsSpan().Slice(Index));
            }

            Connection.PathsCount--;
            Connection.Paths[Connection.PathsCount].InUse = false;
        }
        
        static QUIC_PATH QuicConnGetPathForPacket(QUIC_CONNECTION Connection, QUIC_RX_PACKET Packet)
        {
            for (int i = 0; i < Connection.PathsCount; ++i)
            {
                if (!QuicAddrCompare(Packet.Route.LocalAddress, Connection.Paths[i].Route.LocalAddress) ||
                    !QuicAddrCompare(Packet.Route.RemoteAddress, Connection.Paths[i].Route.RemoteAddress))
                {
                    if (!Connection.State.HandshakeConfirmed)
                    {
                        return null;
                    }
                    continue;
                }
                return Connection.Paths[i];
            }

            if (Connection.PathsCount == QUIC_MAX_PATH_COUNT)
            {
                for (int i = Connection.PathsCount - 1; i > 0; i--)
                {
                    if (!Connection.Paths[i].IsActive && QuicAddrCompare(Packet.Route.LocalAddress, Connection.Paths[i].Route.LocalAddress))
                    {
                        QuicPathRemove(Connection, i);
                    }
                }

                if (Connection.PathsCount == QUIC_MAX_PATH_COUNT)
                {
                    return null;
                }
            }

            if (Connection.PathsCount > 1)
            {
                Connection.Paths.AsSpan().Slice(1, Connection.PathsCount - 1).CopyTo(Connection.Paths.AsSpan().Slice(2));
            }

            NetLog.Assert(Connection.PathsCount < QUIC_MAX_PATH_COUNT);
            QUIC_PATH Path = Connection.Paths[1];
            QuicPathInitialize(Connection, Path);
            Connection.PathsCount++;
            if (Connection.Paths[0].DestCid.Data.Length == 0)
            {
                Path.DestCid = Connection.Paths[0].DestCid;
            }
            Path.Binding = Connection.Paths[0].Binding;
            QuicCopyRouteInfo(Path.Route, Packet.Route);
            QuicPathValidate(Path);
            return Path;
        }

        //QuicPathSetAllowance 函数负责设置或更新与某个特定网络路径（Path）相关的这个 【允许发送的字节数】。
        static void QuicPathSetAllowance(QUIC_CONNECTION Connection,QUIC_PATH Path, long NewAllowance)
        {
            Path.Allowance = NewAllowance;
            bool IsBlocked = Path.Allowance < QUIC_MIN_SEND_ALLOWANCE;

            if (!Path.IsPeerValidated)
            {
                if (!IsBlocked)
                {
                    if (QuicConnRemoveOutFlowBlockedReason(Connection, QUIC_FLOW_BLOCKED_AMPLIFICATION_PROT))
                    {
                        if (Connection.Send.SendFlags != 0)
                        {
                            QuicSendQueueFlush(Connection.Send,  QUIC_SEND_FLUSH_REASON.REASON_AMP_PROTECTION);
                        }
                        QuicLossDetectionUpdateTimer(Connection.LossDetection, true);
                    }
                }
                else
                {
                    QuicConnAddOutFlowBlockedReason(Connection, QUIC_FLOW_BLOCKED_AMPLIFICATION_PROT);
                }
            }
        }


        static void QuicPathSetValid(QUIC_CONNECTION Connection, QUIC_PATH Path, QUIC_PATH_VALID_REASON Reason)
        {
            if (Path.IsPeerValidated)
            {
                return;
            }

            Path.IsPeerValidated = true;
            QuicPathSetAllowance(Connection, Path, int.MaxValue);

            if (Reason == QUIC_PATH_VALID_REASON.QUIC_PATH_VALID_PATH_RESPONSE)
            {
                QuicMtuDiscoveryPeerValidated(Path.MtuDiscovery, Connection);
            }
        }

        static void QuicPathIncrementAllowance(QUIC_CONNECTION Connection, QUIC_PATH Path, int Amount)
        {
            QuicPathSetAllowance(Connection, Path, Path.Allowance + Amount);
        }

        static void QuicPathSetActive(QUIC_CONNECTION Connection, QUIC_PATH Path)
        {
            bool UdpPortChangeOnly = false;
            if (Path == Connection.Paths[0])
            {
                NetLog.Assert(!Path.IsActive);
                Path.IsActive = true;
            }
            else
            {
                NetLog.Assert(Path.DestCid != null);
                UdpPortChangeOnly =
                    QuicAddrGetFamily(Path.Route.RemoteAddress) == QuicAddrGetFamily(Connection.Paths[0].Route.RemoteAddress) &&
                    QuicAddrCompareIp(Path.Route.RemoteAddress, Connection.Paths[0].Route.RemoteAddress);

                QUIC_PATH PrevActivePath = Connection.Paths[0];

                PrevActivePath.IsActive = false;
                Path.IsActive = true;
                if (UdpPortChangeOnly)
                {
                    Path.IsMinMtuValidated = PrevActivePath.IsMinMtuValidated;
                }

                Connection.Paths[0] = Path;
                Path = PrevActivePath;
            }

            if (!UdpPortChangeOnly)
            {
                QuicCongestionControlReset(Connection.CongestionControl, false);
            }

            NetLog.Assert(Path.DestCid != null);
            NetLog.Assert(!Path.DestCid.Retired);
        }

        //减少允许发送的数据量
        static void QuicPathDecrementAllowance(QUIC_CONNECTION Connection,QUIC_PATH Path, int Amount)
        {
            QuicPathSetAllowance(Connection, Path, Path.Allowance <= Amount ? 0 : (Path.Allowance - Amount));
        }
        
        [Conditional("DEBUG")]
        static void QuicPathValidate(QUIC_PATH Path)
        {
            NetLog.Assert(Path.DestCid == null ||
                Path.DestCid.Data.Length == 0 ||
                (Path.DestCid.AssignedPath == Path && Path.DestCid.UsedLocally)
            );
        }
    }
}
