using AKNet.Common;
using System;
using System.Net.Sockets;
using System.Runtime.InteropServices;

namespace AKNet.Udp1MSQuic.Common
{
    internal enum CXPLAT_TOEPLITZ_INPUT_SIZE
    {
        CXPLAT_TOEPLITZ_INPUT_SIZE_IP = 36,
        CXPLAT_TOEPLITZ_INPUT_SIZE_QUIC = 38,
        CXPLAT_TOEPLITZ_INPUT_SIZE_MAX = 38,
    }

    internal class CXPLAT_TOEPLITZ_LOOKUP_TABLE
    {
        public readonly uint[] Table = new uint[MSQuicFunc.CXPLAT_TOEPLITZ_LOOKUP_TABLE_SIZE];
    }

    internal class CXPLAT_TOEPLITZ_HASH
    {
        public CXPLAT_TOEPLITZ_LOOKUP_TABLE[] LookupTableArray = new CXPLAT_TOEPLITZ_LOOKUP_TABLE[MSQuicFunc.CXPLAT_TOEPLITZ_LOOKUP_TABLE_COUNT_MAX];
        public byte[] HashKey = new byte[MSQuicFunc.CXPLAT_TOEPLITZ_KEY_SIZE_MAX];
        public CXPLAT_TOEPLITZ_INPUT_SIZE InputSize;

        public CXPLAT_TOEPLITZ_HASH()
        {
            for(int i = 0; i < LookupTableArray.Length; i++)
            {
                LookupTableArray[i] = new CXPLAT_TOEPLITZ_LOOKUP_TABLE();
            }
        }
    }

    internal static partial class MSQuicFunc
    {
        public const int NIBBLES_PER_BYTE = 2;
        public const int BITS_PER_NIBBLE = 4;
        public const int CXPLAT_TOEPLITZ_OUPUT_SIZE = sizeof(uint);
        public const int CXPLAT_TOEPLITZ_LOOKUP_TABLE_SIZE = 16;
        public const int CXPLAT_TOEPLITZ_LOOKUP_TABLE_COUNT_MAX = ((int)CXPLAT_TOEPLITZ_INPUT_SIZE.CXPLAT_TOEPLITZ_INPUT_SIZE_MAX * NIBBLES_PER_BYTE);
        public const int CXPLAT_TOEPLITZ_KEY_SIZE_MAX = ((int)CXPLAT_TOEPLITZ_INPUT_SIZE.CXPLAT_TOEPLITZ_INPUT_SIZE_MAX + CXPLAT_TOEPLITZ_OUPUT_SIZE);

        static void CxPlatToeplitzHashInitialize(CXPLAT_TOEPLITZ_HASH Toeplitz)
        {
            for (int i = 0; i < (int)Toeplitz.InputSize * NIBBLES_PER_BYTE; i++)
            {
                int StartByteOfKey = i / NIBBLES_PER_BYTE;

                uint Word1 = ((uint)Toeplitz.HashKey[StartByteOfKey] << 24) +
                                 ((uint)Toeplitz.HashKey[StartByteOfKey + 1] << 16) +
                                 ((uint)Toeplitz.HashKey[StartByteOfKey + 2] << 8) +
                                  (uint)Toeplitz.HashKey[StartByteOfKey + 3];

                uint Word2 = Toeplitz.HashKey[StartByteOfKey + 4];
                int BaseShift = (i % NIBBLES_PER_BYTE) * BITS_PER_NIBBLE;

                uint Signature1 = (Word1 << BaseShift) | (Word2 >> (8 * sizeof(byte) - BaseShift));
                BaseShift++;
                uint Signature2 = (Word1 << BaseShift) | (Word2 >> (8 * sizeof(byte) - BaseShift));
                BaseShift++;
                uint Signature3 = (Word1 << BaseShift) | (Word2 >> (8 * sizeof(byte) - BaseShift));
                BaseShift++;
                uint Signature4 = (Word1 << BaseShift) | (Word2 >> (8 * sizeof(byte) - BaseShift));

                for (int j = 0; j < CXPLAT_TOEPLITZ_LOOKUP_TABLE_SIZE; j++)
                {

                    Toeplitz.LookupTableArray[i].Table[j] = 0;
                    if (BoolOk(j & 0x1))
                    {
                        Toeplitz.LookupTableArray[i].Table[j] ^= Signature4;
                    }

                    if (BoolOk(j & 0x2))
                    {
                        Toeplitz.LookupTableArray[i].Table[j] ^= Signature3;
                    }

                    if (BoolOk(j & 0x4))
                    {
                        Toeplitz.LookupTableArray[i].Table[j] ^= Signature2;
                    }

                    if (BoolOk(j & 0x8))
                    {
                        Toeplitz.LookupTableArray[i].Table[j] ^= Signature1;
                    }
                }
            }
        }

        static void CxPlatToeplitzHashComputeAddr(CXPLAT_TOEPLITZ_HASH Toeplitz, QUIC_ADDR Addr, out uint Key, out int Offset)
        {
            NetLog.Assert(Addr.Family == AddressFamily.InterNetworkV6);

            Key = 0;
            Offset = 0;
            ReadOnlySpan<byte> IpBytes = Addr.GetAddressSpan();
            ushort nPort = (ushort)Addr.nPort;
            ReadOnlySpan<byte> nPortBytes = MemoryMarshal.Cast<ushort, byte>(MemoryMarshal.CreateSpan<ushort>(ref nPort, 1));
            Key ^= CxPlatToeplitzHashCompute(Toeplitz, 0, nPortBytes);
            Key ^= CxPlatToeplitzHashCompute(Toeplitz, 2, IpBytes);
            Offset = 2 + 16;
        }

        static uint CxPlatToeplitzHashCompute(CXPLAT_TOEPLITZ_HASH Toeplitz, int Toeplitz_Offset, ReadOnlySpan<byte> HashInput)
        {
            int BaseOffset = Toeplitz_Offset * NIBBLES_PER_BYTE;
            uint Result = 0;

            NetLog.Assert(HashInput.Length + Toeplitz_Offset <= (int)Toeplitz.InputSize);
            NetLog.Assert((BaseOffset + HashInput.Length * NIBBLES_PER_BYTE) <= (int)Toeplitz.InputSize * NIBBLES_PER_BYTE);

            for (int i = 0; i < HashInput.Length; i++)
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
