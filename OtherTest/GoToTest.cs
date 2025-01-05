namespace OtherTest
{
    internal class GoToTest
    {
        public void Test()
        {
            goto Label1;

        Label0:
            Console.WriteLine("00000000000000000000000000");
        Label1:
            {
                Console.WriteLine("11111111111111111111111111");
            }
        Label2:
            {
                Console.WriteLine("22222222222222222222222222");
            }
        Label3:
            Console.WriteLine("33333333333333333333333333");
        }
    }
}
