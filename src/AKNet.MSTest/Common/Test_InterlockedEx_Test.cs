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

            //并发逻辑
            Parallel.For(0, 10000, i =>
            {
                InterlockedEx.Increment(ref A);
                B++;
            });

            Assert.IsTrue(A == 10000);
            Assert.IsFalse(B == 10000);
        }
    }
}
