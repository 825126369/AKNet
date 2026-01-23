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
using MSQuic1;

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
            MSQuicFunc.QuicRangeInitialize(QUIC_SUBRANGE.sizeof_Length * 1024, mRange);
            long largeACK = 0;
            long minACK = long.MaxValue;
            for (int i = 0; i < 1000; i++)
            {
                long A = RandomTool.RandomInt64(0, MSQuicFunc.QUIC_VAR_INT_MAX);
                int nCount = RandomTool.RandomInt32(1, ushort.MaxValue);
                Assert.IsTrue(MSQuicFunc.QuicRangeAddRange(mRange, A, nCount, out _) != null);
                if (MSQuicFunc.QuicRangeGetHighByLow(A, nCount) > largeACK)
                {
                    largeACK = MSQuicFunc.QuicRangeGetHighByLow(A, nCount);
                }

                if (A < minACK)
                {
                    minACK = A;
                }
            }

            Assert.IsTrue(MSQuicFunc.QuicRangeGetMin(mRange) == minACK);
            Assert.IsTrue(MSQuicFunc.QuicRangeGetMax(mRange) == largeACK);
            MSQuicFunc.QuicRangeReset(mRange);

            const int nTestCount = 1000;
            List<long> mList = new List<long>(ushort.MaxValue * nTestCount);
            List<long> mList2 = new List<long>(ushort.MaxValue * nTestCount);
            largeACK = 0;
            minACK = long.MaxValue;
            for (int i = 0; i < nTestCount; i++)
            {
                long A = RandomTool.RandomInt64(0, MSQuicFunc.QUIC_VAR_INT_MAX);
                int nCount = RandomTool.RandomInt32(1, byte.MaxValue);
                Assert.IsTrue(MSQuicFunc.QuicRangeAddRange(mRange, A, nCount, out _) != null);

                for (int j = 0; j < nCount; j++)
                {
                    long value = A + j;
                    if (!mList.Contains(value))
                    {
                        mList.Add(value);
                    }
                }

                if (MSQuicFunc.QuicRangeGetHighByLow(A, nCount) > largeACK)
                {
                    largeACK = MSQuicFunc.QuicRangeGetHighByLow(A, nCount);
                }

                if (A < minACK)
                {
                    minACK = A;
                }
            }

            Assert.IsTrue(MSQuicFunc.QuicRangeGetMin(mRange) == minACK);
            Assert.IsTrue(MSQuicFunc.QuicRangeGetMax(mRange) == largeACK);
            for (int i = 0; i < mRange.UsedLength; i++)
            {
                for (int j = 0; j < mRange.SubRanges[i].Count; j++)
                {
                    long value = mRange.SubRanges[i].Low + (long)j;
                    if (!mList2.Contains(value))
                    {
                        mList2.Add(value);
                    }
                }
            }

            Assert.IsTrue(mList2.Count == mList.Count);

            foreach (long j in mList)
            {
                bool bFind = false;
                for (int i = 0; i < mRange.UsedLength; i++)
                {
                    if (j >= mRange.SubRanges[i].Low && j <= mRange.SubRanges[i].High)
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


        [TestMethod]
        public void TestMethod3()
        {
            QUIC_RANGE mRange = new QUIC_RANGE();
            MSQuicFunc.QuicRangeInitialize(QUIC_SUBRANGE.sizeof_Length * 1024, mRange);
            MSQuicFunc.QuicRangeAddRange(mRange, 1, 4, out _);
            MSQuicFunc.QuicRangeAddRange(mRange, 2, 1, out _);
            MSQuicFunc.QuicRangeAddRange(mRange, 3, 4, out _);
            Assert.IsTrue(mRange.UsedLength == 1);
            Assert.IsTrue(mRange.SubRanges[0] == new QUIC_SUBRANGE() { Low = 1, Count = 6 });
            MSQuicFunc.QuicRangeAddRange(mRange, 0, 1, out _);
            MSQuicFunc.QuicRangeAddRange(mRange, 7, 1, out _);
            Assert.IsTrue(mRange.UsedLength == 1);
            Assert.IsTrue(mRange.SubRanges[0] == new QUIC_SUBRANGE() { Low = 0, Count = 8 });
            MSQuicFunc.QuicRangeAddRange(mRange, 9, 1, out _);
            MSQuicFunc.QuicRangeAddRange(mRange, 10, 1, out _);
            Assert.IsTrue(mRange.UsedLength == 2);
            Assert.IsTrue(mRange.SubRanges[0] == new QUIC_SUBRANGE() { Low = 0, Count = 8 });
            Assert.IsTrue(mRange.SubRanges[1] == new QUIC_SUBRANGE() { Low = 9, Count = 2 });

            mRange.UsedLength = 0;
            for (int i = 0; i < 1000; i++)
            {
                Assert.IsTrue(MSQuicFunc.QuicRangeAddRange(mRange, i, 1, out _) != null);
            }
            Assert.IsTrue(mRange.UsedLength == 1);
            Assert.IsTrue(mRange.SubRanges[0].Low == 0);
            Assert.IsTrue(mRange.SubRanges[0].High == 999);

            mRange = new QUIC_RANGE();
            MSQuicFunc.QuicRangeInitialize(QUIC_SUBRANGE.sizeof_Length * 1024, mRange);
            MSQuicFunc.QuicRangeAddRange(mRange, 20, 2, out _);
            MSQuicFunc.QuicRangeAddRange(mRange, 25, 2, out _);
            MSQuicFunc.QuicRangeAddRange(mRange, 30, 2, out _);
            MSQuicFunc.QuicRangeAddRange(mRange, 18, 2, out _);
            MSQuicFunc.QuicRangeAddRange(mRange, 32, 2, out _);
            Assert.IsTrue(mRange.UsedLength == 3);
            Assert.IsTrue(mRange.SubRanges[0] == new QUIC_SUBRANGE() { Low = 18, Count = 4 });
            Assert.IsTrue(mRange.SubRanges[2] == new QUIC_SUBRANGE() { Low = 30, Count = 4 });

            MSQuicFunc.QuicRangeAddRange(mRange, 22, 5, out _);
            MSQuicFunc.QuicRangeAddRange(mRange, 27, 3, out _);
            Assert.IsTrue(mRange.UsedLength == 1);
            Assert.IsTrue(mRange.SubRanges[0] == new QUIC_SUBRANGE() { Low = 18, Count = 16 });

            MSQuicFunc.QuicRangeSetMin(mRange, 19);
            Assert.IsTrue(mRange.UsedLength == 1);
            Assert.IsTrue(mRange.SubRanges[0] == new QUIC_SUBRANGE() { Low = 19, Count = 15 });

            MSQuicFunc.QuicRangeSetMin(mRange, 33);
            Assert.IsTrue(mRange.UsedLength == 1);
            Assert.IsTrue(mRange.SubRanges[0] == new QUIC_SUBRANGE() { Low = 33, Count = 1 });
        }

        [TestMethod]
        public void TestMethod4()
        {
            QUIC_SSBuffer Encode()
            {
                QUIC_RANGE mAckRange = new QUIC_RANGE();
                MSQuicFunc.QuicRangeInitialize(QUIC_SUBRANGE.sizeof_Length * 1024, mAckRange);
                MSQuicFunc.QuicRangeAddRange(mAckRange, 1, 1, out _);
                MSQuicFunc.QuicRangeAddRange(mAckRange, 2, 1, out _);
                MSQuicFunc.QuicRangeAddRange(mAckRange, 3, 1, out _);
                MSQuicFunc.QuicRangeAddRange(mAckRange, 4, 1, out _);
                MSQuicFunc.QuicRangeAddRange(mAckRange, 5, 1, out _);
                MSQuicFunc.QuicRangeAddRange(mAckRange, 6, 1, out _);
                MSQuicFunc.QuicRangeAddRange(mAckRange, 7, 1, out _);

                QUIC_ACK_ECN_EX mECN = new QUIC_ACK_ECN_EX();
                mECN.ECT_0_Count = 1;
                mECN.ECT_1_Count = 2;
                mECN.CE_Count = 3;

                long AckDelay = 1;
                QUIC_SSBuffer mBuffer = new byte[1024];
                Assert.IsTrue(MSQuicFunc.QuicAckFrameEncode(mAckRange, AckDelay, mECN, ref mBuffer));
                mBuffer.Length = mBuffer.Offset;
                mBuffer.Offset = 0;
                return mBuffer;
            }

            void Decode(QUIC_SSBuffer mBuffer)
            {
                byte nFrameType = 0;
                Assert.IsTrue(MSQuicFunc.QuicVarIntDecode(ref mBuffer, ref nFrameType));

                QUIC_ACK_ECN_EX mECN = new QUIC_ACK_ECN_EX();
                long AckDelay = 0;
                bool InvalidFrame = false;

                QUIC_RANGE mDecodeRange = new QUIC_RANGE();
                MSQuicFunc.QuicRangeInitialize(QUIC_SUBRANGE.sizeof_Length * 1024, mDecodeRange);
                Assert.IsTrue(MSQuicFunc.QuicAckFrameDecode(
                    (QUIC_FRAME_TYPE)nFrameType,
                    ref mBuffer,
                    ref InvalidFrame,
                    mDecodeRange,
                    ref mECN,
                    ref AckDelay));

                Assert.IsTrue(mDecodeRange.SubRanges[0] == new QUIC_SUBRANGE() { Low = 1, Count = 7 });
                Assert.IsTrue(mECN.ECT_0_Count == 1);
                Assert.IsTrue(mECN.ECT_1_Count == 2);
                Assert.IsTrue(mECN.CE_Count == 3);
                Assert.IsTrue(AckDelay == 1);
                Assert.IsTrue(AckDelay == 1);
            }

            Decode(Encode());
        }

        [TestMethod]
        public void TestMethod5()
        {
            QUIC_RANGE mRange1 = new QUIC_RANGE();
            MSQuicFunc.QuicRangeInitialize(QUIC_SUBRANGE.sizeof_Length * 1024, mRange1);
            for (int i = 0; i < 100; i++)
            {
                long A = RandomTool.RandomInt64(0, MSQuicFunc.QUIC_VAR_INT_MAX);
                long nCount = RandomTool.RandomInt32(1, ushort.MaxValue);
                Assert.IsTrue(MSQuicFunc.QuicRangeAddRange(mRange1, A, nCount, out _) != null);
            }

            QUIC_ACK_ECN_EX mECN1 = new QUIC_ACK_ECN_EX();
            mECN1.ECT_0_Count = RandomTool.RandomInt64(0, MSQuicFunc.QUIC_VAR_INT_MAX);
            mECN1.ECT_1_Count = RandomTool.RandomInt64(0, MSQuicFunc.QUIC_VAR_INT_MAX);
            mECN1.CE_Count = RandomTool.RandomInt64(0, MSQuicFunc.QUIC_VAR_INT_MAX);

            long AckDelay1 = RandomTool.RandomInt64(0, MSQuicFunc.QUIC_VAR_INT_MAX);

            QUIC_SSBuffer Encode()
            {
                
                QUIC_SSBuffer mBuffer = new byte[8192];
                Assert.IsTrue(MSQuicFunc.QuicAckFrameEncode(mRange1, AckDelay1, mECN1, ref mBuffer));
                mBuffer.Length = mBuffer.Offset;
                mBuffer.Offset = 0;
                return mBuffer;
            }

            void Decode(QUIC_SSBuffer mBuffer)
            {
                byte nFrameType = 0;
                Assert.IsTrue(MSQuicFunc.QuicVarIntDecode(ref mBuffer, ref nFrameType));

                QUIC_ACK_ECN_EX mECN2 = new QUIC_ACK_ECN_EX();
                long AckDelay2 = 0;
                bool InvalidFrame = false;

                QUIC_RANGE mRange2 = new QUIC_RANGE();
                MSQuicFunc.QuicRangeInitialize(QUIC_SUBRANGE.sizeof_Length * 1024, mRange2);
                Assert.IsTrue(MSQuicFunc.QuicAckFrameDecode(
                    (QUIC_FRAME_TYPE)nFrameType,
                    ref mBuffer,
                    ref InvalidFrame,
                    mRange2,
                    ref mECN2,
                    ref AckDelay2));

                Assert.IsTrue(mECN2.ECT_0_Count == mECN1.ECT_0_Count);
                Assert.IsTrue(mECN2.ECT_1_Count == mECN1.ECT_1_Count);
                Assert.IsTrue(mECN2.CE_Count == mECN1.CE_Count);
                Assert.IsTrue(AckDelay1 == AckDelay2);

                Assert.IsTrue(mRange2.UsedLength == mRange1.UsedLength);
                for (int i = 0; i < mRange2.UsedLength; i++)
                {
                    Assert.IsTrue(mRange2.SubRanges[i] == mRange1.SubRanges[i]);
                }
            }

            Decode(Encode());
        }

        [TestMethod]
        public void TestMethod6()
        {
            for (int i = 0; i < 100000; i++)
            {
                long A = RandomTool.RandomInt64(0, MSQuicFunc.QUIC_VAR_INT_MAX);
                long B = 0;
                Span<byte> mBuf = new Span<byte>(new byte[100]);
                MSQuicFunc.QuicVarIntEncode(A, mBuf);

                ReadOnlySpan<byte> mBuf2 = mBuf;
                if (MSQuicFunc.QuicVarIntDecode(ref mBuf2, ref B))
                {
                    Assert.IsTrue(A == B, $"DoTest Error: {A}, {B}");
                }
                else
                {
                    Assert.IsTrue(A == B, $"DoTest Error");
                }
            }

            for (int i = 0; i < 100000; i++)
            {
                long A = RandomTool.RandomInt64(0, MSQuicFunc.QUIC_VAR_INT_MAX);
                long B = 0;
                QUIC_SSBuffer mBuf = new byte[100];
                MSQuicFunc.QuicVarIntEncode(A, mBuf);

                QUIC_SSBuffer mBuf2 = mBuf;
                if (MSQuicFunc.QuicVarIntDecode(ref mBuf2, ref B))
                {
                    Assert.IsTrue(A == B, $"DoTest Error: {A}, {B}");
                }
                else
                {
                    Assert.IsTrue(A == B, $"DoTest Error");
                }
            }
        }

    }
}