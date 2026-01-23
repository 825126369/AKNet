using System;
using System.Runtime.InteropServices;

namespace MSTest
{
    [TestClass]
    public sealed class Test_Span_Test
    {
        [TestMethod]
        public void TestFunc1()
        {
            Span<byte> buffer1 = new Span<byte>(new byte[] { 1, 2, 3, 4 });
            Span<byte> buffer2 = new Span<byte>(new byte[] { 2, 3, 3, 4 });
            buffer1.Slice(1, 2).CopyTo(buffer1);
            Assert.IsTrue(buffer1.SequenceEqual(buffer2));
            Assert.IsFalse(buffer1 == buffer2);
        }

        [TestMethod]
        public void TestFunc2()
        {
            int A = 0;
            Span<byte> B = MemoryMarshal.Cast<int, byte>(MemoryMarshal.CreateSpan(ref A, 1));
            Assert.IsTrue(B.Length == sizeof(int));
        }
    }
}
