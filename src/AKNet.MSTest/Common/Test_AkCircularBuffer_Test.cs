using AKNet.Common;
using System;
using System.Diagnostics;
using System.Security.Cryptography;

namespace MSTest
{
    [TestClass]
    public sealed class Test_AkCircularBuffer_Test
    {
        [TestMethod]
        public void TestFunc1()
        {
            AkCircularBuffer mAkCircularManyBuffer = new AkCircularBuffer();
            for (int i = 0; i < 1000; i++)
            {
                int nLength = 1000000;
                Span<byte> mArray = new byte[nLength];
                RandomNumberGenerator.Fill(mArray);
                mAkCircularManyBuffer.WriteFrom(mArray);
                Assert.IsTrue(mAkCircularManyBuffer.Length == nLength);

                Span<byte> mArray2 = new byte[nLength];
                Assert.IsTrue(mAkCircularManyBuffer.CopyTo(0, mArray2) == nLength);

                mAkCircularManyBuffer.ClearBuffer(nLength);
                Assert.IsTrue(mAkCircularManyBuffer.Length == 0);

                Assert.IsTrue(BufferTool.orBufferEqual(mArray, mArray2));
            }
        }
    }
}
