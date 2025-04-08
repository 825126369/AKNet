using AKNet.Common;
using System;
using System.Collections.Generic;

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

    }
}
