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
        }
    }
}
