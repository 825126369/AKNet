using AKNet.Common;

namespace AKNet.Udp5MSQuic.Common
{
    internal static partial class MSQuicFunc
    {
        public static void DoTest()
        {
            for (int i = 0; i < 100000; i++)
            {
                ulong A = RandomTool.Random(0, QUIC_VAR_INT_MAX - 1);
                ulong B = 0;
                QUIC_SSBuffer mBuf = new byte[100];
                QuicVarIntEncode(A, mBuf);
                if (QuicVarIntDecode(ref mBuf, ref B))
                {
                    NetLog.Assert(A == B, $"DoTest Error: {A}, {B}");
                }
                else
                {
                    NetLog.Assert(A == B, $"DoTest Error");
                }
            }

            for (int i = 0; i < 100000; i++)
            {
                int A = RandomTool.Random(0, int.MaxValue - 1);
                QUIC_SSBuffer mBuf1 = new byte[100];
                QUIC_SSBuffer mBuf2 = new byte[100];
                EndianBitConverter.SetBytes(mBuf1.GetSpan(), 0, A);
                EndianBitConverter2.SetBytes(mBuf2.GetSpan(), 0, A);
                NetLog.Assert(orBufferEqual(mBuf1, mBuf2));
            }

            QUIC_RANGE mRange = new QUIC_RANGE();
            QuicRangeInitialize(16 * 10, mRange);

            bool bUpdate = false;
            for (int i = 0; i < 100; i++)
            {
                ulong A = RandomTool.Random(0, QUIC_VAR_INT_MAX - 1);
                int nCount = RandomTool.Random(1, ushort.MaxValue);
                QuicRangeAddRange(mRange, A, nCount, ref bUpdate);
            }

            NetLog.Log("mRange.Length: " + mRange.UsedLength);
        }
    }
}