using System.Diagnostics;

namespace OtherTest
{
    public ref struct RS
    {
        public int x;
    }

    internal class RefStructTest
    {
        Stopwatch Stopwatch = Stopwatch.StartNew();
        public RefStructTest()
        {
           
        }

        public void Test()
        {
            RS rS = new RS();
            Do1(rS);
        }

        public void Do1(RS r)
        {
            r.x+= 11;
            Console.WriteLine(r.x);
        }
    }
}
