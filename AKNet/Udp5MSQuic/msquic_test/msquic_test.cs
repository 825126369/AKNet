using AKNet.Common;
using System;
using System.Security.Cryptography.X509Certificates;

namespace AKNet.Udp5MSQuic.Common
{
    internal static partial class MSQuicFunc
    {
        public static void DoTest()
        {
            for (int i = 0; i < 100000; i++)
            {
                int A = (int)RandomTool.Random(0, int.MaxValue - 1);
                int B = 0;
                Span<byte> mBuf = new Span<byte>(new byte[100]);
                QuicVarIntEncode(A, mBuf);

                ReadOnlySpan<byte> mBuf2 = mBuf;
                if (QuicVarIntDecode(ref mBuf2, ref B))
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
                ulong A = (ulong)i;
                int nCount = 1;
                QuicRangeAddRange(mRange, A, nCount, out bUpdate);
            }
            X509Certificate2 mCert = X509CertTool.GetPfxCert();
        }
    }
}