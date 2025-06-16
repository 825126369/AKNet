namespace OtherTest
{
    internal class StructDefaultConstructorTest
    {
        public struct A
        {
            public int x;
            public A(int t = -1)
            {
                x = t;
            }
        }

        public void Test()
        {
            A m = new A();
            Console.WriteLine($"x: {m.x}");
        }
    }
}
