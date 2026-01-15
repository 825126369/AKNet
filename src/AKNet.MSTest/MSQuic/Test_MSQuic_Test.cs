/************************************Copyright*****************************************
*        ProjectName:AKNet
*        Web:https://github.com/825126369/AKNet
*        Description:C#游戏网络库
*        Author:许珂
*        StartTime:2024/11/01 00:00:00
*        ModifyTime:2025/11/30 19:43:18
*        Copyright:MIT软件许可证
************************************Copyright*****************************************/
using AKNet.Common;
using System;
using System.Security.Cryptography.X509Certificates;
using static Google.Protobuf.WellKnownTypes.Field.Types;


#if true
using MSQuic1;
#else
using MSQuic2;
#endif

namespace MSTest
{
    [TestClass]
    public sealed class Test_MSQuic_Test
    {
        [TestMethod]
        public void TestMethod1()
        {
            //for (int i = 0; i < 100000; i++)
            //{
            //    int A = (int)RandomTool.Random(0, int.MaxValue - 1);
            //    int B = 0;
            //    Span<byte> mBuf = new Span<byte>(new byte[100]);
            //    MSQuicFunc.QuicVarIntEncode(A, mBuf);

            //    ReadOnlySpan<byte> mBuf2 = mBuf;
            //    if (QuicVarIntDecode(ref mBuf2, ref B))
            //    {
            //        NetLog.Assert(A == B, $"DoTest Error: {A}, {B}");
            //    }
            //    else
            //    {
            //        NetLog.Assert(A == B, $"DoTest Error");
            //    }
            //}

            ////PFX 证书测试
            //X509Certificate2 mCert = X509CertTool.GetPfxCert();

            ////压缩包号测试
            //for (long i = 0; i < 100; i++)
            //{
            //    ulong nPackageNumber = RandomTool.Random(0, ulong.MaxValue >> 20);
            //    int nNumberLength = 4;
            //    ulong A = nPackageNumber;
            //    ulong B = 0;
            //    ulong C = 0;
            //    byte[] mBuffer = new byte[1000];
            //    QuicPktNumEncode(A, nNumberLength, mBuffer);
            //    QuicPktNumDecode(nNumberLength, mBuffer, out B);
            //    C = QuicPktNumDecompress(nPackageNumber, B, nNumberLength);
            //    NetLog.Assert(A == C, $"{nPackageNumber}, {A}, {B}, {C}");
            //    if (A != C)
            //    {
            //        break;
            //    }
            //}

            ////分区Id测试
            //MsQuicLib.PartitionMask = 100;
            //MsQuicLib.PartitionCount = 100;
            //MsQuicCalculatePartitionMask();
            //for (int i = 0; i < 100; i++)
            //{
            //    var nId = QuicPartitionIdCreate(i);
            //    var nIndex = QuicPartitionIdGetIndex(nId);
            //    NetLog.Assert(i == nIndex);
            //}

            ////CXList 测试
            //CXPLAT_LIST_ENTRY<int> mList = new CXPLAT_LIST_ENTRY<int>(0);
            //CxPlatListInitializeHead(mList);

            //CxPlatListInsertTail(mList, new CXPLAT_LIST_ENTRY<int>(1));
            //CxPlatListInsertTail(mList, new CXPLAT_LIST_ENTRY<int>(2));
            //CxPlatListInsertTail(mList, new CXPLAT_LIST_ENTRY<int>(3));
            //CxPlatListInsertTail(mList, new CXPLAT_LIST_ENTRY<int>(4));
            //CxPlatListInsertTail(mList, new CXPLAT_LIST_ENTRY<int>(5));
            //CxPlatListInsertTail(mList, new CXPLAT_LIST_ENTRY<int>(6));
            //CxPlatListInsertTail(mList, new CXPLAT_LIST_ENTRY<int>(7));

            //NetLog.Log($"Count: {CxPlatListCount(mList)}");
            //while (!CxPlatListIsEmpty(mList))
            //{
            //    CxPlatListRemoveHead(mList);
            //    NetLog.Log($"Count: {CxPlatListCount(mList)}");
            //}
        }

        [TestMethod]
        public void TestMethod2()
        {
            QUIC_RANGE mRange = new QUIC_RANGE();
            MSQuicFunc.QuicRangeInitialize(QUIC_SUBRANGE.sizeof_Length * 128, mRange);

            List<ulong> mList = new List<ulong>();
            for (int i = 0; i < 100; i++)
            {
                ulong A = (ulong)RandomTool.Random(0, ushort.MaxValue);
                int nCount = RandomTool.Random(1, 3);
                Assert.IsTrue(MSQuicFunc.QuicRangeAddRange(mRange, A, nCount, out _) != null);

                for(int j = 0; j < nCount; j++)
                {
                    ulong value = A + (ulong)j;
                    if (!mList.Contains(value))
                    {
                        mList.Add(value);
                    }
                }
            }

            List<ulong> mList2 = new List<ulong>();
            for (int i = 0; i < mRange.UsedLength; i++)
            {
                for (int j = 0; j < mRange.SubRanges[i].Count; j++)
                {
                    ulong value = mRange.SubRanges[i].Low + (ulong)j;
                    if (!mList2.Contains(value))
                    {
                        mList2.Add(value);
                    }
                }
            }

            Assert.IsTrue(mList2.Count == mList.Count);

            foreach (ulong j in mList)
            {
                bool bFind = false;
                for (int i = 0; i < mRange.UsedLength; i++)
                {
                    if(j >= mRange.SubRanges[i].Low && j <= mRange.SubRanges[i].High)
                    {
                        bFind = true;
                    }
                }

                Assert.IsTrue(bFind);
            }


            for (int i = 0; i < mRange.UsedLength; i++)
            {
                Assert.IsTrue(mRange.SubRanges[i].High >= mRange.SubRanges[i].Low);
            }

            for (int i = 1; i < mRange.UsedLength; i++)
            {
                Assert.IsTrue(mRange.SubRanges[i].Low > mRange.SubRanges[i - 1].High);
            }
        }

    }
}