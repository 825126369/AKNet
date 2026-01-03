using AKNet.Common;
using System;

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
                byte[] mBuffer = new byte[RandomTool.Random(1024, ushort.MaxValue)];
                RandomBufTool.Random(mBuffer);

                AkCircularManyBuffer mAkCircularManyBuffer = new AkCircularManyBuffer();
                mAkCircularManyBuffer.WriteFrom(mBuffer);

                byte[] mBuffer2 = new byte[RandomTool.Random(1024, ushort.MaxValue)];
                int nCopyOffset = RandomTool.Random(0, byte.MaxValue);
                int nCopyLength = RandomTool.Random(0, mBuffer2.Length);
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
    }
}
