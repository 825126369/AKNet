using System.Diagnostics;
using TestCommon;

namespace OtherTest
{
    internal class outTest
    {
        internal class outTestClass
        {
            public int AAA;
        }

        public void Test()
        {
            outTestClass m = new outTestClass();
            Func(out m.AAA);
            Console.WriteLine("AAA: " + m.AAA);
        }

        private void Func(out int A)
        {
            A = 2;
        }
    }
}