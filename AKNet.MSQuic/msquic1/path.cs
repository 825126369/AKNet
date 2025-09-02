using AKNet.Common;
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
        public byte ID;
        public bool InUse;
        public bool IsActive;
        public bool InitiatedCidUpdate;
        public bool GotFirstRttSample;
        public bool GotValidPacket;
        public bool IsPeerValidated;
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
        public readonly QUIC_MTU_DISCOVERY MtuDiscovery = new QUIC_MTU_DISCOVERY();
        public QUIC_BINDING Binding;
        public readonly CXPLAT_ROUTE Route = new CXPLAT_ROUTE();
        public QUIC_CID DestCid;

        public long SmoothedRtt;
        public long LatestRttSample;
        public long MinRtt;
        public long MaxRtt;
        public long RttVariance;
        public long OneWayDelay;
        public long OneWayDelayLatest;

        public int Allowance;
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

        static QUIC_PATH QuicConnGetPathByID(QUIC_CONNECTION Connection, byte ID, ref int Index)
        {
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

        static void QuicPathUpdateQeo(QUIC_CONNECTION Connection, QUIC_PATH Path, CXPLAT_QEO_OPERATION Operation)
        {
            //QUIC_CID SourceCid = CXPLAT_CONTAINING_RECORD<QUIC_CID>(Connection.SourceCids.Next);
            //CXPLAT_QEO_CONNECTION[] Offloads = new CXPLAT_QEO_CONNECTION[2]
            //{
            //    new CXPLAT_QEO_CONNECTION()
            //    {
            //        Operation = Operation,
            //        Direction = CXPLAT_QEO_DIRECTION.CXPLAT_QEO_DIRECTION_TRANSMIT,
            //        DecryptFailureAction = CXPLAT_QEO_DECRYPT_FAILURE_ACTION.CXPLAT_QEO_DECRYPT_FAILURE_ACTION_DROP,
            //        KeyPhase = 0,
            //        RESERVED = 0,
            //        CipherType = CXPLAT_QEO_CIPHER_TYPE.CXPLAT_QEO_CIPHER_TYPE_AEAD_AES_256_GCM,
            //        NextPacketNumber = Connection.Send.NextPacketNumber,
            //        Address = Path.Route.RemoteAddress,
            //        ConnectionIdLength = Path.DestCid.CID.Length,
            //    },
            //    new CXPLAT_QEO_CONNECTION()
            //    {
            //        Operation = Operation,
            //        Direction = CXPLAT_QEO_DIRECTION.CXPLAT_QEO_DIRECTION_RECEIVE,
            //        DecryptFailureAction = CXPLAT_QEO_DECRYPT_FAILURE_ACTION.CXPLAT_QEO_DECRYPT_FAILURE_ACTION_DROP,
            //        KeyPhase = 0,
            //        RESERVED = 0,
            //        CipherType = CXPLAT_QEO_CIPHER_TYPE.CXPLAT_QEO_CIPHER_TYPE_AEAD_AES_256_GCM,
            //        NextPacketNumber = 0,
            //        Address = Path.Route.LocalAddress,
            //       ConnectionIdLength = SourceCid.CID.Length,
            //    }
            //};


            //Path.DestCid.CID.Data.CopyTo(Offloads[0].ConnectionId);
            //SourceCid.CID.Data.CopyTo(Offloads[1].ConnectionId);

            //if (Operation == CXPLAT_QEO_OPERATION.CXPLAT_QEO_OPERATION_ADD)
            //{
            //    NetLog.Assert(Connection.Packets[(int)QUIC_ENCRYPT_LEVEL.QUIC_ENCRYPT_LEVEL_1_RTT] != null);
            //    Offloads[0].KeyPhase = Connection.Packets[(int)QUIC_ENCRYPT_LEVEL.QUIC_ENCRYPT_LEVEL_1_RTT].CurrentKeyPhase;
            //    Offloads[1].KeyPhase = Connection.Packets[(int)QUIC_ENCRYPT_LEVEL.QUIC_ENCRYPT_LEVEL_1_RTT].CurrentKeyPhase;
            //    Offloads[1].NextPacketNumber = Connection.Packets[(int)QUIC_ENCRYPT_LEVEL.QUIC_ENCRYPT_LEVEL_1_RTT].AckTracker.LargestPacketNumberAcknowledged;
            //    if (QuicTlsPopulateOffloadKeys(Connection.Crypto.TLS, Connection.Crypto.TlsState.WriteKeys[(int)QUIC_PACKET_KEY_TYPE.QUIC_PACKET_KEY_1_RTT], "Tx offload", Offloads[0]) &&
            //        QuicTlsPopulateOffloadKeys(Connection.Crypto.TLS, Connection.Crypto.TlsState.ReadKeys[(int)QUIC_PACKET_KEY_TYPE.QUIC_PACKET_KEY_1_RTT], "Rx offload", Offloads[1]) &&
            //        QUIC_SUCCEEDED(CxPlatSocketUpdateQeo(Path.Binding.Socket, Offloads, 2)))
            //    {
            //        Connection.Stats.EncryptionOffloaded = true;
            //        Path.EncryptionOffloading = true;
            //    }
            //}
            //else
            //{
            //    NetLog.Assert(Path.EncryptionOffloading);
            //    CxPlatSocketUpdateQeo(Path.Binding.Socket, Offloads, 2);
            //    Path.EncryptionOffloading = false;
            //}
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
                for (int i = 0; i < Connection.PathsCount - Index - 1; i++)
                {
                    Connection.Paths[Index + i] = Connection.Paths[Index + 1 + i];
                }
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
                    if (!Connection.Paths[i].IsActive
                        && QuicAddrGetFamily(Packet.Route.RemoteAddress) == QuicAddrGetFamily(Connection.Paths[i].Route.RemoteAddress)
                        && QuicAddrCompareIp(Packet.Route.RemoteAddress, Connection.Paths[i].Route.RemoteAddress)
                        && QuicAddrCompare(Packet.Route.LocalAddress, Connection.Paths[i].Route.LocalAddress))
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
                for (int i = 0; i < Connection.PathsCount - 1; i++)
                {
                    Connection.Paths[2 + i] = Connection.Paths[i + 1];
                }
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

        static void QuicPathSetAllowance(QUIC_CONNECTION Connection,QUIC_PATH Path, int NewAllowance)
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

            string[] ReasonStrings = {
                "Initial Token",
                "Handshake Packet",
                "Path Response"
            };

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

#if DEBUG
        static void QuicPathValidate(QUIC_PATH Path)
        {
            NetLog.Assert(Path.DestCid == null ||
                Path.DestCid.Data.Length == 0 ||
                (Path.DestCid.AssignedPath == Path && Path.DestCid.UsedLocally)
            );
        }
#else
        static void QuicPathValidate(QUIC_PATH Path) {}
#endif
    }
}
