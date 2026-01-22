using AKNet.Common;
using System;
using System.Diagnostics;
using System.Security.Cryptography;

namespace MSTest
{
    [TestClass]
    public sealed class Test_AkCircularManyBuffer_Test
    {
        [TestMethod]
        public void TestMethod1()
        {
            for (int i = 0; i < 100; i++)
            {
                byte[] mBuffer = new byte[RandomTool.RandomInt32(1024, ushort.MaxValue)];
                RandomBufTool.Random(mBuffer);

                AkCircularManyBuffer mAkCircularManyBuffer = new AkCircularManyBuffer();
                mAkCircularManyBuffer.WriteFrom(mBuffer);

                byte[] mBuffer2 = new byte[RandomTool.RandomInt32(1024, ushort.MaxValue)];
                int nCopyOffset = RandomTool.RandomInt32(0, byte.MaxValue);
                int nCopyLength = RandomTool.RandomInt32(0, mBuffer2.Length);
                nCopyLength = Math.Min(nCopyLength, mBuffer.Length - nCopyOffset);

                mAkCircularManyBuffer.CopyTo(mBuffer2, nCopyOffset, nCopyLength);
            }
        }

        [TestMethod]
        public void TestMethod2()
        {
            byte[] mBuffer = new byte[1388];
            RandomBufTool.Random(mBuffer);

            AkCircularManyBuffer mAkCircularManyBuffer = new AkCircularManyBuffer();
            mAkCircularManyBuffer.WriteFrom(mBuffer);

            byte[] mBuffer2 = new byte[1388];
            mAkCircularManyBuffer.CopyTo(mBuffer2, 5, 1024);
        }

        [TestMethod]
        public void TestMethod3()
        {
            AkCircularManyBuffer mAkCircularManyBuffer = new AkCircularManyBuffer();
            for (int i = 0; i < 1000; i++)
            {
                int nLength = 1000000;
                Span<byte> mArray = new byte[nLength];
                RandomNumberGenerator.Fill(mArray);
                mAkCircularManyBuffer.WriteFrom(mArray);
                Assert.IsTrue(mAkCircularManyBuffer.Length == nLength);
                Span<byte> mArray2 = new byte[nLength];
                Assert.IsTrue(mAkCircularManyBuffer.CopyTo(mArray2) == nLength);
                mAkCircularManyBuffer.ClearBuffer(nLength);
                Assert.IsTrue(mAkCircularManyBuffer.Length == 0);
                Assert.IsTrue(BufferTool.orBufferEqual(mArray, mArray2));
            }
        }
    }
}
