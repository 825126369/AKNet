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
        public ulong EcnTestingEndingTime;
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
        public ulong PathValidationStartTime;
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

        static ushort QuicPathGetDatagramPayloadSize(QUIC_PATH Path)
        {
            return MaxUdpPayloadSizeForFamily(QuicAddrGetFamily(Path.Route.RemoteAddress), Path.Mtu);
        }
    }
}
