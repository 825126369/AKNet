using TestCommon;

namespace OtherTest
{
    internal class Program
    {
        static void Main(string[] args)
        {
            //TimerTest mTimerTest = new TimerTest();
            //mTimerTest.Test();

            //GoToTest mGoToTest = new GoToTest();
            //mGoToTest.Test();

            //CheckSumTest mCheckSumTest = new CheckSumTest();
            //mCheckSumTest.Test();

            //outTest mOutTest = new outTest();
            //mOutTest.Test();

            //RefStructTest mOutTest = new RefStructTest();
            //mOutTest.Test();

            RefStructTest mTest = new RefStructTest();
            mTest.Test();

            UpdateMgr.Do(Update);
        }

        static void Update(double a)
        {

        }
    }
}
