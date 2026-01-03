using AKNet.Common;
using System;
using System.Net;

namespace MSTest
{
    [TestClass]
    public sealed class Test_IPEndPoint_Test
    {
        [TestMethod]
        public void TestMethod1()
        {
            IPEndPoint A1 = new IPEndPoint(IPAddress.Any, 10);
            IPEndPoint A2 = new IPEndPoint(IPAddress.Any, 10);
            if (!A1.Equals(A2))
            {
                Assert.Fail($"{A1} {A2}");
            }
        }
    }
}
