/************************************Copyright*****************************************
*        ProjectName:AKNet
*        Web:https://github.com/825126369/AKNet
*        Description:C#游戏网络库
*        Author:许珂
*        StartTime:2024/11/01 00:00:00
*        ModifyTime:2026/2/1 20:26:57
*        Copyright:MIT软件许可证
************************************Copyright*****************************************/
using AKNet.Common;
using System.Text;

namespace MSQuic1
{
    /*
    Flags: 1143295
    IdleTimeout:0
    InitialMaxStreamDataBidiLocal:65536
    InitialMaxStreamDataBidiRemote:65536
    InitialMaxStreamDataUni:65536
    InitialMaxBidiStreams:255
    InitialMaxUniStreams:255
    MaxUdpPayloadSize:1472
    AckDelayExponent:8
    MaxAckDelay:41
    MinAckDelay:16000
    ActiveConnectionIdLimit:4
    MaxDatagramFrameSize:0
    CibirLength:0
    CibirOffset:0
    StatelessResetToken:2 189 51 90 249 99 152 41 221 204 142 64 18 155 151 183
    PreferredAddress:
    OriginalDestinationConnectionID:Offset: 0, Length: 8, Buffer: 112 150 137 100 150 161 60 39 0 0 0 0 0 0 0 0 0 0 0 0 
    */

    internal class QUIC_TRANSPORT_PARAMETERS : CXPLAT_POOL_Interface<QUIC_TRANSPORT_PARAMETERS>
    {
        public CXPLAT_POOL<QUIC_TRANSPORT_PARAMETERS> mPool = null;
        public readonly CXPLAT_POOL_ENTRY<QUIC_TRANSPORT_PARAMETERS> POOL_ENTRY = null;

        public uint Flags;
        public long IdleTimeout; //毫秒
        public long InitialMaxStreamDataBidiLocal;
        public long InitialMaxStreamDataBidiRemote;
        public long InitialMaxStreamDataUni;
        public long InitialMaxData;
        public int InitialMaxBidiStreams;
        public int InitialMaxUniStreams;
        public int MaxUdpPayloadSize;
        public byte AckDelayExponent;
        public long MaxAckDelay; //这个是毫秒
        public long MinAckDelay; //这个是微妙
        public int ActiveConnectionIdLimit;
        public int MaxDatagramFrameSize;
        public int CibirLength;
        public int CibirOffset;
        public readonly byte[] StatelessResetToken = new byte[MSQuicFunc.QUIC_STATELESS_RESET_TOKEN_LENGTH];
        public string PreferredAddress;
        public readonly QUIC_BUFFER OriginalDestinationConnectionID = new QUIC_BUFFER(MSQuicFunc.QUIC_MAX_CONNECTION_ID_LENGTH_V1);
        public readonly QUIC_BUFFER RetrySourceConnectionID = new QUIC_BUFFER(MSQuicFunc.QUIC_MAX_CONNECTION_ID_LENGTH_V1);
        public QUIC_BUFFER VersionInfo = new QUIC_BUFFER();
        public readonly QUIC_BUFFER InitialSourceConnectionID = new QUIC_BUFFER(MSQuicFunc.QUIC_MAX_CONNECTION_ID_LENGTH_V1);

        public override string ToString()
        {
            StringBuilder mBuilder = new StringBuilder();
            mBuilder.AppendLine("------------- QUIC_TRANSPORT_PARAMETERS -----------------------");
            mBuilder.AppendLine($"Flags: {Flags}");
            mBuilder.AppendLine($"IdleTimeout:{IdleTimeout}");
            mBuilder.AppendLine($"InitialMaxStreamDataBidiLocal:{InitialMaxStreamDataBidiLocal}");
            mBuilder.AppendLine($"InitialMaxStreamDataBidiRemote:{InitialMaxStreamDataBidiRemote}");
            mBuilder.AppendLine($"InitialMaxStreamDataUni:{InitialMaxStreamDataUni}");
            mBuilder.AppendLine($"InitialMaxBidiStreams:{InitialMaxBidiStreams}");
            mBuilder.AppendLine($"InitialMaxUniStreams:{InitialMaxUniStreams}");
            mBuilder.AppendLine($"MaxUdpPayloadSize:{MaxUdpPayloadSize}");
            mBuilder.AppendLine($"AckDelayExponent:{AckDelayExponent}");
            mBuilder.AppendLine($"MaxAckDelay:{MaxAckDelay}");
            mBuilder.AppendLine($"MinAckDelay:{MinAckDelay}");
            mBuilder.AppendLine($"ActiveConnectionIdLimit:{ActiveConnectionIdLimit}");
            mBuilder.AppendLine($"MaxDatagramFrameSize:{MaxDatagramFrameSize}");
            mBuilder.AppendLine($"CibirLength:{CibirLength}");
            mBuilder.AppendLine($"CibirOffset:{CibirOffset}");
            mBuilder.AppendLine($"StatelessResetToken:{CommonFunc.GetByteArrayStr(StatelessResetToken)}");
            mBuilder.AppendLine($"PreferredAddress:{PreferredAddress}");
            mBuilder.AppendLine($"OriginalDestinationConnectionID:{OriginalDestinationConnectionID}");
            mBuilder.AppendLine($"VersionInfo:{VersionInfo}");
            mBuilder.AppendLine($"InitialSourceConnectionID:{InitialSourceConnectionID}");
            return mBuilder.ToString();
        }

        public QUIC_TRANSPORT_PARAMETERS()
        {
            POOL_ENTRY = new CXPLAT_POOL_ENTRY<QUIC_TRANSPORT_PARAMETERS>(this);
        }
        public CXPLAT_POOL_ENTRY<QUIC_TRANSPORT_PARAMETERS> GetEntry()
        {
            return POOL_ENTRY;
        }

        public void Reset()
        {
            Flags = 0;
            IdleTimeout = 0;
            InitialMaxStreamDataBidiLocal = 0;
            InitialMaxStreamDataBidiRemote = 0;
            InitialMaxStreamDataUni = 0;
            InitialMaxData = 0;
            InitialMaxBidiStreams = 0;
            InitialMaxUniStreams = 0;
            MaxUdpPayloadSize = 0;
            AckDelayExponent = 0;
            MaxAckDelay = 0;
            MinAckDelay = 0;
            ActiveConnectionIdLimit = 0;
            MaxDatagramFrameSize = 0;
            CibirLength = 0;
            CibirOffset = 0;
            PreferredAddress = null;
            VersionInfo = null;
        }

        public void SetPool(CXPLAT_POOL<QUIC_TRANSPORT_PARAMETERS> mPool)
        {
            this.mPool = mPool;
        }

        public CXPLAT_POOL<QUIC_TRANSPORT_PARAMETERS> GetPool()
        {
            return this.mPool;
        }
    }
}
