using AKNet.Common;
using System;
using System.IO;
using static System.Net.WebRequestMethods;

namespace AKNet.Udp5Quic.Common
{
    internal enum ECN_VALIDATION_STATE
    {
        ECN_VALIDATION_TESTING,
        ECN_VALIDATION_UNKNOWN,
        ECN_VALIDATION_CAPABLE,
        ECN_VALIDATION_FAILED, // or not enabled by the app.
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

        static void QuicPathUpdateQeo(QUIC_CONNECTION Connection,QUIC_PATH Path,CXPLAT_QEO_OPERATION Operation)
        {
            QUIC_CID_HASH_ENTRY SourceCid = CXPLAT_CONTAINING_RECORD(Connection.SourceCids.Next);
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
                    KeyPhase = 0, // KeyPhase
                    RESERVED = 0, // Reserved
                    CipherType = CXPLAT_QEO_CIPHER_TYPE.CXPLAT_QEO_CIPHER_TYPE_AEAD_AES_256_GCM,
                    NextPacketNumber = 0, // NextPacketNumber
                    Address = Path.Route.LocalAddress,
                   ConnectionIdLength = SourceCid.CID.Length,
                }
            };


            Array.Copy(Path.DestCid.CID.Data, Offloads[0].ConnectionId, Path.DestCid.CID.Length);
            Array.Copy(SourceCid.CID.Data, Offloads[1].ConnectionId, SourceCid.CID.Length);

            if (Operation ==  CXPLAT_QEO_OPERATION.CXPLAT_QEO_OPERATION_ADD)
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
    }
}
