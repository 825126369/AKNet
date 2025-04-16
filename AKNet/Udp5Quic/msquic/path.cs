using AKNet.Common;
using System;
using System.IO;

namespace AKNet.Udp5Quic.Common
{
    internal enum ECN_VALIDATION_STATE
    {
        ECN_VALIDATION_TESTING,
        ECN_VALIDATION_UNKNOWN,
        ECN_VALIDATION_CAPABLE,
        ECN_VALIDATION_FAILED, // or not enabled by the app.
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
        public byte PartitionUpdated;
        public ECN_VALIDATION_STATE EcnValidationState;
        public bool EncryptionOffloading;
        public long EcnTestingEndingTime;
        public ushort Mtu;
        public ushort LocalMtu;
        public QUIC_MTU_DISCOVERY MtuDiscovery;
        public QUIC_BINDING Binding;
        public CXPLAT_ROUTE Route;
        public QUIC_CID_LIST_ENTRY DestCid;

        public long SmoothedRtt;
        public long LatestRttSample;
        public long MinRtt;
        public long MaxRtt;
        public long RttVariance;
        public long OneWayDelay;
        public long OneWayDelayLatest;

        public uint Allowance;
        public byte[] Response = new byte[8];
        public byte[] Challenge = new byte[8];
        public long PathValidationStartTime;
    }

    internal static partial class MSQuicFunc
    {
        static void QuicPathInitialize(QUIC_CONNECTION Connection, QUIC_PATH Path)
        {
            Path.ID = Connection.NextPathId++;
            Path.InUse = true;
            Path.MinRtt = long.MaxValue;
            Path.Mtu = Connection.Settings.MinimumMtu;
            Path.SmoothedRtt = Connection.Settings.InitialRttMs;
            Path.RttVariance = Path.SmoothedRtt / 2;
            Path.EcnValidationState = Connection.Settings.EcnEnabled ? ECN_VALIDATION_STATE.ECN_VALIDATION_TESTING : ECN_VALIDATION_STATE.ECN_VALIDATION_FAILED;

            if (MsQuicLib.ExecutionConfig && BoolOk(MsQuicLib.ExecutionConfig.Flags & QUIC_EXECUTION_CONFIG_FLAG_QTIP))
            {
                Path.Route.TcpState.SequenceNumber = RandomTool.Random(uint.MinValue, uint.MaxValue);
            }
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
            QUIC_CID_HASH_ENTRY SourceCid = CXPLAT_CONTAINING_RECORD<QUIC_CID_HASH_ENTRY>(Connection.SourceCids.Next);
            CXPLAT_QEO_CONNECTION[] Offloads = new CXPLAT_QEO_CONNECTION[2]
            {
                new CXPLAT_QEO_CONNECTION()
                {
                    Operation = Operation,
                    Direction = CXPLAT_QEO_DIRECTION.CXPLAT_QEO_DIRECTION_TRANSMIT,
                    DecryptFailureAction = CXPLAT_QEO_DECRYPT_FAILURE_ACTION.CXPLAT_QEO_DECRYPT_FAILURE_ACTION_DROP,
                    KeyPhase = 0,
                    RESERVED = 0,
                    CipherType = CXPLAT_QEO_CIPHER_TYPE.CXPLAT_QEO_CIPHER_TYPE_AEAD_AES_256_GCM,
                    NextPacketNumber = Connection.Send.NextPacketNumber,
                    Address = Path.Route.RemoteAddress,
                    ConnectionIdLength = Path.DestCid.CID.Length,
                },
                new CXPLAT_QEO_CONNECTION()
                {
                    Operation = Operation,
                    Direction = CXPLAT_QEO_DIRECTION.CXPLAT_QEO_DIRECTION_RECEIVE,
                    DecryptFailureAction = CXPLAT_QEO_DECRYPT_FAILURE_ACTION.CXPLAT_QEO_DECRYPT_FAILURE_ACTION_DROP,
                    KeyPhase = 0, 
                    RESERVED = 0, 
                    CipherType = CXPLAT_QEO_CIPHER_TYPE.CXPLAT_QEO_CIPHER_TYPE_AEAD_AES_256_GCM,
                    NextPacketNumber = 0,
                    Address = Path.Route.LocalAddress,
                   ConnectionIdLength = SourceCid.CID.Length,
                }
            };


            Array.Copy(Path.DestCid.CID.Data, Offloads[0].ConnectionId, Path.DestCid.CID.Length);
            Array.Copy(SourceCid.CID.Data, Offloads[1].ConnectionId, SourceCid.CID.Length);

            if (Operation == CXPLAT_QEO_OPERATION.CXPLAT_QEO_OPERATION_ADD)
            {
                NetLog.Assert(Connection.Packets[(int)QUIC_ENCRYPT_LEVEL.QUIC_ENCRYPT_LEVEL_1_RTT] != null);
                Offloads[0].KeyPhase = Connection.Packets[(int)QUIC_ENCRYPT_LEVEL.QUIC_ENCRYPT_LEVEL_1_RTT].CurrentKeyPhase;
                Offloads[1].KeyPhase = Connection.Packets[(int)QUIC_ENCRYPT_LEVEL.QUIC_ENCRYPT_LEVEL_1_RTT].CurrentKeyPhase;
                Offloads[1].NextPacketNumber = Connection.Packets[(int)QUIC_ENCRYPT_LEVEL.QUIC_ENCRYPT_LEVEL_1_RTT].AckTracker.LargestPacketNumberAcknowledged;
                if (QuicTlsPopulateOffloadKeys(Connection.Crypto.TLS, Connection.Crypto.TlsState.WriteKeys[(int)QUIC_PACKET_KEY_TYPE.QUIC_PACKET_KEY_1_RTT], "Tx offload", Offloads[0]) &&
                    QuicTlsPopulateOffloadKeys(Connection.Crypto.TLS, Connection.Crypto.TlsState.ReadKeys[(int)QUIC_PACKET_KEY_TYPE.QUIC_PACKET_KEY_1_RTT], "Rx offload", Offloads[1]) &&
                    QUIC_SUCCEEDED(CxPlatSocketUpdateQeo(Path.Binding.Socket, Offloads, 2)))
                {
                    Connection.Stats.EncryptionOffloaded = true;
                    Path.EncryptionOffloading = true;
                }
            }
            else
            {
                NetLog.Assert(Path.EncryptionOffloading);
                CxPlatSocketUpdateQeo(Path.Binding.Socket, Offloads, 2);
                Path.EncryptionOffloading = false;
            }
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
                for(int i = 0; i < Connection.PathsCount - 1; i++)
                {
                    Connection.Paths[2 + i] = Connection.Paths[i + 1];
                }
            }

            NetLog.Assert(Connection.PathsCount < QUIC_MAX_PATH_COUNT);
            QUIC_PATH Path = Connection.Paths[1];
            QuicPathInitialize(Connection, Path);
            Connection.PathsCount++;

            if (Connection.Paths[0].DestCid.CID.Length == 0)
            {
                Path.DestCid = Connection.Paths[0].DestCid; // TODO - Copy instead?
            }
            Path.Binding = Connection.Paths[0].Binding;
            QuicCopyRouteInfo(Path.Route, Packet.Route);
            return Path;
        }

        static void QuicPathSetAllowance(QUIC_CONNECTION Connection,QUIC_PATH Path, uint NewAllowance)
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
            QuicPathSetAllowance(Connection, Path, uint.MaxValue);

            if (Reason == QUIC_PATH_VALID_REASON.QUIC_PATH_VALID_PATH_RESPONSE)
            {
                QuicMtuDiscoveryPeerValidated(Path.MtuDiscovery, Connection);
            }
        }
    }
}
