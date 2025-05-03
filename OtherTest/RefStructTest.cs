using System.Diagnostics;

namespace OtherTest
{
    public class A
    {
        public int x;
    }

    public ref struct RS
    {
       // private Span<byte> _buffer;
        public int x;
        public A y;

        //public RS(int size)
        //{
        //    _buffer = new byte[size];
        //}
    }

    public struct S
    {
        //private Span<byte> _buffer;
        public int x;
        public A y;
    }

    internal class RefStructTest
    {
        Stopwatch Stopwatch = Stopwatch.StartNew();
        public RefStructTest()
        {
           
        }

        public void Test()
        {
            long Now = Stopwatch.ElapsedMilliseconds;
            RS rS = new RS();
            for (int i = 0; i < 100000000; i++)
            {
                Do1(rS);
            }

            Console.WriteLine(Stopwatch.ElapsedMilliseconds - Now);
            Now = Stopwatch.ElapsedMilliseconds;
            S s = new S();
            for (int i = 0; i < 100000000; i++)
            {
                Do1(s);
            }
            Console.WriteLine(Stopwatch.ElapsedMilliseconds - Now);
        }

        public void Do1(RS r)
        {
            r.x++;
            r.y = new A();

            //Console.WriteLine(r.x);
        }

        public void Do1(S r)
        {
            r.x++;
            r.y = new A();
           // Console.WriteLine(r.x);
        }
    }
}
