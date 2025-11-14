/************************************Copyright*****************************************
*        ProjectName:AKNet
*        Web:https://github.com/825126369/AKNet
*        Description:C#游戏网络库
*        Author:许珂
*        ModifyTime:2025/11/14 8:44:32
*        Copyright:MIT软件许可证
************************************Copyright*****************************************/
using AKNet.Common;
using System;
using System.Security.Cryptography.X509Certificates;

namespace MSQuic1
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
                MSQuicFunc.QuicVarIntEncode(A, mBuf);

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
            CXPLAT_LIST_ENTRY<int> mList = new CXPLAT_LIST_ENTRY<int>(0);
            CxPlatListInitializeHead(mList);

            CxPlatListInsertTail(mList, new CXPLAT_LIST_ENTRY<int>(1));
            CxPlatListInsertTail(mList, new CXPLAT_LIST_ENTRY<int>(2));
            CxPlatListInsertTail(mList, new CXPLAT_LIST_ENTRY<int>(3));
            CxPlatListInsertTail(mList, new CXPLAT_LIST_ENTRY<int>(4));
            CxPlatListInsertTail(mList, new CXPLAT_LIST_ENTRY<int>(5));
            CxPlatListInsertTail(mList, new CXPLAT_LIST_ENTRY<int>(6));
            CxPlatListInsertTail(mList, new CXPLAT_LIST_ENTRY<int>(7));

            NetLog.Log($"Count: {CxPlatListCount(mList)}");
            while (!CxPlatListIsEmpty(mList))
            {
                CxPlatListRemoveHead(mList);
                NetLog.Log($"Count: {CxPlatListCount(mList)}");
            }
        }
    }
}