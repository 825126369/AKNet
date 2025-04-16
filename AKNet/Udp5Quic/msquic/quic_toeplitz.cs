using AKNet.Common;
using System;
using System.Net;
using System.Net.Sockets;

namespace AKNet.Udp5Quic.Common
{
    internal class CXPLAT_TOEPLITZ_LOOKUP_TABLE
    {
        public uint[] Table = new uint[MSQuicFunc.CXPLAT_TOEPLITZ_LOOKUP_TABLE_SIZE];
    }

    internal class CXPLAT_TOEPLITZ_HASH
    {
        public CXPLAT_TOEPLITZ_LOOKUP_TABLE[] LookupTableArray = new CXPLAT_TOEPLITZ_LOOKUP_TABLE[MSQuicFunc.CXPLAT_TOEPLITZ_LOOKUP_TABLE_COUNT];
        public byte[] HashKey = new byte[MSQuicFunc.CXPLAT_TOEPLITZ_KEY_SIZE];
    }

    internal static partial class MSQuicFunc
    {
        public const int NIBBLES_PER_BYTE = 2;
        public const int BITS_PER_NIBBLE = 4;
        public const int CXPLAT_TOEPLITZ_INPUT_SIZE = 38;
        public const int CXPLAT_TOEPLITZ_OUPUT_SIZE = sizeof(uint);
        public const int CXPLAT_TOEPLITZ_KEY_SIZE = (CXPLAT_TOEPLITZ_INPUT_SIZE + CXPLAT_TOEPLITZ_OUPUT_SIZE);
        public const int CXPLAT_TOEPLITZ_LOOKUP_TABLE_SIZE = 16;
        public const int CXPLAT_TOEPLITZ_LOOKUP_TABLE_COUNT = (CXPLAT_TOEPLITZ_INPUT_SIZE * NIBBLES_PER_BYTE);

        static void CxPlatToeplitzHashComputeAddr(CXPLAT_TOEPLITZ_HASH Toeplitz, QUIC_ADDR Addr, ref int Key, ref int Offset)
        {
            if (QuicAddrGetFamily(Addr) == AddressFamily.InterNetwork)
            {
                Key ^= CxPlatToeplitzHashCompute(Toeplitz,
                        ((uint8_t*)Addr) + QUIC_ADDR_V4_PORT_OFFSET,
                        2, 0);

                Key ^=
                    CxPlatToeplitzHashCompute(
                        Toeplitz,
                        ((uint8_t*)Addr) + QUIC_ADDR_V4_IP_OFFSET,
                        4, 2);
                Offset = 2 + 4;
            }
            else
            {
                Key ^=
                    CxPlatToeplitzHashCompute(
                        Toeplitz,
                        ((uint8_t*)Addr) + QUIC_ADDR_V6_PORT_OFFSET,
                        2, 0);
                Key ^=
                    CxPlatToeplitzHashCompute(
                        Toeplitz,
                        ((uint8_t*)Addr) + QUIC_ADDR_V6_IP_OFFSET,
                        16, 2);
                Offset = 2 + 16;
            }
        }

        static uint CxPlatToeplitzHashCompute(CXPLAT_TOEPLITZ_HASH Toeplitz,  byte[] HashInput, int HashInputLength, int HashInputOffset)
        {
            int BaseOffset = HashInputOffset * NIBBLES_PER_BYTE;
            uint Result = 0;

            NetLog.Assert((BaseOffset + HashInputLength * NIBBLES_PER_BYTE) <= CXPLAT_TOEPLITZ_LOOKUP_TABLE_COUNT);
            for (int i = 0; i < HashInputLength; i++)
            {
                Result ^= Toeplitz.LookupTableArray[BaseOffset].Table[(HashInput[i] >> 4) & 0xf];
                BaseOffset++;
                Result ^= Toeplitz.LookupTableArray[BaseOffset].Table[HashInput[i] & 0xf];
                BaseOffset++;
            }

            return Result;
        }
    }
}
