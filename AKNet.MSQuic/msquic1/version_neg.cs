/************************************Copyright*****************************************
*        ProjectName:AKNet
*        Web:https://github.com/825126369/AKNet
*        Description:C#游戏网络库
*        Author:许珂
*        StartTime:2024/11/01 00:00:00
*        ModifyTime:2025/11/14 8:56:47
*        Copyright:MIT软件许可证
************************************Copyright*****************************************/
using AKNet.Common;
using System.Collections.Generic;

namespace MSQuic1
{
    internal class QUIC_VERSION_INFORMATION_V1
    {
        public uint ChosenVersion;
        public readonly List<uint> AvailableVersions = new List<uint>();
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
        };

        static readonly QUIC_COMPATIBLE_VERSION_MAP[] CompatibleVersionsMap = new QUIC_COMPATIBLE_VERSION_MAP[] 
        {
            new QUIC_COMPATIBLE_VERSION_MAP() { OriginalVersion = QUIC_VERSION_1, CompatibleVersion =  0},
            new QUIC_COMPATIBLE_VERSION_MAP() { OriginalVersion = QUIC_VERSION_1, CompatibleVersion = QUIC_VERSION_2}
        };

        static int QuicVersionNegotiationExtParseVersionInfo(QUIC_CONNECTION Connection, QUIC_SSBuffer Buffer, QUIC_VERSION_INFORMATION_V1 VersionInfo)
        {
            if (Buffer.Length < sizeof(uint))
            {
                return QUIC_STATUS_INVALID_PARAMETER;
            }

            VersionInfo.ChosenVersion = EndianBitConverter.ToUInt32(Buffer.GetSpan(), 0);
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
                uint Version = EndianBitConverter.ToUInt32(Buffer.GetSpan(), i);
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
            if (HasFlag(Connection.Settings.IsSetFlags, E_SETTING_FLAG_VersionSettings))
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
            if (HasFlag(Connection.Settings.IsSetFlags, E_SETTING_FLAG_VersionSettings))
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

        static QUIC_SSBuffer QuicVersionNegotiationExtEncodeVersionInfo(QUIC_CONNECTION Connection)
        {
            int VILen = 0;
            QUIC_SSBuffer VIBuf = QUIC_SSBuffer.Empty;
            byte[] VersionInfo = null;

            if (QuicConnIsServer(Connection))
            {
                List<uint> AvailableVersionsList = new List<uint>();
                int AvailableVersionsListLength = 0;
                if (HasFlag(MsQuicLib.Settings.IsSetFlags, E_SETTING_FLAG_VersionSettings))
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
                VIBuf = VersionInfo;
                NetLog.Assert(VILen >= sizeof(uint));

                EndianBitConverter.SetBytes(VIBuf.Buffer, VIBuf.Offset, Connection.Stats.QuicVersion);
                VIBuf += sizeof(uint);

                NetLog.Assert(VILen - sizeof(uint) == AvailableVersionsListLength * sizeof(uint));
                for (int i = 1; i < AvailableVersionsListLength; ++i)
                {
                    EndianBitConverter.SetBytes(VIBuf.Buffer, VIBuf.Offset, AvailableVersionsList[i]);
                    VIBuf += sizeof(uint);
                }
            }
            else
            {
                int CompatibilityListByteLength = 0;
                VILen = sizeof(uint);
                if (HasFlag(Connection.Settings.IsSetFlags, E_SETTING_FLAG_VersionSettings))
                {
                    QuicVersionNegotiationExtGenerateCompatibleVersionsList(
                        Connection.Stats.QuicVersion,
                        Connection.Settings.VersionSettings.FullyDeployedVersions,
                        out CompatibilityListByteLength);
                    VILen += CompatibilityListByteLength * sizeof(uint);
                }
                else
                {
                    VILen += MsQuicLib.DefaultCompatibilityList.Count * sizeof(uint);
                }

                VersionInfo = new byte[VILen];
                VIBuf = VersionInfo;
                NetLog.Assert(VILen >= sizeof(uint));
                EndianBitConverter.SetBytes(VIBuf.Buffer, VIBuf.Offset, Connection.Stats.QuicVersion);
                VIBuf += sizeof(uint);
                
                VIBuf += sizeof(uint);
                if (HasFlag(Connection.Settings.IsSetFlags, E_SETTING_FLAG_VersionSettings))
                {
                    int RemainingBuffer = VIBuf.Length;
                    NetLog.Assert(RemainingBuffer == CompatibilityListByteLength);
                    QuicVersionNegotiationExtGenerateCompatibleVersionsList(
                        Connection.Stats.QuicVersion,
                        Connection.Settings.VersionSettings.FullyDeployedVersions,
                        ref VIBuf);
                }
                else
                {
                    NetLog.Assert(VILen - sizeof(uint) == MsQuicLib.DefaultCompatibilityList.Count * sizeof(uint));
                    for (int i = 0; i < MsQuicLib.DefaultCompatibilityList.Count; i++)
                    {
                        EndianBitConverter.SetBytes(VIBuf.Buffer, VIBuf.Offset, MsQuicLib.DefaultCompatibilityList[i]);
                        VIBuf += sizeof(uint);
                    }
                }
            }
                
            return VersionInfo;
        }

        static ulong QuicVersionNegotiationExtGenerateCompatibleVersionsList(uint OriginalVersion, List<uint> FullyDeployedVersions, out int CompatibleVersionsListLength)
        {
            CompatibleVersionsListLength = 0;
            for (int i = 0; i < FullyDeployedVersions.Count; ++i)
            {
                for (int j = 0; j < CompatibleVersionsMap.Length; ++j)
                {
                    if (CompatibleVersionsMap[j].OriginalVersion == OriginalVersion && CompatibleVersionsMap[j].CompatibleVersion == FullyDeployedVersions[i])
                    {
                        CompatibleVersionsListLength++;
                        break;
                    }
                }
            }
            return QUIC_STATUS_SUCCESS;
        }

        static ulong QuicVersionNegotiationExtGenerateCompatibleVersionsList(uint OriginalVersion, List<uint> FullyDeployedVersions, List<uint> CompatibleVersionsList)
        {
            for (int i = 0; i < FullyDeployedVersions.Count; ++i)
            {
                for (int j = 0; j < CompatibleVersionsMap.Length; ++j)
                {
                    if (CompatibleVersionsMap[j].OriginalVersion == OriginalVersion && CompatibleVersionsMap[j].CompatibleVersion == FullyDeployedVersions[i])
                    {
                        CompatibleVersionsList.Add(CompatibleVersionsMap[i].CompatibleVersion);
                        break;
                    }
                }
            }
            return QUIC_STATUS_SUCCESS;
        }

        static ulong QuicVersionNegotiationExtGenerateCompatibleVersionsList(uint OriginalVersion, List<uint> FullyDeployedVersions, ref QUIC_SSBuffer Buffer)
        {
            int NeededBufferLength = sizeof(uint);
            for (int i = 0; i < FullyDeployedVersions.Count; ++i)
            {
                for (int j = 0; j < CompatibleVersionsMap.Length; ++j)
                {
                    if (CompatibleVersionsMap[j].OriginalVersion == OriginalVersion && CompatibleVersionsMap[j].CompatibleVersion == FullyDeployedVersions[i])
                    {
                        NeededBufferLength += sizeof(uint);
                        break;
                    }
                }
            }
            
            if (Buffer.Length < NeededBufferLength)
            {
                Buffer.Length = NeededBufferLength;
                return QUIC_STATUS_BUFFER_TOO_SMALL;
            }

            if (Buffer.Buffer == null)
            {
                return QUIC_STATUS_INVALID_PARAMETER;
            }

            int Offset = sizeof(uint);
            EndianBitConverter.SetBytes(Buffer.Buffer, Buffer.Offset, OriginalVersion);

            for (int i = 0; i < FullyDeployedVersions.Count; ++i)
            {
                for (int j = 0; j < CompatibleVersionsMap.Length; ++j)
                {
                    if (CompatibleVersionsMap[j].OriginalVersion == OriginalVersion && CompatibleVersionsMap[j].CompatibleVersion == FullyDeployedVersions[i])
                    {
                        EndianBitConverter.SetBytes(Buffer.Buffer, Buffer.Offset + Offset, CompatibleVersionsMap[i].CompatibleVersion);
                        Offset += sizeof(uint);
                        break;
                    }
                }
            }

            NetLog.Assert(Offset <= Buffer.Length);
            return QUIC_STATUS_SUCCESS;
        }


    }
}
