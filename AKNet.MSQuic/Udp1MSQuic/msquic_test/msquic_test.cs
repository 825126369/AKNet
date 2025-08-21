using AKNet.Common;
using System;
using System.Security.Cryptography.X509Certificates;

namespace AKNet.Udp1MSQuic.Common
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

            //PFX 证书测试
            X509Certificate2 mCert = X509CertTool.GetPfxCert();

            //压缩包号测试
            for (long i = 0; i < 100; i++)
            {
                ulong nPackageNumber = RandomTool.Random(0, ulong.MaxValue >> 20);
                int nNumberLength = 4;
                ulong A = nPackageNumber;
                ulong B = 0;
                ulong C = 0;
                byte[] mBuffer = new byte[1000];
                QuicPktNumEncode(A, nNumberLength, mBuffer);
                QuicPktNumDecode(nNumberLength, mBuffer, out B);
                C = QuicPktNumDecompress(nPackageNumber, B, nNumberLength);
                NetLog.Assert(A == C, $"{nPackageNumber}, {A}, {B}, {C}");
                if (A != C)
                {
                    break;
                }
            }

            //分区Id测试
            MsQuicLib.PartitionMask = 100;
            MsQuicLib.PartitionCount = 100;
            MsQuicCalculatePartitionMask();
            for (int i = 0; i < 100; i++)
            {
                var nId = QuicPartitionIdCreate(i);
                var nIndex = QuicPartitionIdGetIndex(nId);
                NetLog.Assert(i == nIndex);
            }

            //CXList 测试
            CXPLAT_LIST_ENTRY<int> mList = new CXPLAT_LIST_ENTRY<int>(-1);
            CXPLAT_LIST_ENTRY<int> mList2 = new CXPLAT_LIST_ENTRY<int>(-1);

            CXPLAT_LIST_ENTRY<int> Entry1 = new CXPLAT_LIST_ENTRY<int>(9);
            CXPLAT_LIST_ENTRY<int> Entry2 = new CXPLAT_LIST_ENTRY<int>(8);

            CxPlatListInitializeHead(mList);
            CxPlatListInitializeHead(mList2);

            //CxPlatListInsertHead(mList, new CXPLAT_LIST_ENTRY<int>(1));
            //CxPlatListInsertHead(mList, new CXPLAT_LIST_ENTRY<int>(2));
            //CxPlatListInsertHead(mList, new CXPLAT_LIST_ENTRY<int>(3));
            //CxPlatListInsertHead(mList, new CXPLAT_LIST_ENTRY<int>(4));
            //CxPlatListInsertHead(mList, new CXPLAT_LIST_ENTRY<int>(5));

            //CxPlatListInsertTail(mList, new CXPLAT_LIST_ENTRY<int>(1));
            //CxPlatListInsertTail(mList, new CXPLAT_LIST_ENTRY<int>(2));
            //CxPlatListInsertTail(mList, new CXPLAT_LIST_ENTRY<int>(3));
            //CxPlatListInsertTail(mList, new CXPLAT_LIST_ENTRY<int>(4));
            //CxPlatListInsertTail(mList, new CXPLAT_LIST_ENTRY<int>(5));

            CxPlatListInsertMiddle(mList, mList.Prev, Entry1);
            CxPlatListInsertMiddle(mList, mList.Prev, new CXPLAT_LIST_ENTRY<int>(1));
            CxPlatListInsertMiddle(mList, mList.Prev, new CXPLAT_LIST_ENTRY<int>(2));
            CxPlatListInsertMiddle(mList, mList.Prev, new CXPLAT_LIST_ENTRY<int>(3));
            CxPlatListInsertMiddle(mList, mList.Prev, new CXPLAT_LIST_ENTRY<int>(4));
            CxPlatListInsertMiddle(mList, mList.Prev, new CXPLAT_LIST_ENTRY<int>(5));
            CxPlatListInsertMiddle(mList, mList.Prev, Entry2);

            CxPlatListInsertMiddle(mList2, mList2.Prev, new CXPLAT_LIST_ENTRY<int>(10));
            CxPlatListInsertMiddle(mList2, mList2.Prev, new CXPLAT_LIST_ENTRY<int>(11));
            CxPlatListInsertMiddle(mList2, mList2.Prev, new CXPLAT_LIST_ENTRY<int>(12));

            CxPlatListMoveItems(mList, mList2);
            mList = mList2;

            CXPLAT_LIST_ENTRY<int> iter = mList.Next as CXPLAT_LIST_ENTRY<int>;
            while (mList != iter)
            {
                NetLog.Log(iter.value);
                iter = iter.Next as CXPLAT_LIST_ENTRY<int>;
            }
        }
    }
}