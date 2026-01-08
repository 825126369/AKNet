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
            var mTimer = Stopwatch.StartNew();
            for (int i = 0; i < 1000; i++)
            {
                int nLength = 1000000;
                Span<byte> mArray = new byte[nLength];
                RandomNumberGenerator.Fill(mArray);
                mAkCircularManyBuffer.WriteFrom(mArray);
                NetLog.Assert(mAkCircularManyBuffer.Length == nLength);

                Span<byte> mArray2 = new byte[nLength];
                NetLog.Assert(mAkCircularManyBuffer.CopyTo(0, mArray2) == nLength);

                mAkCircularManyBuffer.ClearBuffer(nLength);
                NetLog.Assert(mAkCircularManyBuffer.Length == 0);

                NetLog.Assert(BufferTool.orBufferEqual(mArray, mArray2));
                //NetLog.Assert(BufferTool.orBufferEqual(mArray, mArray3));
            }
            NetLog.Log($"花费时间: {mTimer.ElapsedMilliseconds}");
        }
    }
}
