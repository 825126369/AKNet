using AKNet.Common;
using System.Net;

namespace MSTest
{
    [TestClass]
    public sealed class Test_InterlockedEx_Test
    {
        [TestMethod]
        public void TestMethod1()
        {
            ulong A = 0;
            ulong B = 0;

            const ulong C = 100000;

            //并发逻辑
            Parallel.For(0, (long)C, i =>
            {
                InterlockedEx.Increment(ref A);
                B++;
            });

            Assert.AreEqual(A, C);
        }
    }
}
