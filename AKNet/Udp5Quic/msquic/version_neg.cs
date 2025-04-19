using AKNet.Common;
using System;
using System.Collections.Generic;
using System.ComponentModel;

namespace AKNet.Udp5Quic.Common
{
    internal class QUIC_VERSION_INFORMATION_V1
    {
        public uint ChosenVersion;
        public readonly List<uint> AvailableVersions;
    }

    internal class QUIC_COMPATIBLE_VERSION_MAP
    {
        public uint OriginalVersion;
        public uint CompatibleVersion;
    }

    internal static partial class MSQuicFunc
    {
        static readonly List<uint> DefaultSupportedVersionsList = new List<uint>() {
            QUIC_VERSION_2,
            QUIC_VERSION_1,
            QUIC_VERSION_MS_1,
            QUIC_VERSION_DRAFT_29,
        };

        static readonly QUIC_COMPATIBLE_VERSION_MAP[] CompatibleVersionsMap = new QUIC_COMPATIBLE_VERSION_MAP[] 
        {
            new QUIC_COMPATIBLE_VERSION_MAP(){OriginalVersion = QUIC_VERSION_MS_1, CompatibleVersion = QUIC_VERSION_1},
            new QUIC_COMPATIBLE_VERSION_MAP() {OriginalVersion = QUIC_VERSION_1, CompatibleVersion =  QUIC_VERSION_MS_1},
            new QUIC_COMPATIBLE_VERSION_MAP(){ OriginalVersion = QUIC_VERSION_1,CompatibleVersion = QUIC_VERSION_2}
        };

        static ulong QuicVersionNegotiationExtParseVersionInfo(QUIC_CONNECTION Connection, ReadOnlySpan<byte> Buffer, QUIC_VERSION_INFORMATION_V1 VersionInfo)
        {
            if (Buffer.Length < sizeof(uint))
            {
                return QUIC_STATUS_INVALID_PARAMETER;
            }

            VersionInfo.ChosenVersion = EndianBitConverter.ToUInt32(Buffer, 0);
            Buffer = Buffer.Slice(sizeof(uint));

            if (QuicConnIsServer(Connection))
            {
                if (Buffer.Length < sizeof(uint))
                {
                    return QUIC_STATUS_INVALID_PARAMETER;
                }
            }

            if (Buffer.Length % sizeof(uint) != 0)
            {
                return QUIC_STATUS_INVALID_PARAMETER;
            }

            VersionInfo.AvailableVersions.Clear();
            for (int i = 0; i < Buffer.Length; i+= sizeof(uint))
            {
                uint Version = EndianBitConverter.ToUInt32(Buffer, i);
                VersionInfo.AvailableVersions.Add(Version);
            }
            return QUIC_STATUS_SUCCESS;
        }

        static bool QuicVersionNegotiationExtAreVersionsCompatible(uint OriginalVersion, uint UpgradedVersion)
        {
            if (OriginalVersion == UpgradedVersion)
            {
                return true;
            }

            for (int i = 0; i < CompatibleVersionsMap.Length; ++i)
            {
                if (CompatibleVersionsMap[i].OriginalVersion == OriginalVersion)
                {
                    while (i < CompatibleVersionsMap.Length && CompatibleVersionsMap[i].OriginalVersion == OriginalVersion)
                    {
                        if (CompatibleVersionsMap[i].CompatibleVersion == UpgradedVersion)
                        {
                            return true;
                        }
                        ++i;
                    }
                    return false;
                }
            }
            return false;
        }

        static bool QuicVersionNegotiationExtIsVersionClientSupported(QUIC_CONNECTION Connection, uint Version)
        {
            if (Connection.Settings.IsSet.VersionSettings)
            {
                if (QuicIsVersionReserved(Version))
                {
                    return false;
                }
                for (int i = 0; i < Connection.Settings.VersionSettings.FullyDeployedVersions.Count; ++i)
                {
                    if (Connection.Settings.VersionSettings.FullyDeployedVersions[i] == Version)
                    {
                        return true;
                    }
                }
            }
            else
            {
                return QuicIsVersionSupported(Version);
            }
            return false;
        }

        static bool QuicVersionNegotiationExtIsVersionCompatible(QUIC_CONNECTION Connection, uint NegotiatedVersion)
        {
            if (Connection.Settings.IsSet.VersionSettings)
            {
                List<uint> CompatibleVersions = Connection.Settings.VersionSettings.FullyDeployedVersions;
                for (int i = 0; i < CompatibleVersions.Count; ++i)
                {
                    if (QuicVersionNegotiationExtAreVersionsCompatible(CompatibleVersions[i], NegotiatedVersion))
                    {
                        return true;
                    }
                }
            }
            else
            {
                for (int i = 0; i < MsQuicLib.DefaultCompatibilityList.Count; ++i)
                {
                    if (MsQuicLib.DefaultCompatibilityList[i] == NegotiatedVersion)
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        static byte[] QuicVersionNegotiationExtEncodeVersionInfo(QUIC_CONNECTION Connection, int VerInfoLength)
        {
            int VILen = 0;
            byte[] VIBuf = null;
            byte[] VersionInfo = null;
            VerInfoLength = 0;

            if (QuicConnIsServer(Connection))
            {
                List<uint> AvailableVersionsList = new List<uint>();
                int AvailableVersionsListLength = 0;
                if (MsQuicLib.Settings.IsSet.VersionSettings)
                {
                    AvailableVersionsList = MsQuicLib.Settings.VersionSettings.FullyDeployedVersions;
                    AvailableVersionsListLength = MsQuicLib.Settings.VersionSettings.FullyDeployedVersions.Count;
                }
                else
                {
                    AvailableVersionsList = DefaultSupportedVersionsList;
                    AvailableVersionsListLength = DefaultSupportedVersionsList.Count;
                }

                VILen = sizeof(uint) + (AvailableVersionsListLength * sizeof(uint));
                NetLog.Assert((AvailableVersionsListLength * sizeof(uint)) + sizeof(uint) > AvailableVersionsListLength + sizeof(uint));

                VersionInfo = new byte[VILen];
                if (VersionInfo == null)
                {
                    return null;
                }
                VIBuf = VersionInfo;

                NetLog.Assert(VILen >= sizeof(uint));

                Connection.Stats.QuicVersion
        CxPlatCopyMemory(VIBuf, Connection.Stats.QuicVersion, sizeof(Connection.Stats.QuicVersion));


                VIBuf += sizeof(Connection->Stats.QuicVersion);
                CXPLAT_DBG_ASSERT(VILen - sizeof(uint32_t) == AvailableVersionsListLength * sizeof(uint32_t));
                CxPlatCopyMemory(
                    VIBuf,
                    AvailableVersionsList,
                    AvailableVersionsListLength * sizeof(uint32_t));

                QuicTraceLogConnInfo(
                    ServerVersionNegotiationInfoEncoded,
                    Connection,
                    "Server VI Encoded: Chosen Ver:%x Other Ver Count:%u",
                    Connection->Stats.QuicVersion,
                    AvailableVersionsListLength);

                QuicTraceEvent(
                    ConnVNEOtherVersionList,
                    "[conn][%p] VerInfo Available Versions List: %!VNL!",
                    Connection,
                    CASTED_CLOG_BYTEARRAY(AvailableVersionsListLength * sizeof(uint32_t), VIBuf));
            } else
            {
                //
                // Generate Client Version Info.
                //
                uint32_t CompatibilityListByteLength = 0;
                VILen = sizeof(Connection->Stats.QuicVersion);
                if (Connection->Settings.IsSet.VersionSettings)
                {
                    QuicVersionNegotiationExtGenerateCompatibleVersionsList(
                        Connection->Stats.QuicVersion,
                        Connection->Settings.VersionSettings->FullyDeployedVersions,
                        Connection->Settings.VersionSettings->FullyDeployedVersionsLength,
                        NULL, &CompatibilityListByteLength);
                    VILen += CompatibilityListByteLength;
                }
                else
                {
                    CXPLAT_DBG_ASSERT(MsQuicLib.DefaultCompatibilityListLength * (uint32_t)sizeof(uint32_t) > MsQuicLib.DefaultCompatibilityListLength);
                    VILen +=
                        MsQuicLib.DefaultCompatibilityListLength * sizeof(uint32_t);
                }

                VersionInfo = CXPLAT_ALLOC_NONPAGED(VILen, QUIC_POOL_VERSION_INFO);
                if (VersionInfo == NULL)
                {
                    QuicTraceEvent(
                        AllocFailure,
                        "Allocation of '%s' failed. (%llu bytes)",
                        "Client Version Info",
                        VILen);
                    return NULL;
                }
                VIBuf = VersionInfo;

                CXPLAT_DBG_ASSERT(VILen >= sizeof(uint32_t));
                CxPlatCopyMemory(VIBuf, &Connection->Stats.QuicVersion, sizeof(Connection->Stats.QuicVersion));
                VIBuf += sizeof(Connection->Stats.QuicVersion);
                if (Connection->Settings.IsSet.VersionSettings)
                {
                    uint32_t RemainingBuffer = VILen - (uint32_t)(VIBuf - VersionInfo);
                    CXPLAT_DBG_ASSERT(RemainingBuffer == CompatibilityListByteLength);
                    QuicVersionNegotiationExtGenerateCompatibleVersionsList(
                        Connection->Stats.QuicVersion,
                        Connection->Settings.VersionSettings->FullyDeployedVersions,
                        Connection->Settings.VersionSettings->FullyDeployedVersionsLength,
                        VIBuf,
                        &RemainingBuffer);
                    CXPLAT_DBG_ASSERT(VILen == (uint32_t)(VIBuf - VersionInfo) + RemainingBuffer);
                }
                else
                {
                    CXPLAT_DBG_ASSERT(VILen - sizeof(uint32_t) == MsQuicLib.DefaultCompatibilityListLength * sizeof(uint32_t));
                    CxPlatCopyMemory(
                        VIBuf,
                        MsQuicLib.DefaultCompatibilityList,
                        MsQuicLib.DefaultCompatibilityListLength * sizeof(uint32_t));
                }
                QuicTraceLogConnInfo(
                    ClientVersionInfoEncoded,
                    Connection,
                    "Client VI Encoded: Current Ver:%x Prev Ver:%x Compat Ver Count:%u",
                    Connection->Stats.QuicVersion,
                    Connection->PreviousQuicVersion,
                    CompatibilityListByteLength == 0 ?
                        MsQuicLib.DefaultCompatibilityListLength :
                        (uint32_t)(CompatibilityListByteLength / sizeof(uint32_t)));

                QuicTraceEvent(
                    ConnVNEOtherVersionList,
                    "[conn][%p] VerInfo Available Versions List: %!VNL!",
                    Connection,
                    CASTED_CLOG_BYTEARRAY(
                        CompatibilityListByteLength == 0 ?
                            MsQuicLib.DefaultCompatibilityListLength * sizeof(uint32_t) :
                            CompatibilityListByteLength,
                        VIBuf));
            }
            *VerInfoLength = VILen;
            return VersionInfo;
        }

    }
}
